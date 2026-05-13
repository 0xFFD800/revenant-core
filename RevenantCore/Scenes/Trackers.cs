using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

public class Tracker<T>(T initialValue, int queueDepth, double queueInterval, Func<T> supplier, Func<T, T, FrameTime, T> interpolate) : ITickable
    where T : notnull, IEquatable<T>
{
    private double lastUpdate = 0;
    private readonly Queue<T> queue = [];
    private bool hasCurrTarget = false;
    private T? currTarget;

    public bool IsDead => false;
    public T CurrValue { get; private set; } = initialValue;

    public void Create(Scene scene, FrameTime time)
    {
        queue.Enqueue(supplier());
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
            T nextTarget = supplier();
            if (queue.Count == 0 || !queue.Last().Equals(nextTarget))
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
            CurrValue = interpolate(CurrValue, currTarget, time);
            if (CurrValue.Equals(currTarget))
                hasCurrTarget = false;
        }
    }
}

public class MoveableTracker(IMoveable toTrack, int queueDepth, double queueInterval, double speed) : Tracker<Vector3>(toTrack.Position, queueDepth, queueInterval,
    () => toTrack.Position,
    (current, target, time) =>
    {
        double dist = time.MillisElapsed * speed;
        Vector3 trip = target - current;
        if (trip.Length() < dist)
            return target;
        else
        {
            trip.Normalize();
            return current + trip * (float)dist;
        }
    });