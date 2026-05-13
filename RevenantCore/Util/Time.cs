using Microsoft.Xna.Framework;

namespace RevenantCore.Util;

/// <summary>
/// A wrapper record around the standard GameTime record which aliases the most commonly used properties for convenience.
/// </summary>
/// <param name="GameTime">The time record to base all this record's values off.</param>
public record FrameTime(GameTime GameTime)
{
    /// <summary>
    /// The total number of milliseconds which have elapsed since the game began running.
    /// </summary>
    public double Millis => GameTime.TotalGameTime.TotalMilliseconds;

    /// <summary>
    /// The total number of milliseconds which have elapsed since the previous frame.
    /// </summary>
    public double MillisElapsed => GameTime.ElapsedGameTime.TotalMilliseconds;
}