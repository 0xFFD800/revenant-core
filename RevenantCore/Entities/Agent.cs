using System;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using RevenantCore.Entities.Spec;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Entities;

/// <summary>
/// An object which applies behavior to an entity.
/// Behavior may be applied in response to input, at random, 
/// based on the environment, or not at all.
/// </summary>
public interface IAgent : IMortal
{
    /// <summary>
    /// The animation which the entity should currently use if it is not overridden.
    /// </summary>
    string Animation { get; }

    /// <summary>
    /// Applies behavior for a given run of the Tick loop.
    /// </summary>
    /// <param name="entity">The entity to which to apply behavior to.</param>
    /// <param name="scene">The scene in which behavior is being applied.</param>
    /// <param name="time">The FrameTime of the current frame.</param>
    void Apply(Entity entity, Scene scene, FrameTime time);

    /// <summary>
    /// The interaction types which this agent causes its entity to trigger.
    /// </summary>
    InteractionType[] Interactions { get; }
}

/// <summary>
/// An agent which does absolutely nothing.
/// The result is a stationary entity which does not move on its own.
/// </summary>
public class NullAgent : IAgent
{
    public virtual string Animation => "idle";
    public bool IsDead => false;

    public InteractionType[] Interactions => [];

    public void Apply(Entity entity, Scene scene, FrameTime time) { }

    public void Create(Scene scene, FrameTime time) { }

    public void Glean(Scene scene, FrameTime time) { }
}

public abstract class WalkAgent : IAgent
{
    protected Vector3 movement = Vector3.Zero;

    public virtual string Animation
    {
        get
        {
            if (movement.X == 0 && movement.Z == 0)
                return "idle";

            double t = Math.Atan2(movement.Z, movement.X);
            if (t < 0)
                t = Math.PI - t;
            return t switch
            {
                var x when x >= Math.PI * 0.25 && x < Math.PI * 0.75 => "walkDown",
                var x when x >= Math.PI * 0.75 && x < Math.PI * 1.25 => "walkLeft",
                var x when x >= Math.PI * 1.25 && x < Math.PI * 1.75 => "walkUp",
                var x when x >= Math.PI * 1.75 || x < Math.PI * 0.25 => "walkRight",
                _ => "idle"
            };
        }
    }

    public abstract bool IsDead { get; }

    public virtual InteractionType[] Interactions => [];

    public abstract void Apply(Entity entity, Scene scene, FrameTime time);
    public abstract void Create(Scene scene, FrameTime time);
    public abstract void Glean(Scene scene, FrameTime time);
}

/// <summary>
/// An agent which bases its movements off a tracker.
/// </summary>
/// <param name="tracker">The tracker which will feed this agent target information.</param>
/// <param name="acceleration">The acceleration to apply each tick towards the tracked goal.</param>
/// <param name="topSpeed">The top speed at which this agent is allowed to travel.</param>
public class TrackingAgent(Tracker<Vector3> tracker, float acceleration, float topSpeed) : WalkAgent, IAgent
{
    public override bool IsDead => tracker.IsDead;

    public override void Apply(Entity entity, Scene scene, FrameTime time)
    {
        tracker.Tick(scene, time);
        movement = tracker.CurrValue - entity.Position;
        movement.Normalize();
        movement *= acceleration;
        if ((entity.Velocity + movement).Length() < topSpeed)
            entity.Acceleration += movement;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        tracker.Create(scene, time);
    }

    public override void Glean(Scene scene, FrameTime time)
    {
        tracker.Glean(scene, time);
    }
}

/// <summary>
/// An agent which bases its movement off control inputs.
/// </summary>
/// <param name="spec">The spec which defines this agent's parameters.</param>
public class InputAgent(InputAgentSpec spec) : WalkAgent, IAgent
{
    private bool interacting = false;

    public override bool IsDead => false;
    public override InteractionType[] Interactions => interacting ? [InteractionType.Enter, InteractionType.Interact] : [InteractionType.Enter];

    private static bool IsPressed(Scene scene, string control) =>
        scene.GetControlState(control).Position is ControlPositions.Press or ControlPositions.Down;

    public override void Apply(Entity entity, Scene scene, FrameTime time)
    {
        if (IsPressed(scene, spec.Controls.Left))
            movement -= Vector3.UnitX * spec.Acceleration;
        if (IsPressed(scene, spec.Controls.Right))
            movement += Vector3.UnitX * spec.Acceleration;
        if (IsPressed(scene, spec.Controls.Up))
            movement -= Vector3.UnitZ * spec.Acceleration;
        if (IsPressed(scene, spec.Controls.Down))
            movement += Vector3.UnitZ * spec.Acceleration;
        if ((entity.Velocity + movement).Length() < spec.TopSpeed)
            entity.Acceleration += movement;
        interacting = scene.GetControlState(spec.InteractControl).Position == ControlPositions.Press;
    }

    public override void Create(Scene scene, FrameTime time) { }

    public override void Glean(Scene scene, FrameTime time) { }
}