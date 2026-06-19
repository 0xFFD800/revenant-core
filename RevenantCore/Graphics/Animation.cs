using System;
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
    public Drawable GetFrame(FrameTime time) => sprites[(int) Math.Floor(time.Millis / millisPerFrame) % sprites.Length];
}