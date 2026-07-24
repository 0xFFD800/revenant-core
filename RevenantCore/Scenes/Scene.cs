using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Cutscenes;
using RevenantCore.Entities;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents a gameplay area where entities exist and can interact.
/// </summary>
/// <param name="universe">The universe in which this scene exists.</param>
/// <param name="spec">The spec containing data for this scene.</param>
/// <param name="trigger">The trigger which this scene was created with.</param>
public class Scene(Universe universe, SceneSpec spec, string trigger) : Scythe
{
    /// <summary>
    /// All visible objects in this scene, organized by <see cref="IVisible.Layer"/>.
    /// Each sublist should be sorted by <see cref="IVisible.Z"/>. 
    /// </summary>
    private readonly OrderedDict<DrawLayer, IVisible> visibles = new();

    /// <summary>
    /// All tickable objects in this scene.
    /// All items in this list will be updated each tick.
    /// </summary>
    private readonly List<ITickable> tickables = [];

    /// <summary>
    /// The moveable objects in this scene, organized by their unique identifier.
    /// </summary>
    private readonly Dictionary<string, IMoveable> moveables = [];

    /// <summary>
    /// All objects in this view with collision boxes.
    /// </summary>
    private readonly List<ICollideable> collideables = [];

    /// <summary>
    /// A dictionary of the walls of this scene.
    /// The walls should also be added to <see cref="collideables"/>, as well as the scene's mortal tracker.
    /// </summary>
    private readonly FrozenDictionary<WallSide, Wall> walls = Enum.GetValues<WallSide>()
        .Select(k => new KeyValuePair<WallSide, Wall>(k, new Wall(k, spec)))
        .ToFrozenDictionary();

    /// <summary>
    /// A collection of cameras for all draw layers of this scene.
    /// </summary>
    private readonly CameraCollection cameras = new(spec.ViewportSize.Data, new(spec.Bounds.X, spec.Bounds.Y));

    /// <summary>
    /// The stack of active control capturers.
    /// Children of a cutscene block or UI container are not added to this stack--only separate ones triggered by the active chain.
    /// </summary>
    private readonly Stack<IControllable> controlCapture = [];

    /// <summary>
    /// The control tracker for this scene, updated each tick.
    /// </summary>
    private readonly ControlTracker controlTracker = new(), keyboardTracker = new KeyboardTracker();

    public override bool IsDead => false;
    public Universe Universe => universe;

    private void DoPhysics(double millis)
    {
        PhysicalObject[] objects = [.. collideables.Select(c => new PhysicalObject(c, millis))];
        foreach (PhysicalObject o in objects)
            o.ApplyGravity(spec.Gravity);

        for (int i = 0; i < objects.Length; i++)
            for (int j = i + 1; j < objects.Length; j++)
                objects[i].ApplyCollisions(objects[j]);

        foreach (PhysicalObject o in objects)
            o.Move();
    }

    /// <summary>
    /// Gets the current value of the specified wall's Suspended flag.
    /// </summary>
    /// <param name="side">The wall side to get the flag for.</param>
    /// <returns>Whether this wall is suspended or not (i.e., whether or not it currently accepts collisions).</returns>
    public bool IsSuspended(WallSide side) => walls[side].Suspended;

    /// <summary>
    /// Sets the Suspended flag of the wall on the specified side.
    /// </summary>
    /// <param name="side">The side of the wall to set the suspended flag on.</param>
    /// <param name="suspended">The value to set the wall's suspended flag to.</param>
    public void SetSuspended(WallSide side, bool suspended)
    {
        walls[side].Suspended = suspended;
    }

    private bool IsCapturing(IControllable? controllable) =>
        !controlCapture.TryPeek(out IControllable? capturer) 
            || (controllable != null && capturer.Matches(controllable));

    /// <summary>
    /// Gets the state of the specified control.
    /// </summary>
    /// <param name="controllable">The item testing for the specified control.</param>
    /// <param name="control">The control to find the state of.</param>
    /// <returns>The state of the specified control, if it is tracked; otherwise, returns Up.</returns>
    public ControlState GetControlState(IControllable? controllable, string control) => IsCapturing(controllable) 
        ? controlTracker.States.GetValueOrDefault(control, new(ControlPositions.Up, 0))
        : new(ControlPositions.Up, 0);
    
