using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Cutscenes;

file class MockCutscene() : Cutscene
{
    private readonly float z;
    private readonly bool expDead, expCreate, expDraw, expTick, expGlean = false;
    private bool created, drawn, ticked, gleaned = false;

    public override float Z => z;

    internal MockCutscene(bool complete, float z, bool expDead, bool expCreate, bool expDraw, bool expTick, bool expGlean) : this()
    {
        SetComplete(complete);
        this.z = z;
        this.expDead = expDead;
        this.expCreate = expCreate;
        this.expDraw = expDraw;
        this.expTick = expTick;
        this.expGlean = expGlean;
    }

    public void SetComplete(bool complete)
    {
        base.complete = complete;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        Assert.IsFalse(created, "Cutscene was created twice!");
        created = true;
    }

    public override void Draw(View view)
    {
        drawn = true;
    }

    public override void Glean(Scene scene, FrameTime time)
    {
        Assert.IsFalse(gleaned, "Cutscene was gleaned twice!");
        base.Glean(scene, time);
        gleaned = true;
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        ticked = true;
    }

    public void Validate()
    {
        Assert.AreEqual(expDead, IsDead, "IsDead did not match expectation");
        Assert.AreEqual(expCreate, created, "created did not match expectation");
        Assert.AreEqual(expDraw, drawn, "drawn did not match expectation");
        Assert.AreEqual(expTick, ticked, "ticked did not match expectation");
        Assert.AreEqual(expGlean, gleaned, "gleaned did not match expectation");
    }
}

[TestFixture]
public class Cutscene_Test
{
    [Test]
    public void Layer_UI()
    {
        Assert.AreEqual(DrawLayer.UI, new MockCutscene().Layer);
    }

    [TestCase(true, true, TestName = "IsDead (Complete)", Description = "A completed cutscene should always have IsDead set to true.")]
    [TestCase(false, false, TestName = "IsDead (Incomplete; no filter)", Description = "An incomplete cutscene with no filter should not be dead.")]
    public void IsDead(bool complete, bool expDead)
    {
        new MockCutscene(complete, 0, expDead, false, false, false, false).Validate();
    }
}

file class FakeScreen : IScreen
{
    public void Draw(Drawable drawable)
    {
        // do nothing
    }

    public void Pop()
    {
        // do nothing
    }

    public void Push(Matrix transform)
    {
        // do nothing
    }
}

[TestFixture]
public class SequentialBlock_Test
{
    [Test]
    public void Act_NoChildren_NoError()
    {
        Scene scene = new(new());
        FrameTime time = new(new());
        SequentialBlock block = new([]);
        Assert.AreEqual(0, block.Z);
        Assert.DoesNotThrow(() =>
        {
            block.Create(scene, time);
            block.Draw(new(new FakeScreen(), 0, DrawLayer.UI));
            block.Tick(scene, time);
            block.Glean(scene, time);
        });
    }

    [Test]
    public void Z_ObtainFromFirst()
    {
        SequentialBlock block = new([new MockCutscene(false, 1, false, false, false, false, false)]);
        Assert.AreEqual(1, block.Z);
    }

    [Test]
    public void Create_AdvanceUntilActive()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, false, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(true, 0, false, false, false, false, true), // Ditto.
            new(false, 0, false, true, false, false, false), // Active; should be created
            new(false, 0, false, false, false, false, false), // Active; should not be created yet
            new(true, 0, true, false, false, false, false) // Inactive; should not be gleaned yet
        ];
        new SequentialBlock(children).Create(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Draw_DrawFirst()
    {
        MockCutscene[] children = [
            new(false, 0, false, false, true, false, false),
            new(false, 0, false, false, false, false, false)
        ];
        new SequentialBlock(children).Draw(new(new FakeScreen(), 0, DrawLayer.UI));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Tick_AdvanceThenTick()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, false, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(true, 0, false, false, false, false, true), // Ditto.
            new(false, 0, false, true, false, true, false), // Active; should be created and ticked.
            new(false, 0, false, false, false, false, false), // Active; should not be created yet.
            new(true, 0, true, false, false, false, false) // Inactive; should not be gleaned yet.
        ];
        new SequentialBlock(children).Tick(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Glean_GleanRemaining()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, false, false, true), // Inactive. Should be gleaned by Create.
            new(false, 0, false, true, false, false, true), // Active; should be created, then gleaned by Glean.
            new(false, 0, false, false, false, false, true), // Active; should not be created, but should be gleaned.
            new(true, 0, false, false, false, false, true) // Inactive; should be gleaned.
        ];
        SequentialBlock block = new(children);
        block.Create(new(new()), new(new()));
        block.Glean(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }
}