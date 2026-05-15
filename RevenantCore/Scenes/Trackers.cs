using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

public abstract class Tracker<T>(uint depth, double interval) : ITickable
    where T : notnull, IEquatable<T>
{
    private double lastUpdate = 0;
    private readonly Queue<T> queue = [];
    private bool hasCurrTarget, hasCurrValue = false;
    private T? currTarget, currValue;

    public abstract bool IsDead { get; }
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

    public void Create(Scene scene, FrameTime time)
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
/// A tracker which follows a moveable through 3D space within a scene.
/// </summary>
/// <param name="toTrack">The moveable object to track.</param>
/// <param name="depth">The depth of the queue to maintain (i.e., how many positions to track).</param>
/// <param name="interval">The interval between enqueueing new positions, in milliseconds.</param>
/// <param name="speed">The maximum speed at which the tracker will move.</param>
/// <param name="smoothing">
/// The distance at which to begin decelerating the tracker's speed to zero.
/// A value of 1 means no smoothing; values less than 1 are not allowed.
/// </param>
public class MoveableTracker(IMoveable toTrack, uint depth, double interval, double speed, double smoothing) : Tracker<Vector3>(depth, interval)
{
    public override bool IsDead => toTrack.IsDead;
    protected override Vector3 NextTarget => toTrack.Position;

    protected override Vector3 Interpolate(Vector3 current, Vector3 target, FrameTime time)
    {
        double dist = time.MillisElapsed * speed;
        Vector3 trip = target - current;
        float length = trip.Length();
        trip.Normalize();
        double smoothedDist = !QueueEmpty
                || length > smoothing
                || length < dist / smoothing
            ? dist : length / smoothing;
        if (length > smoothedDist)
            return current + trip * (float)smoothedDist;
        else
            return target;
    }
}

/// <summary>
/// A tracker which follows a collideable through 3D space, targeting positions it is likely to move to next based on its velocity.
/// </summary>
/// <param name="toTrack">The collideable object to track.</param>
/// <param name="depth">The depth of the queue to maintain (i.e., how many positions to track).</param>
/// <param name="interval">The interval between enqueueing new positions, in milliseconds.</param>
/// <param name="speed">The maximum speed at which the tracker will move.</param>
/// <param name="smoothing">The distance at which to begin decelerating the tracker's speed to zero.</param>
/// <param name="velocityFactor">
/// The factor by which to multiply <paramref name="toTrack"/>.Velocity.
/// This is equivalent to the number of milliseconds until toTrack will be at the next target position, assuming its velocity does not change.
/// </param>
public class ForwardLookingTracker(ICollideable toTrack, uint depth, double interval, double speed, double smoothing, float velocityFactor) : MoveableTracker(toTrack, depth, interval, speed, smoothing)
{
    protected override Vector3 NextTarget => base.NextTarget + (toTrack.Velocity * velocityFactor);
}