using System;
using Microsoft.Xna.Framework;

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
public class Camera(Vector2 size)
{
    private Vector4 bounds = new(0, 0, size.X, size.Y);
    
    public void SetPos(Vector2 pos)
    {
        bounds.X = pos.X;
        bounds.Y = pos.Y;
    }

    /// <summary>
    /// Project <paramref name="vector"/> from the 3D area of the scene into this 2D viewport.
    /// </summary>
    /// <param name="vector">The 3D vector representing a position in the scene.</param>
    /// <returns>The projection of <paramref name="vector"/> onto the 2D viewport.</returns>
    public Vector2 Project(Vector3 vector)
    {
        const float ratioYZ = 0.5F;

        Vector2 pos2 = Vector2.Zero;

        // impact of vector.Z on pos2.Y
        float impactZY = vector.Z * ratioYZ;

        // impact of vector.Y on pos2.Y
        float ratioYY = 1 - (vector.Z / (bounds.W * 2));
        float impactYY = vector.Y * ratioYY;

        pos2.Y = impactZY + impactYY;

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
}