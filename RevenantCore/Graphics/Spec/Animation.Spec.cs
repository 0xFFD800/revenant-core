using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RevenantCore.Graphics.Spec;

/// <summary>
/// A YAML-serializable representation of a 2-dimensional integer rectanble.
/// </summary>
public class RectangleSpec
{
    /// <summary>
    /// The X coordinate of the top-left corner of this rectangle.
    /// </summary>
    public int X { get; set; } = 0;

    /// <summary>
    /// The Y coordinate of the top-left corner of this rectangle.
    /// </summary>
    public int Y { get; set; } = 0;

    /// <summary>
    /// The width of this rectangle.
    /// </summary>
    public int W { get; set; } = 0;

    /// <summary>
    /// The height of this rectangle.
    /// </summary>
    public int H { get; set; } = 0;

    /// <summary>
    /// The rectangle represented by this dataspec.
    /// </summary>
    public Rectangle Data => new(X, Y, W, H);
}

public class FrameSpec
{
    /// <summary>
    /// If this is a key into the Sprites dictionary of the parent collection, 
    /// it will be interpreted as the path of that sprite alias; otherwise, it 
    /// will be interpreted as a standalone sprite path.
    /// If it is null, it will be interpreted as the parent collection's default sprite.
    /// </summary>
    public string? Sprite { get; set; }

    /// <summary>
    /// The source area of the sprite to draw on this frame.
    /// This allows multiple frames to point to different areas of the same image.
    /// </summary>
    public RectangleSpec? Source { get; set; }
}

/// <summary>
/// The dataspec for an animation collection, which holds information about a group of 
/// animations which can be referenced by a string ID.
/// </summary>
public class AnimationCollectionSpec
{
    /// <summary>
    /// Aliases for the paths of sprites to be referenced in <see cref="Animations"/>.
    /// Keys should be the sprite aliases, while values should be the corresponding sprite paths
    /// relative to the base directory. 
    /// </summary>
    public Dictionary<string, string> Sprites { get; set; } = [];

    /// <summary>
    /// The collection of animations to load for this collection.
    /// Keys should be the IDs for each animation, while values should be the list of sprites
    /// which compose this animation.
    /// </summary>
    public Dictionary<string, FrameSpec[]> Animations { get; set; } = [];

    /// <summary>
    /// The number of milliseconds for which to display each frame.
    /// The default is 200 milliseconds, or 5 frames per second.
    /// Since Monogame is set to 60 frames per second, 1000 / MillisPerFrame should evenly 
    /// divide 60 in order to ensure smooth animation.
    /// </summary>
    public int MillisPerFrame { get; set; } = 200;

    /// <summary>
    /// The default sprite name of this collection. If it is found in <see cref="Sprites"/>, 
    /// the path with this key in that dictionary will be used; otherwise, it will
    /// be interpreted as a standalone path.
    /// </summary>
    public string DefaultSprite { get; set; } = "base";
}