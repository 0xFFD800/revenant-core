using System;
using System.Collections.Frozen;
using System.Numerics;
using RevenantCore.Scenes;
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
    /// <param name="millis">The time in milliseconds of the frame currently being drawn.</param>
    /// <returns>The animation frame to display for the provided frame time.</returns>
    public Drawable GetFrame(double millis) => sprites[(int)Math.Floor(millis / millisPerFrame) % sprites.Length];
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
    public Drawable GetFrame(string key, double millis) =>
        (animations.TryGetValue(key, out Animation? animation)
            || (defAnim != null && animations.TryGetValue(defAnim, out animation)))
        ? animation.GetFrame(millis)
        : throw new ArgumentException("No animations were found for the requested key and no default was found", nameof(key));
}

/// <summary>
/// A hook which applies an extrinsic animation onto drawables of any type.
/// </summary>
public interface IAnimationHook : IMortal
{
    /// <summary>
    /// Applies the animation hook to a drawable.
    /// </summary>
    /// <param name="drawable">The drawable object on which to apply this animation hook.</param>
    public void Apply(Drawable drawable);
}

/// <summary>
/// An animation which fades in opacity from clear to opaque or vice versa.
/// </summary>
/// <param name="lengthMillis">The length, in milliseconds, of the fade animation.</param>
/// <param name="reverse">If false, fade from clear to opaque; otherwise, fade in the other direction.</param>
public class FadeAnimation(double lengthMillis, bool reverse) : IAnimationHook
{
    private double counter = 0;

    public bool IsDead => counter >= lengthMillis;

    public void Apply(Drawable drawable)
    {
        float opacity = (float)(counter++ / lengthMillis);
        drawable.SetOpacity(reverse ? 1 - opacity : opacity);
    }

    public void Create(Scene scene, FrameTime time)
    {
        counter = 0;
    }

    public void Glean(Scene scene, FrameTime time) { }
}

/// <summary>
/// An animation which moves along a straight line.
/// Note that this animation assumes the drawable being animated is a copy rather than the original.
/// </summary>
/// <param name="lengthMillis">The length, in milliseconds, of the movement animation.</param>
/// <param name="trip">The line from the drawable's current positions which the animation should trace</param>
public class MoveAnimation(double lengthMillis, Vector2 trip) : IAnimationHook
{
    private double counter = 0;

    public bool IsDead => counter >= lengthMillis;

    public void Apply(Drawable drawable)
    {
        float ratio = (float)(counter++ / lengthMillis);
        drawable.Pos += trip * ratio;
    }

    public void Create(Scene scene, FrameTime time)
    {
        counter = 0;
    }

    public void Glean(Scene scene, FrameTime time) { }
}

/// <summary>
/// An animation which rotates along a pre-defined arc.
/// Rotates around whatever point is already defined for this drawable.
/// </summary>
/// <param name="lengthMillis">The length, in milliseconds, of the movement animation.</param>
/// <param name="radians">The angle which the animation should trace out.</param>
public class RotateAnimation(double lengthMillis, float radians) : IAnimationHook
{
    private double counter = 0;

    public bool IsDead => counter >= lengthMillis;

    public void Apply(Drawable drawable)
    {
        double ratio = counter++ / lengthMillis;
        drawable.Rotation += (float)(ratio * radians);
    }

    public void Create(Scene scene, FrameTime time)
    {
        counter = 0;
    }

    public void Glean(Scene scene, FrameTime time) { }
}