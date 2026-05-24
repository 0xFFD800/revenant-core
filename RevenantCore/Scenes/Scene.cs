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
        Vector3 totalAcc = c.Acceleration * (float)millis;
        return c.Position + totalVel + (0.5F * totalAcc * totalAcc.Abs());
    }

    private record struct Collision(double RemMillis, Vector3 Friction);

    private static double FindMidpoint(ICollideable c1, ICollideable c2, BoundingBox intersection)
    {
        Vector3 c1c = c1.CollisionBox.Center;
        Vector3 c2c = c2.CollisionBox.Center;
        Vector3 ic = intersection.Center;
        Ray rc1f = new(c1c, ic - c1c);
        Ray rc2f = new(c2c, ic - c2c);
        Ray rc1b = new(ic, ic - c1c);
        Ray rc2b = new(ic, ic - c2c);
        float m1 = (intersection.Intersects(rc1f) - intersection.Intersects(rc1b)) ?? 0;
        float m2 = (intersection.Intersects(rc2f) - intersection.Intersects(rc2b)) ?? 0;
        return m1 + ((m2 - m1) / 2);
    }

    private static void UpdateFriction(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 normal1 = first.Velocity;
        Vector3 normal2 = second.Velocity;
        normal1.Normalize();
        normal2.Normalize();
        curr1.Friction *= normal1 * (1 - second.Material.Friction);
        curr2.Friction *= normal2 * (1 - first.Material.Friction);
    }

    private static void HandleSlide(BoundingBox? intersection, ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Trace.Assert(intersection.HasValue);
        throw new NotImplementedException();
    }

    private static void HandleCollide(BoundingBox intersection, ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        double midpoint = FindMidpoint(first, second, intersection);

        first.Position = GetNextPos(first, midpoint);
        second.Position = GetNextPos(second, midpoint);

        float? m1 = first.Material.Mass;
        float? m2 = second.Material.Mass;
        float massRatio1 = m1.HasValue && m2.HasValue ? m2.Value / m1.Value : m1.HasValue ? 1 : 0;
        float massRatio2 = m1.HasValue && m2.HasValue ? m1.Value / m2.Value : m2.HasValue ? 1 : 0;
        float? absorption = first.Material.MaterialAbsorption * second.Material.MaterialAbsorption;
        if (!absorption.HasValue)
            first.Velocity = second.Velocity = first.Acceleration = second.Acceleration = Vector3.Zero;
        else
        {
            first.Velocity = second.Velocity * massRatio1 / absorption.Value;
            second.Velocity = first.Velocity * massRatio2 / absorption.Value;
            first.Acceleration = second.Acceleration * massRatio1 / absorption.Value;
            second.Acceleration = first.Acceleration * massRatio2 / absorption.Value;
        }
        curr1.RemMillis -= midpoint;
        curr2.RemMillis -= midpoint;
    }

    private static void ApplyCollisions(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 np1 = GetNextPos(first, curr1.RemMillis) - first.Position;
        Vector3 np2 = GetNextPos(second, curr2.RemMillis) - second.Position;
        if ((first.CollisionBox + np1).FindIntersection(second.CollisionBox + np2, out BoundingBox? intersection))
        {
            Trace.Assert(intersection.HasValue);
            bool sf1 = np1.Length() < second.Material.StaticFriction;
            bool sf2 = np2.Length() < first.Material.StaticFriction;
            if (sf1)
            {
                first.Velocity = Vector3.Zero;
                first.Acceleration = Vector3.Zero;
            }
            if (sf2)
            {
                second.Velocity = Vector3.Zero;
                second.Acceleration = Vector3.Zero;
            }
            if (sf1 && sf2)
                return;
            UpdateFriction(first, second, ref curr1, ref curr2);
            if (intersection.Value.IsEmpty)
                return;
            if (first.CollisionBox.FindIntersection(second.CollisionBox, out BoundingBox? currIntersection))
                HandleSlide(currIntersection, first, second, ref curr1, ref curr2);
            else
                HandleCollide(intersection.Value, first, second, ref curr1, ref curr2);
        }
    }

    private static void ApplyFriction(ICollideable c, Collision cl)
    {
        Vector3 a = c.Acceleration.Abs();
        c.Acceleration = c.Acceleration.Sign() * (a - (c.Velocity * (Vector3.One - cl.Friction) / (float)cl.RemMillis).Abs().Clamp(Vector3.Zero, a));
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