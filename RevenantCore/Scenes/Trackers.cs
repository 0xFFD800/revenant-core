using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

public abstract class Tracker<T>(T initialValue, int queueDepth, double queueInterval) : ITickable
    where T : notnull, IEquatable<T>
{
    private double lastUpdate = 0;
    private readonly Queue<T> queue = [];
    private bool hasCurrTarget = false;
    private T? currTarget;

    public bool IsDead => false;
    public T CurrValue { get; private set; } = initialValue;
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
        if (time.Millis - lastUpdate > queueInterval)
        {
            T nextTarget = NextTarget;
            if (QueueEmpty || !queue.Last().Equals(nextTarget))
                queue.Enqueue(nextTarget);
            if (queue.Count > queueDepth)
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

public class MoveableTracker(IMoveable toTrack, int queueDepth, double queueInterval, double speed, double smoothing) : Tracker<Vector3>(toTrack.Position, queueDepth, queueInterval)
{
    protected override Vector3 NextTarget => toTrack.Position;

    protected override Vector3 Interpolate(Vector3 current, Vector3 target, FrameTime time)
    {
        double dist = time.MillisElapsed * speed;
        Vector3 trip = target - current;
        float length = trip.Length();
        trip.Normalize();
        if (length > dist * smoothing || QueueEmpty)
            return current + trip * (float)dist;
        else if (length <= dist && QueueEmpty)
            return target;
        else
            return current + trip * (length / (float)smoothing);
    }
}