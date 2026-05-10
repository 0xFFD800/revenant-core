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
/// A wrapper around the default SpriteBatch allowing drawing code to utilize a stack of transformation matrices.
/// Calls to Push and Pop <em>must</em> be balanced on any given run of the Draw loop.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Pushes a new transformation matrix.
    /// If there is no active sprite buffer, this will create a new one with the provided matrix.
    /// Otherwise, this will flush the existing sprite buffer and create a new one with the multiple of the current two.
    /// </summary>
    /// <param name="transform">The transformation matrix to apply to the new buffer.</param>
    void Push(Matrix transform);

    /// <summary>
    /// Pop the active transformation matrix off the stack.
    /// </summary>
    void Pop();
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