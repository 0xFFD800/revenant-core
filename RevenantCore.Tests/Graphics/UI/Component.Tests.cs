using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Graphics.UI;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Graphics.UI;

file class FakeScreen : IScreen
{
    public int currDrawOrder = 0;

    public void Draw(Drawable drawable)
    {
        throw new NotImplementedException();
    }

    public void Pop()
    {
        throw new NotImplementedException();
    }

    public void Push(Matrix transform)
    {
        throw new NotImplementedException();
    }
}

public class FakeComponent(Rectangle area, bool initEnabled, bool initHasFocus, float z, bool isDead, bool expAnimate, bool expCreate, int? expDrawOrder, bool expGlean, bool expTick) : IComponent
{
    private bool animated = false, created = false, gleaned = false, matched = false, ticked = false;
    private int? drawOrder = null;

    public bool HasFocus { get; set; } = initHasFocus;
    public Rectangle Area => area;
    public bool Enabled { get; set; } = initEnabled;
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => z;

    public bool IsDead => isDead;

    public void Animate(IAnimationHook hook, Scene scene, FrameTime time)
    {
        animated = true;
    }

    public void Create(Scene scene, FrameTime time)
    {
        created = true;
    }

    public void Draw(View view, Camera camera)
    {
        drawOrder = ((FakeScreen)view.Screen).currDrawOrder++;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        gleaned = true;
    }

    public bool Matches(IControllable other)
    {
        matched = true;
        return other == this;
    }

    public void Tick(Scene scene, FrameTime time)
    {
        ticked = true;
    }

    public void Validate()
    {
        Assert.AreEqual(expAnimate, animated);
        Assert.AreEqual(expCreate, created);
        Assert.AreEqual(expDrawOrder, drawOrder);
        Assert.AreEqual(expGlean, gleaned);
        Assert.AreEqual(expTick, ticked);
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