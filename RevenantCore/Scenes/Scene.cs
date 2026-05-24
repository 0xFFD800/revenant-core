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

    private static double FindMidpoint(ICollideable first, ICollideable second, float ratio) => throw new NotImplementedException();

    private static void UpdateFriction(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 normal1 = first.Velocity;
        Vector3 normal2 = second.Velocity;
        normal1.Normalize();
        normal2.Normalize();
        curr1.Friction *= normal1 * (1 - second.Material.Friction);
        curr2.Friction *= normal2 * (1 - first.Material.Friction);
    }

    private static void ApplyCollisions(ICollideable first, ICollideable second, ref Collision curr1, ref Collision curr2)
    {
        Vector3 np1 = GetNextPos(first, curr1.RemMillis) - first.Position;
        Vector3 np2 = GetNextPos(second, curr2.RemMillis) - second.Position;
        if ((first.CollisionBox + np1).Intersects(second.CollisionBox + np2))
        {
            double midpoint;
            if (np1.Length() == 0)
            {
                if (np2.Length() == 0)
                    return;
                midpoint = FindMidpoint(second, first, np1.Length() / np2.Length());
            }
            else
                midpoint = FindMidpoint(first, second, np2.Length() / np1.Length());

            first.Position = GetNextPos(first, midpoint);
            second.Position = GetNextPos(second, midpoint);
            if (!first.Material.Mass.HasValue)
                throw new NotImplementedException();
            else if (!second.Material.Mass.HasValue)
                throw new NotImplementedException();
            else 
            {
                float massRatio = first.Material.Mass.Value / second.Material.Mass.Value;
                float? absorption = first.Material.MaterialAbsorption * second.Material.MaterialAbsorption;
                if (!absorption.HasValue)
                    first.Velocity = second.Velocity = first.Acceleration = second.Acceleration = Vector3.Zero;
                else
                {
                    first.Velocity = second.Velocity / massRatio / absorption.Value; 
                    second.Velocity = first.Velocity * massRatio / absorption.Value;
                    first.Acceleration = second.Acceleration / massRatio / absorption.Value; 
                    second.Acceleration = first.Acceleration * massRatio / absorption.Value;
                }
            }
            UpdateFriction(first, second, ref curr1, ref curr2);
            curr1.RemMillis -= midpoint;
            curr2.RemMillis -= midpoint;
        } else if (first.CollisionBox.Expand(0.1F).Intersects(second.CollisionBox.Expand(0.1F)))
            UpdateFriction(first, second, ref curr1, ref curr2);
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