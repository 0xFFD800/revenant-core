using Microsoft.Xna.Framework;
using RevenantCore.Entities.Spec;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Entities;

/// <summary>
/// A collideable object which handles its own dynamic behavior and drawing behavior.
/// </summary>
/// <param name="spec">The spec defining data for this entity.</param>
/// <param name="animations">The collection of animations used to find the sprites to draw.</param>
public class Entity(EntitySpec spec, AnimationCollection animations) : ICollideable, ITickable, IVisible
{
    private Vector3 bounds = spec.Bounds.Data;

    /// <summary>
    /// The agent which controls this entity's behavior.
    /// </summary>
    public IAgent Agent { get; set; } = spec.Agent.Create();

    public BoundingBox CollisionBox => new(
        Position - new Vector3(bounds.X / 2, 0, bounds.Z / 2),
        Position + new Vector3(bounds.X / 2, bounds.Y, bounds.Z / 2));
    public MaterialSpec Material => spec.Material;
    public Vector3 Acceleration { get; set; } = new();
    public Vector3 Velocity { get; set; } = new();
    public Vector3 Position { get; set; } = new();
    public bool IsDead { get; set; } = false;
    public DrawLayer Layer => spec.Layer;
    public float Z => Position.Z;

    public string ID => spec.Id;

    public void Create(Scene scene, FrameTime time)
    {
        Agent.Create(scene, time);
    }

    public virtual void Draw(View view, Camera camera)
    {
        view.Screen.Draw(animations.GetFrame(Agent.Animation, view.Millis)
            .SetBase(camera.Project(Position))
            .RotateAroundCenter());
    }

    public void Glean(Scene scene, FrameTime time)
    {
        Agent.Glean(scene, time);
    }

    public void Tick(Scene scene, FrameTime time)
    {
        Agent.Apply(this, scene, time);
    }
}