using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Entities;

/// <summary>
/// A collideable object which handles its own dynamic behavior and drawing behavior.
/// </summary>
/// <param name="agent">The agent which controls this entity's behavior.</param>
/// <param name="animations">The collection of animations used to find the sprites to draw.</param>
/// <param name="material">The spec for the material which defines physics parameters for this entity.</param>
/// <param name="bounds">The size of this entity's bounding box along each axis.</param>
/// <param name="layer">The layer in which this entity should be drawn.</param>
public class Entity(IAgent agent, AnimationCollection animations, MaterialSpec material, Vector3 bounds, DrawLayer layer) : ICollideable, ITickable, IVisible
{
    /// <summary>
    /// The agent which controls this entity's behavior.
    /// </summary>
    public IAgent Agent { private get; set; } = agent;

    public BoundingBox CollisionBox => new(
        Position - new Vector3(bounds.X / 2, 0, bounds.Z / 2),
        Position + new Vector3(bounds.X / 2, bounds.Y, bounds.Z / 2));
    public MaterialSpec Material => material;
    public Vector3 Acceleration { get; set; } = new();
    public Vector3 Velocity { get; set; } = new();
    public Vector3 Position { get; set; } = new();
    public bool IsDead => false;
    public DrawLayer Layer => layer;
    public float Z => Position.Z;

    public void Create(Scene scene, FrameTime time)
    {
        throw new System.NotImplementedException();
    }

    public void Draw(View view)
    {
        // TODO: Need to place the sprite correctly and get the right animation
        view.Screen.Draw(animations.GetFrame("base", view.Millis));
    }

    public void Glean(Scene scene, FrameTime time)
    {
        throw new System.NotImplementedException();
    }

    public void Tick(Scene scene, FrameTime time)
    {
        Agent.Apply(this, scene, time);
    }
}