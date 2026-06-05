using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Graphics;

/// <summary>
/// Represents the layers of the Draw loop.
/// Layers will be drawn in order of declaration.
/// </summary>
public enum DrawLayer
{
    /// <summary>
    /// The background of the scene. Does not scroll.
    /// </summary>
    Base,
    /// <summary>
    /// A semi-distant background, which scrolls at a slower rate from Scene.
    /// </summary>
    Background,
    /// <summary>
    /// The foreground of the scene. 
    /// Positions of objects which are drawn in the scene should match 
    /// corresponding positions of hitboxes in the scene (if they exist).
    /// </summary>
    Scene,
    /// <summary>
    /// Represents objects closer than the scene, which scroll at a faster rate.
    /// </summary>
    Foreground,
    /// <summary>
    /// Represents a UI overlay which is always drawn on top. Does not scroll.
    /// </summary>
    UI
}

/// <summary>
/// Represents a viewport on a scene.
/// Contains the information about a specific run of the Draw loop for a particular <paramref name="Layer"/>.
/// </summary>
/// <param name="Screen">The <see cref="IScreen"/> for this run of the Draw loop.</param>
/// <param name="Millis">
/// The total number of milliseconds for which this game has been running. 
/// Equal to <see cref="GameTime.TotalGameTime.TotalMilliseconds"/>.
/// </param>
/// <param name="Layer">The layer currently being drawn.</param>
public record View(IScreen Screen, double Millis, DrawLayer Layer);

/// <summary>
/// Represents a camera viewing a scene, including size and position information.
/// </summary>
/// <param name="size">The size of the camera viewport, in pixels.</param>
/// <param name="totalSize">The size of the total area over which the camera may range, in pixels.</param>
public class Camera(Vector2 size, Vector2 totalSize)
{
    private Vector4 bounds = new(0, 0, size.X, size.Y);

    /// <summary>
    /// Moves the camera to the specified position, adjusting as necessary to stay in bounds.
    /// </summary>
    /// <param name="pos">The position to attempt to move the camera to.</param>
    public void MoveTo(Vector2 pos)
    {
        Vector2 newPos = VectorMath.Min(pos, totalSize - size);
        newPos = VectorMath.Max(newPos, Vector2.Zero);
        bounds.X = newPos.X;
        bounds.Y = newPos.Y;
    }

    /// <summary>
    /// Project <paramref name="vector"/> from the 3D area of the scene into this 2D viewport.
    /// </summary>
    /// <param name="vector">The 3D vector representing a position in the scene.</param>
    /// <returns>The projection of <paramref name="vector"/> onto the 2D viewport.</returns>
    public Vector2 Project(Vector3 vector)
    {
        const float ratioYZ = 0.5F;

        Vector2 pos2 = new(0, totalSize.Y);

        // impact of vector.Z on pos2.Y
        float impactZY = vector.Z * ratioYZ;

        // impact of vector.Y on pos2.Y
        float ratioYY = 1 - (vector.Z / (bounds.W * 2));
        float impactYY = vector.Y * ratioYY;

        // Since the Y coordinate in 3-space counts up as towards the top of the screen
        // and the Y coordinate in 2-space counts up as towards the bottom, we need to 
        // reverse the direction when creating a projection.
        pos2.Y -= impactZY + impactYY;

        // impact of vector.Z on pos2.X
        float impactZX = 1 - (vector.Z / (bounds.Z * 2));

        // center X of bounds
        float lCX = bounds.X + (bounds.Z / 2);
        // distance from center X of bounds
        float dlCX = Math.Abs(vector.X - lCX);
        // distance from center X of bounds after perspective is applied
        float dlCXp = dlCX * impactZX;
        pos2.X = lCX + (vector.X < lCX ? -dlCXp : dlCXp);

        return pos2;
    }

    /// <summary>
    /// The matrix transformation for the current camera state.
    /// </summary>
    public Matrix Transform => Matrix.CreateTranslation(new(bounds.X, bounds.Y, 0));
}

/// <summary>
/// A collection of cameras created for each DrawLayer for a size of viewport and size of level.
/// </summary>
/// <param name="viewportSize">The size of area which should be visible at any given point in time, in pixels.</param>
/// <param name="totalSize">The size of the entire viewable area, in pixels.</param>
public class CameraCollection(Vector2 viewportSize, Vector2 totalSize)
{
    private static float GetFactor(DrawLayer layer) => layer switch
    {
        DrawLayer.Base => 0,
        DrawLayer.Background => 0.5F,
        DrawLayer.Scene => 1,
        DrawLayer.Foreground => 2,
        DrawLayer.UI => 0,
        _ => throw new ArgumentException("Unsupported draw layer")
    };

    private readonly ImmutableDictionary<DrawLayer, Camera> cameras = Enum.GetValues<DrawLayer>()
        .Select(l => new KeyValuePair<DrawLayer, Camera>(l, new(viewportSize, VectorMath.Max(totalSize * GetFactor(l), viewportSize))))
        .ToImmutableDictionary();

    /// <summary>
    /// Gets the camera for a particular DrawLayer
    /// </summary>
    /// <param name="layer">The layer to get the camera for.</param>
    /// <returns>The camera for a particular layer.</returns>
    public Camera Get(DrawLayer layer) => cameras[layer];

    /// <summary>
    /// Moves all cameras in the scene to be centered on the specified position 
    /// in the scene's 3-space in tandem, adjusting for the viewport bounds as necessary.
    /// </summary>
    /// <param name="pos">The scene position all cameras should be moved to.</param>
    public void MoveAllTo(Vector3 scenePos)
    {
        Vector2 center = Get(DrawLayer.Scene).Project(scenePos);
        Vector2 topLeft = center - viewportSize / 2;
        foreach(KeyValuePair<DrawLayer, Camera> p in cameras)
            p.Value.MoveTo(topLeft * GetFactor(p.Key));
    }
}