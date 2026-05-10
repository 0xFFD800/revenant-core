using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
/// <param name="Screen">The <see cref="SpriteBatch"/> for this run of the Draw loop.</param>
/// <param name="Millis">
/// The total number of milliseconds for which this game has been running. 
/// Equal to <see cref="GameTime.TotalGameTime.TotalMilliseconds"/>.
/// </param>
/// <param name="Layer">The layer currently being drawn.</param>
public record View(SpriteBatch Screen, double Millis, DrawLayer Layer);