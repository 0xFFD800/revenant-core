using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

    /// <summary>
    /// A dictionary of the walls of this scene.
    /// The walls should also be added to <see cref="collideables"/>, as well as the scene's mortal tracker.
    /// </summary>
    private readonly ImmutableDictionary<WallSide, Wall> walls = Enum.GetValues<WallSide>()
        .Select(k => new KeyValuePair<WallSide, Wall>(k, new Wall(k, spec)))
        .ToImmutableDictionary();

    public override bool IsDead => false;

    private void ApplyGravity(ICollideable c)
    {
        c.Acceleration -= Vector3.UnitY * spec.Gravity;
    }

    private static Vector3 GetNextPos(ICollideable c, double millis)
    {
        Vector3 totalVel = c.Velocity * (float)millis;
        Vector3 totalAcc = c.Acceleration * (float)(millis * millis);
        return c.Position + totalVel + (0.5F * totalAcc);
    }

    private record struct Collision(double RemMillis, Vector3 Friction);

    private static float FindMidpoint(Vector3 trip, BoundingBox box, BoundingBox futureOther, BoundingBox intersection)
    {
        Vector3 ic = intersection.Center;
        Ray rc = new(ic - trip, trip);
        Ray rb = new(ic, -trip);
        float mc = futureOther.Intersects(rc) ?? 0;
        float mb = box.Intersects(rb) ?? 0;
        return Math.Min(mc, mb);
    }

    private static void UpdateFriction(ICollideable c, float friction, ref Collision cl)
    {
        Vector3 normal = c.Velocity;
        if (normal != Vector3.Zero)
        {
            normal.Normalize();
            cl.Friction *= normal * (1 - friction);
        }
    }
    
    private static void HandleReflection(ICollideable c, Vector3 v, Vector3 a, float? massRatio, float absorption, BoundingBox intersection)
    {
        if (massRatio.HasValue)
        {
            c.Velocity = v * massRatio.Value;
            c.Acceleration = a * massRatio.Value;
        }
        else
        {
            Vector3 b = intersection.Max - intersection.Min;
            Vector3 normal;
            if (b.X <= b.Y && b.X <= b.Z)
                normal = new(-1, 1, 1);
            else if (b.Y <= b.X && b.Y <= b.Z)
                normal = new(1, -1, 1);
            else
                normal = new(1, 1, -1);
            c.Velocity *= normal;
            c.Acceleration *= normal;
        }
        c.Velocity /= absorption;
        c.Acceleration /= absorption;
    }

    private static void HandleSlide(BoundingBox intersection, ICollideable first, ICollideable second)
    {
        // Determine the direction of the occlusion
        Vector3 b = intersection.Max - intersection.Min;
        float shift;
        Vector3 dir;
        if (b.X <= b.Y && b.X <= b.Z)
        {
            shift = b.X;
            dir = Vector3.UnitX;
        }
        else if (b.Y <= b.X && b.Y <= b.Z)
        {
            shift = b.Y;
            dir = Vector3.UnitY;
        }
        else
        {
            shift = b.Z;
            dir = Vector3.UnitZ;
        }

        // Eliminate occlusion by shifting according to mass ratio
        float? m1 = first.Material.Mass;
        float? m2 = second.Material.Mass;
        Vector3 sign = -(dir * (second.Position - first.Position)).Sign();
        if (sign == Vector3.Zero)
            sign = Vector3.One * dir;
        if (m1.HasValue && !m2.HasValue)
            first.Position += shift * dir * -sign;
        else if (m2.HasValue && !m1.HasValue)
            second.Position += shift * dir * sign;
        else if (m1.HasValue && m2.HasValue)
        {
            first.Position += shift * dir * (m1.Value / (m1.Value + m2.Value)) * -sign;
            second.Position += shift * dir * (m2.Value / (m1.Value + m2.Value)) * sign;
        }

        // Eliminate velocity and acceleration in the direction in which the occlusion had been
        dir = Vector3.One - dir;
        first.Velocity *= dir;
        first.Acceleration *= dir;
        second.Velocity *= dir;
        second.Acceleration *= dir;
    }

    private static void HandleCollide(BoundingBox intersection, ICollideable first, ICollideable second, Vector3 trip1, Vector3 trip2, ref Collision curr1, ref Collision curr2)
    {
        double p1 = FindMidpoint(trip1, first.CollisionBox, second.CollisionBox + trip2, intersection) * curr1.RemMillis;
        double p2 = FindMidpoint(trip2, second.CollisionBox, first.CollisionBox + trip1, intersection) * curr2.RemMillis;

        first.Position = GetNextPos(first, p1);
        second.Position = GetNextPos(second, p2);

        float? m1 = first.Material.Mass;
        float? m2 = second.Material.Mass;
        float? massRatio1 = m1.HasValue && m2.HasValue ? m2.Value / m1.Value : m1.HasValue ? null : 0;
        float? massRatio2 = m1.HasValue && m2.HasValue ? m1.Value / m2.Value : m2.HasValue ? null : 0;
        float? absorption = first.Material.MaterialAbsorption * second.Material.MaterialAbsorption;
        if (!absorption.HasValue)
            first.Velocity = second.Velocity = first.Acceleration = second.Acceleration = Vector3.Zero;
        else
        {
            Vector3 v1 = first.Velocity;
            Vector3 a1 = first.Acceleration;
            HandleReflection(first, second.Velocity, second.Acceleration, massRatio1, absorption.Value, intersection);
            HandleReflection(second, v1, a1, massRatio2, absorption.Value, intersection);
        }
        curr1.RemMillis -= p1;
        curr2.RemMillis -= p2;
    }

    private static void ApplyCollisions(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 np1 = GetNextPos(first, curr1.RemMillis) - first.Position;
        Vector3 np2 = GetNextPos(second, curr2.RemMillis) - second.Position;
        BoundingBox? futureInt = (first.CollisionBox + np1).FindIntersection(second.CollisionBox + np2);
        BoundingBox? currInt = first.CollisionBox.FindIntersection(second.CollisionBox);
        if (futureInt.HasValue || currInt.HasValue)
        {
            UpdateFriction(first, second.Material.Friction, ref curr1);
            UpdateFriction(second, first.Material.Friction, ref curr2);
            if (currInt.HasValue)
                HandleSlide(currInt.Value, first, second);
            else if (futureInt.HasValue && !futureInt.Value.IsEmpty)
                HandleCollide(futureInt.Value, first, second, np1, np2, ref curr1, ref curr2);
        }
    }

    private static Vector3 ApplyFrictionTo(Vector3 v, Collision cl) =>
        v.Sign() * (v.Abs() - (v.Abs() * (Vector3.One - cl.Friction.Abs())).Clamp(Vector3.Zero, v.Abs()));

    private static void ApplyFriction(ICollideable c, Collision cl)
    {
        c.Velocity = ApplyFrictionTo(c.Velocity, cl);
        c.Acceleration = ApplyFrictionTo(c.Acceleration, cl);

        // If this object's velocity after this tick will be less than its static friction, set its velocity and acceleration to zero now as it should not be moved
        if ((c.Velocity + c.Acceleration * (float)cl.RemMillis).Length() <= c.Material.StaticFriction)
            c.Velocity = c.Acceleration = Vector3.Zero;
    }

    private static void MoveObject(ICollideable c, double millis)
    {
        c.Position = GetNextPos(c, millis);
        c.Velocity += c.Acceleration * (float)millis;
        c.Acceleration = Vector3.Zero;
    }

    private void DoPhysics(double millis)
    {
        Collision[] collisions = [.. collideables.Select(c => new Collision(millis, Vector3.One))];
        // Only collideables with a defined mass should be affected by gravity.
        foreach (ICollideable c in collideables)
            if (c.Material.Mass.HasValue)
                ApplyGravity(c);

        for (int i = 0; i < collideables.Count; i++)
        {
            ICollideable ci = collideables[i];
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
        foreach (Wall wall in walls.Values)
            Add(wall, scene, time);
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
        base.Tick(scene, time);
        Trace.Assert(scene == this);
        visibles.Sort(Comparer<IVisible>.Create((x, y) => (int)(y.Z - x.Z)));
        DoPhysics(time.MillisElapsed);
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

/// <summary>
/// Walls are immovable collideables which can be suspended if need be.
/// </summary>
/// <param name="origin">The bottom near left corner of the collideable.</param>
/// <param name="bounds">The size of this wall in 3 dimensions.</param>
/// <param name="material">The material this wall is made out of.</param>
public class Wall(WallSide side, SceneSpec scene) : ICollideable
{
    private readonly Vector3 origin = side switch
    {
        WallSide.Floor => Vector3.UnitY * -scene.Bounds.Y,
        WallSide.Near => Vector3.UnitZ * -scene.Bounds.Z,
        WallSide.Far => Vector3.UnitZ * scene.Bounds.Z,
        WallSide.Left => Vector3.UnitX * -scene.Bounds.X,
        WallSide.Right => Vector3.UnitX * scene.Bounds.X,
        _ => throw new ArgumentOutOfRangeException("Unsupported side " + Enum.GetName(side))
    };
    private readonly Vector3 bounds = scene.Bounds.Data;
    private readonly MaterialSpec material = scene.Walls[side];

    /// <summary>
    /// If the wall is suspended, objects cannot collide with it.
    /// This is intended to allow entities to travel through a door on a specific wall.
    /// Walls should only be suspended during active cutscenes, not during gameplay.
    /// </summary>
    public bool Suspended { get; set; } = false;
    private Vector3 CurrBounds => Suspended ? Vector3.Zero : bounds;
    public BoundingBox CollisionBox => new(origin, origin + CurrBounds);

    public MaterialSpec Material => material;

    public Vector3 Acceleration { get => Vector3.Zero; set { } }
    public Vector3 Velocity { get => Vector3.Zero; set { } }
    public Vector3 Position { get => origin + (bounds / 2); set { } }

    public bool IsDead => false;

    public void Create(Scene scene, FrameTime time)
    {
        Suspended = false;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        Suspended = true;
    }
}