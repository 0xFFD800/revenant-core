using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// Provides a sequence of tracked positions with interpolations between them each tick.
/// </summary>
/// <typeparam name="T">The type to provide from this tracker.</typeparam>
/// <param name="depth">The depth of the queue to provide positions from.</param>
/// <param name="interval">The interval at which to provide new positions.</param>
public abstract class Tracker<T>(uint depth, double interval) : ITickable
    where T : notnull, IEquatable<T>
{
    private double lastUpdate = 0;
    private readonly Queue<T> queue = [];
    private bool hasCurrTarget, hasCurrValue = false;
    private T? currTarget, currValue;

    public abstract bool IsDead { get; }

    /// <summary>
    /// The current value being provided by this tracker.
    /// Includes interpolation.
    /// </summary>
    public T CurrValue
    {
        get {
            currValue = !hasCurrValue ? NextTarget : currValue ?? NextTarget;
            hasCurrValue = true;
            return currValue;
        }
        private set => currValue = value;
    }
    protected abstract T NextTarget { get; }
    protected bool QueueEmpty => queue.Count == 0;

    public virtual void Create(Scene scene, FrameTime time)
    {
        queue.Enqueue(NextTarget);
        lastUpdate = time.Millis;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        queue.Clear();
    }

    public void Tick(Scene scene, FrameTime time)
    {
        if (time.Millis - lastUpdate > interval)
        {
            T nextTarget = NextTarget;
            if (QueueEmpty || !queue.Last().Equals(nextTarget))
                queue.Enqueue(nextTarget);
            if (queue.Count > depth)
            {
                currTarget = queue.Dequeue();
                hasCurrTarget = true;
            }
            lastUpdate = time.Millis;
        }

        if (currTarget != null && hasCurrTarget)
        {
            CurrValue = Interpolate(CurrValue, currTarget, time);
            if (CurrValue.Equals(currTarget))
                hasCurrTarget = false;
        }
    }

    protected abstract T Interpolate(T current, T target, FrameTime time);
}

/// <summary>
/// A tracker which provides Vector3s, adding smoothing when the tracker closes in on its target.  
/// </summary>
/// <param name="spec">The parameters this tracker should use to determine its behavior.</param>
public abstract class Vec3Tracker(Vec3TrackerSpec spec) : Tracker<Vector3>(spec.Depth, spec.Interval)
{
    protected override Vector3 Interpolate(Vector3 current, Vector3 target, FrameTime time)
    {
        double dist = time.MillisElapsed * spec.Speed;
        Vector3 trip = target - current;
        float length = trip.Length();
        trip.Normalize();
        double smoothedDist = !QueueEmpty
                || length > spec.Smoothing
                || length < dist / spec.Smoothing
            ? dist : length / spec.Smoothing;
        if (length > smoothedDist)
            return current + trip * (float)smoothedDist;
        else
            return target;
    }
}

/// <summary>
/// A tracker which follows a moveable through 3D space within a scene.
/// </summary>
/// <param name="spec">The parameters this tracker should use to determine its behavior.</param>
public class MoveableTracker(Vector3 initialPos, MoveableTrackerSpec spec) : Vec3Tracker(spec)
{
    private IMoveable? toTrack;
    private bool created = false;

    public MoveableTracker(IMoveable moveable, MoveableTrackerSpec spec) 
        : this(spec.InitialPos.Data, spec)
    {
        toTrack = moveable;
    }

    public override bool IsDead => toTrack?.IsDead ?? !created;
    protected override Vector3 NextTarget => toTrack?.Position ?? initialPos;

    public override void Create(Scene scene, FrameTime time)
    {
        base.Create(scene, time);
        if (toTrack == null)
            scene.TryGetMoveable(spec.Moveable, out toTrack);
        created = true;
    }
}

/// <summary>
/// A tracker which follows a collideable through 3D space, targeting positions it is likely to move to next based on its velocity.
/// </summary>
/// <param name="toTrack">The collideable object to track.</param>
/// <param name="spec">The parameters this tracker should use to determine its behavior.</param>
/// <param name="velocityFactor">
/// The factor by which to multiply <paramref name="toTrack"/>.Velocity.
/// This is equivalent to the number of milliseconds until toTrack will be at the next target position, assuming its velocity does not change.
/// </param>
public class ForwardLookingTracker(ICollideable toTrack, MoveableTrackerSpec spec, float velocityFactor) : MoveableTracker(toTrack, spec)
{
    protected override Vector3 NextTarget => base.NextTarget + (toTrack.Velocity * velocityFactor);
}

/// <summary>
/// A tracker which tracks positions around a home position based on a provider function which provides distances.
/// The provider function is intended to provide random positions, but could be used for other means.
/// This tracker is best suited to a shallow queue and broad interval.
/// </summary>
/// <param name="home">The home position which all targets will be based around.</param>
/// <param name="spec">The parameters this tracker should use to determine its behavior.</param>
/// <param name="numProvider">A provider function which determines the distance from <paramref name="home"/> to use for targets.</param>
public class WanderTracker(Vector3 home, Vec3TrackerSpec spec, Func<float> numProvider) : Vec3Tracker(spec)
{
    public override bool IsDead => false;

    protected override Vector3 NextTarget => home + new Vector3(numProvider(), numProvider(), numProvider());
}