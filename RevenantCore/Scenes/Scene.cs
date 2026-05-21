using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents a gameplay area where entities exist and can interact.
/// </summary>
public class Scene(SceneSpec spec) : Scythe
{
    /// <summary>
    /// All visible objects in this scene, organized by <see cref="IVisible.Layer"/>.
    /// Each sublist should be sorted by <see cref="IVisible.Z"/>. 
    /// </summary>
    private readonly OrderedDict<DrawLayer, IVisible> visibles = new();

    /// <summary>
    /// All objects in this view with collision boxes.
    /// </summary>
    private readonly List<ICollideable> collideables = [];

    public override bool IsDead => false;

    private void ApplyGravity(ICollideable c)
    {
        c.Acceleration -= Vector3.UnitY * spec.Gravity;
    }

    private static Vector3 GetNextPos(ICollideable c, double millis)
    {
        Vector3 totalVel = c.Velocity * (float)millis;
        Vector3 totalAcc = c.Acceleration * (float)millis;
        return c.Position + totalVel + (0.5F * totalAcc * totalAcc.Abs());
    }

    private record struct Collision(double RemMillis, Vector3 Friction);

    private static void ApplyCollisions(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 np1 = GetNextPos(first, curr1.RemMillis) - first.Position;
        Vector3 np2 = GetNextPos(second, curr2.RemMillis) - second.Position;
        if ((first.CollisionBox + np1).Intersects(second.CollisionBox + np2))
            throw new NotImplementedException();
        // Otherwise do nothing...?
    }

    private static void ApplyFriction(ICollideable c, Collision cl)
    {
        Vector3 a = c.Acceleration.Abs();
        c.Acceleration = c.Acceleration.Sign() * (a - (c.Velocity * cl.Friction / (float)cl.RemMillis).Abs().Clamp(Vector3.Zero, a));
    }

    private static void MoveObject(ICollideable c, double millis)
    {
        c.Position = GetNextPos(c, millis);
        c.Velocity += c.Acceleration * (float)millis;
        c.Acceleration = Vector3.Zero;
    }

    private void DoPhysics(double millis)
    {
        Collision[] collisions = [.. collideables.Select(c => new Collision(millis, Vector3.Zero))];
        for (int i = 0; i < collideables.Count; i++)
        {
            ICollideable ci = collideables[i];
            ApplyGravity(ci);
            for (int j = i + 1; j < collideables.Count; j++)
                ApplyCollisions(ci, collideables[j], ref collisions[i], ref collisions[j]);
        }

        for (int i = 0; i < collideables.Count; i++)
        {
            ICollideable c = collideables[i];
            ApplyFriction(c, collisions[i]);
            MoveObject(c, collisions[i].RemMillis);
        }
    }

    public override void Create(Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
    }

    public void Draw(View view)
    {
        // TODO: apply matrix to the SpriteBatch depending on the DrawLayer
        // view.Screen.Push(transform);
        foreach (IVisible visible in visibles.Get(view.Layer))
            visible.Draw(view);
        // view.Screen.Pop();
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        visibles.Sort(Comparer<IVisible>.Create((x, y) => (int)(y.Z - x.Z)));
        DoPhysics(time.MillisElapsed);
        base.Tick(scene, time);
    }

    public override void Add(IMortal mortal, Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        base.Add(mortal, scene, time);
        if (mortal is IVisible visible)
            visibles.Add(visible.Layer, visible);
        if (mortal is ICollideable collideable)
            collideables.Add(collideable);
    }

    protected override void Reap(IMortal mortal, Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        base.Reap(mortal, scene, time);
        if (mortal is IVisible visible)
            visibles.Remove(visible.Layer, visible);
        if (mortal is ICollideable collideable)
            collideables.Remove(collideable);
    }
}