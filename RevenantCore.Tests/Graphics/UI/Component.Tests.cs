using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Graphics.UI;
using RevenantCore.Scenes;
using RevenantCore.Util;
using static RevenantCore.Tests.Scenes.Scene_Test;

namespace RevenantCore.Tests.Graphics.UI;

file class FakeScreen : IScreen
{
    public int currDrawOrder = 0;
    public Matrix? matrix = null;

    public void Draw(Drawable drawable)
    {
        throw new NotImplementedException();
    }

    public void Pop()
    {
        matrix = null;
    }

    public void Push(Matrix transform)
    {
        matrix = transform;
    }
}

public class MockComponent(Rectangle area, bool initEnabled, bool initHasFocus, float z, bool isDead, bool expAnimate, bool expCreate, int? expDrawOrder, bool expMatched, bool expGlean, bool expTick, Matrix? expTranslation) : IComponent
{
    private bool animated = false, created = false, gleaned = false, matched = false, ticked = false;
    private int? drawOrder = null;
    private Matrix? translation = null;

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
        FakeScreen screen = (FakeScreen)view.Screen;
        drawOrder = screen.currDrawOrder++;
        translation = screen.matrix;
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
        Assert.AreEqual(expMatched, matched);
        Assert.AreEqual(expGlean, gleaned);
        Assert.AreEqual(expTick, ticked);
        Assert.AreEqual(expTranslation, translation);
    }
}

[TestFixture]
public class Container_Test
{
    [Test]
    public void EmptyDraw_SanityCheck()
    {
        Assert.DoesNotThrow(() => new Container([]).Draw(new(new FakeScreen(), 0, DrawLayer.UI), new(new(), new())));
    }

    [Test]
    public void DrawOrder()
    {
        Vector3 pos = new(4, 6, 0);
        Matrix matrix = Matrix.CreateTranslation(pos);
        MockComponent mock1 = new(new(0, 1, 4, 2), true, false, 1, false, false, true, 1, false, false, true, matrix);
        MockComponent mock2 = new(new(7, 0, 2, 4), true, false, 2, false, false, true, 0, false, false, true, matrix);
        Container container = new([mock1, mock2], new(4, 6, 4, 4), new());
        Scene scene = new FakeScene();
        container.Create(scene, new(new()));
        container.Tick(scene, new(new()));
        container.Draw(new(new FakeScreen(), 0, DrawLayer.UI), new(new(), new()));
        mock1.Validate();
        mock2.Validate();
    }

    // To test:
    // * Tick Loop Sanity Check & Focus Changes
    // * Matches
    // * Animate Sanity Check
}