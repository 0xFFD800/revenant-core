using System;
using System.Collections.Frozen;
using RevenantCore.Util;

namespace RevenantCore.Graphics;

/// <summary>
/// An animation which cycles through a list of provided sprites, displaying each
/// of them in order.
/// </summary>
/// <param name="sprites">The list of sprites to cycle through.</param>
/// <param name="millisPerFrame">How many milliseconds to spend on each frame.</param>
public class Animation(Drawable[] sprites, int millisPerFrame)
{
    /// <summary>
    /// Gets the animation frame to display for the current time.
    /// This object does not track position or rotation information;
    /// those will have to be added by client code.
    /// </summary>
    /// <param name="time">The time of the frame currently being drawn.</param>
    /// <returns>The animation frame to display for the provided frame time.</returns>
    public Drawable GetFrame(FrameTime time) => sprites[(int)Math.Floor(time.Millis / millisPerFrame) % sprites.Length];
}

/// <summary>
/// A collection of animations which can be queried for specific keys.
/// </summary>
/// <param name="animations">The dictionary of animations to be queried for frames.</param>
/// <param name="defAnim">The animation to default to if the requested one is not found.</param>
public class AnimationCollection(FrozenDictionary<string, Animation> animations, string? defAnim)
{
    /// <summary>
    /// Queries the animation collection for an animation, then finds that animation's frame for the given time.
    /// </summary>
    /// <param name="key">The key to query the animation dictionary for.</param>
    /// <param name="time">The time to find the animation's current frame for.</param>
    /// <returns>The frame to be drawn for the given animation key and time.</returns>
    /// <exception cref="ArgumentException">Thrown if the given animation key does not exist and this collection does not have a default.</exception>
    public Drawable GetFrame(string key, FrameTime time) =>
        (animations.TryGetValue(key, out Animation? animation)
            || (defAnim != null && animations.TryGetValue(defAnim, out animation)))
        ? animation.GetFrame(time)
        : throw new ArgumentException("No animations were found for the requested key and no default was found", nameof(key));
}