using System;
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

/// <summary>
/// The spec for a tracker which follows a moveable object.
/// </summary>
public class MoveableTrackerSpec : Vec3TrackerSpec
{
    /// <summary>
    /// The ID of the moveable object to follow.
    /// </summary>
    public string Moveable { get; set; } = "player";

    /// <summary>
    /// The position at which the tracker should be initialized before it identifies the object.
    /// </summary>
    public Vector3Spec InitialPos { get; set; } = new();

    public override Tracker<Vector3> Create() => new MoveableTracker(InitialPos.Data, this);
}

/// <summary>
/// The spec for a tracker which attempts to follow ahead of a collideable object.
/// Note that the target object must implement ICollideable (MoveableTracker only requires IMoveable).
/// </summary>
public class ForwardLookingTrackerSpec : MoveableTrackerSpec
{
    /// <summary>
    /// The factor by which to multiply the velocity of the tracked object.
    /// This is equivalent to the number of milliseconds until the tracked object 
    /// will be at the next target position, assuming its velocity does not change.
    /// </summary>
    public float VelocityFactor { get; set; } = 0;

    public override Tracker<Vector3> Create() => new ForwardLookingTracker(this);
}

/// <summary>
/// The spec for a tracker which picks random values within a certain range of a certain point.
/// </summary>
public class WanderTrackerSpec : Vec3TrackerSpec
{
    private readonly Random random = new();

    /// <summary>
    /// The position around which target points will be picked.
    /// </summary>
    public Vector3Spec Home { get; set; } = new();

    /// <summary>
    /// How far from Home target points should be allowed to deviate.
    /// </summary>
    public float Range { get; set; } = 50F;

    public override Tracker<Vector3> Create() => new WanderTracker(Home.Data, this, () => random.NextSingle() * Range);
}