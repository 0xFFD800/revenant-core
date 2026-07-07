using Microsoft.Xna.Framework;

namespace RevenantCore.Scenes.Spec;

/// <summary>
/// Specifies common parameters used by trackers of Vector3 values.
/// </summary>
public abstract class Vec3TrackerSpec
{
    /// <summary>
    /// The depth of the queue to maintain (i.e., how many positions to track).
    /// </summary>
    public uint Depth { get; set; } = 1;

    /// <summary>
    /// The interval between enqueueing new positions, in milliseconds.
    /// </summary>
    public double Interval { get; set; } = 100;

    /// <summary>
    /// The maximum speed at which the tracker will move.
    /// </summary>
    public double Speed { get; set; } = 0.1;

    /// <summary>
    /// The distance at which to begin decelerating the tracker's speed to zero.
    /// A value of 1 means no smoothing; values less than 1 are not allowed.
    /// </summary>
    public double Smoothing { get; set; } = 1;

    /// <summary>
    /// Creates a tracker for the values in this spec.
    /// </summary>
    /// <returns>The tracker represented by this spec.</returns>
    public abstract Tracker<Vector3> Create();
}