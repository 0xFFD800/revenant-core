using Microsoft.Xna.Framework;
using RevenantCore.Entities.Spec;
using RevenantCore.Scenes;
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
    /// Applies behavior for a given run of the Tick loop.
    /// </summary>
    /// <param name="entity">The entity to which to apply behavior to.</param>
    /// <param name="scene">The scene in which behavior is being applied.</param>
    /// <param name="time">The FrameTime of the current frame.</param>
    void Apply(Entity entity, Scene scene, FrameTime time);
}

/// <summary>
/// An agent which does absolutely nothing.
/// The result is a stationary entity which does not move on its own.
/// </summary>
public class NullAgent : IAgent
{
    public bool IsDead => false;

    public void Apply(Entity entity, Scene scene, FrameTime time) { }

    public void Create(Scene scene, FrameTime time) { }

    public void Glean(Scene scene, FrameTime time) { }
}

/// <summary>
/// An agent which bases its movements off a tracker.
/// </summary>
/// <param name="tracker">The tracker which will feed this agent target information.</param>
/// <param name="acceleration">The acceleration to apply each tick towards the tracked goal.</param>
/// <param name="topSpeed">The top speed at which this agent is allowed to travel.</param>
public class TrackingAgent(Tracker<Vector3> tracker, float acceleration, float topSpeed) : IAgent
{
    public bool IsDead => tracker.IsDead;

    public void Apply(Entity entity, Scene scene, FrameTime time)
    {
        tracker.Tick(scene, time);
        Vector3 acc = tracker.CurrValue - entity.Position;
        acc.Normalize();
        acc *= acceleration;
        if ((entity.Velocity + acc).Length() < topSpeed)
            entity.Acceleration += acc;
    }

    public void Create(Scene scene, FrameTime time)
    {
        tracker.Create(scene, time);
    }

    public void Glean(Scene scene, FrameTime time)
    {
        tracker.Glean(scene, time);
    }
}

/// <summary>
/// An agent which bases its movement off control inputs.
/// </summary>
/// <param name="spec">The spec which defines this agent's parameters.</param>
public class InputAgent(InputAgentSpec spec) : IAgent
{
    public bool IsDead => false;

    private static bool IsPressed(Scene scene, string control) => 
        scene.GetControlState(control).Position is ControlPositions.Press or ControlPositions.Down;

    public void Apply(Entity entity, Scene scene, FrameTime time)
    {
        Vector3 acc = Vector3.Zero;
        if (IsPressed(scene, spec.Left)) 
            acc -= Vector3.UnitX * spec.Acceleration;
        if (IsPressed(scene, spec.Right)) 
            acc += Vector3.UnitX * spec.Acceleration;
        if (IsPressed(scene, spec.Up)) 
            acc -= Vector3.UnitZ * spec.Acceleration;
        if (IsPressed(scene, spec.Down)) 
            acc += Vector3.UnitZ * spec.Acceleration;
        if ((entity.Velocity + acc).Length() < spec.TopSpeed)
            entity.Acceleration += acc;
    }

    public void Create(Scene scene, FrameTime time) { }

    public void Glean(Scene scene, FrameTime time) { }
}