    public ControlState GetControlState(string control) => GetControlState(null, control);

    public string[] GetPressedKeys(IControllable? controllable) => IsCapturing(controllable) 
        ? [..keyboardTracker.States.Where(s => s.Value.Position == ControlPositions.Press || s.Value.Millis > KeyboardTracker.RepeatMillis).Select(s => s.Key)]
        : [];

    /// <summary>
    /// Attempts to find a moveable object for a given ID within a scene.
    /// </summary>
    /// <param name="id">The identifier to find a moveable object for.</param>
    /// <param name="moveable">The moveable object with the given identifier, if it exists.</param>
    /// <returns>Whether the moveable object was successfully found.</returns>
    public bool TryGetMoveable(string id, out IMoveable? moveable) => moveables.TryGetValue(id, out moveable);

    public override void Create(Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        foreach (Wall wall in walls.Values)
            Add(wall, scene, time);

        Add(controlTracker, scene, time);

        if (spec.Triggers.TryGetValue(trigger, out CutsceneSpec? intro))
        {
            Cutscene cutscene = intro.Create(universe);
            if (cutscene.IsDead)
                cutscene.Glean(scene, time);
            else
                Add(intro.Create(universe), scene, time);
        }
    }

    public void Draw(View view)
    {
        Camera camera = cameras.Get(view.Layer);
        view.Screen.Push(camera.Transform);
        foreach (IVisible visible in visibles.Get(view.Layer))
            visible.Draw(view, camera);
        view.Screen.Pop();
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        base.Tick(scene, time);
        Trace.Assert(scene == this);
        visibles.Sort((x, y) => Math.Sign(y.Z - x.Z));
        foreach (ITickable tickable in tickables)
            tickable.Tick(scene, time);
        DoPhysics(time.MillisElapsed);
    }

    public override void Add(IMortal mortal, Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        base.Add(mortal, scene, time);
        if (mortal is IVisible visible)
            visibles.Add(visible.Layer, visible);
        if (mortal is ITickable tickable)
            tickables.Add(tickable);
        if (mortal is IMoveable moveable)
            moveables.Add(moveable.ID, moveable);
        if (mortal is ICollideable collideable)
            collideables.Add(collideable);
        if (mortal is Cutscene cutscene)
            controlCapture.Push(cutscene);
    }

    protected override void Reap(IMortal mortal, Scene scene, FrameTime time)
    {
        Trace.Assert(scene == this);
        base.Reap(mortal, scene, time);
        if (mortal is IVisible visible)
            visibles.Remove(visible.Layer, visible);
        if (mortal is ITickable tickable)
            tickables.Remove(tickable);
        if (mortal is IMoveable moveable)
            moveables.Remove(moveable.ID);
        if (mortal is ICollideable collideable)
            collideables.Remove(collideable);
        if (mortal is Cutscene)
            controlCapture.Pop();
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
        WallSide.Floor => new(-scene.Bounds.X, -scene.Bounds.Y * 3, -scene.Bounds.Z),
        WallSide.Near => Vector3.UnitZ * -scene.Bounds.Z,
        WallSide.Far => Vector3.UnitZ * scene.Bounds.Z,
        WallSide.Left => Vector3.UnitX * -scene.Bounds.X,
        WallSide.Right => Vector3.UnitX * scene.Bounds.X,
        _ => throw new ArgumentOutOfRangeException("Unsupported side " + Enum.GetName(side))
    };
    private readonly Vector3 bounds = side == WallSide.Floor ? scene.Bounds.Data * 3 : scene.Bounds.Data;
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
    public Vector3 Position { get => origin + new Vector3(bounds.X / 2, 0, bounds.Z / 2); set { } }

    public bool IsDead => false;

    public string ID => "wall" + Enum.GetName(side);

    public void Create(Scene scene, FrameTime time)
    {
        Suspended = false;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        Suspended = true;
    }
}
