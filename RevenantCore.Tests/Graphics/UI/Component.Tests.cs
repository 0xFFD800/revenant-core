using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Graphics.UI;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Graphics.UI;

public class FakeComponent : IComponent
{
    public bool HasFocus { set => throw new NotImplementedException(); }

    public Rectangle Area => throw new NotImplementedException();

    public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public DrawLayer Layer => throw new NotImplementedException();

    public float Z => throw new NotImplementedException();

    public bool IsDead => throw new NotImplementedException();

    public void Animate(IAnimationHook hook, Scene scene, FrameTime time)
    {
        throw new NotImplementedException();
    }

    public void Create(Scene scene, FrameTime time)
    {
        throw new NotImplementedException();
    }

    public void Draw(View view, Camera camera)
    {
        throw new NotImplementedException();
    }

    public void Glean(Scene scene, FrameTime time)
    {
        throw new NotImplementedException();
    }

    public bool Matches(IControllable other)
    {
        throw new NotImplementedException();
    }

    public void Tick(Scene scene, FrameTime time)
    {
        throw new NotImplementedException();
    }
}

[TestFixture]
public class Container_Test
{
    // To test:
    // * Draw Order & translation 
    // * Tick Loop Sanity Check & Focus Changes
    // * Matches
    // * Animate Sanity Check
}