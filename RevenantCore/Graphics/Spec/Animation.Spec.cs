using System.Collections.Generic;

namespace RevenantCore.Graphics.Spec;

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
    /// If the value is a key into Sprites, it will be interpreted as the path of that sprite
    /// alias; otherwise, the value will be interpreted as a standalone sprite path.
    /// </summary>
    public Dictionary<string, string[]> Animations { get; set; } = [];
}