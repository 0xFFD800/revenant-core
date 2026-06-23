using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Entities;

public class Entity(IAgent agent, MaterialSpec material, BoundingBox bounds, DrawLayer layer) : ICollideable, ITickable, IVisible
{
    private readonly IAgent agent = agent;

    public BoundingBox CollisionBox => bounds + Position;
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
        throw new System.NotImplementedException();
    }

    public void Glean(Scene scene, FrameTime time)
    {
        throw new System.NotImplementedException();
    }

    public void Tick(Scene scene, FrameTime time)
    {
        agent.Apply(this, scene, time);
    }
}