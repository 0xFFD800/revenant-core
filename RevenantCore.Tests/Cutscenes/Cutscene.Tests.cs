using System.Diagnostics;
using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Cutscenes;

file class FakeScreen : IScreen
{
    internal int drawOrder = 0;

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

file class MockCutscene() : Cutscene(new(new([])))
{
    private readonly float z = 0;
    private readonly bool expDead, expCreate, expTick, expGlean = false;
    private readonly int? expDrawOrder = null;
    private bool created, ticked, gleaned = false;
    private int? drawOrder = null;

    public override float Z => z;

    internal MockCutscene(bool complete, float z, bool expDead, bool expCreate, int? expDrawOrder, bool expTick, bool expGlean) : this()
    {
        SetComplete(complete);
        this.z = z;
        this.expDead = expDead;
        this.expCreate = expCreate;
        this.expDrawOrder = expDrawOrder;
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
        if (view.Screen is FakeScreen screen)
            drawOrder = screen.drawOrder++;
        else
            throw new ArgumentException("MockCutscene must be drawn by a FakeScreen");
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

    internal void Validate()
    {
        Assert.AreEqual(expDead, IsDead, "IsDead did not match expectation");
        Assert.AreEqual(expCreate, created, "created did not match expectation");
        Assert.AreEqual(expDrawOrder, drawOrder, "drawOrder did not match expectation");
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

    [TestCase(false, true, true, TestName = "IsDead (Complete; no filter)", Description = "A completed cutscene should always have IsDead set to true.")]
    [TestCase(true, true, true, TestName = "IsDead (Complete; filter)", Description = "A completed and filtered cutscene should always have IsDead set to true.")]
    [TestCase(true, false, true, TestName = "IsDead (Incomplete; filter)", Description = "An incomplete but filtered cutscene should always have IsDead set to true.")]
    [TestCase(false, false, false, TestName = "IsDead (Incomplete; no filter)", Description = "An incomplete cutscene with no filter should not be dead.")]
    public void IsDead(bool failedFilter, bool complete, bool expDead)
    {
        MockCutscene c = new(complete, 0, expDead, false, null, false, false);
        if (failedFilter)
            c.Filter = new(new()
            {
                HasAll = ["INCOMPLETE"]
            });
        c.Validate();
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
        SequentialBlock block = new(new(new([])), []);
        Assert.AreEqual(0, block.Z);
        Assert.DoesNotThrow(() => block.Create(scene, time));
        Assert.IsTrue(block.IsDead, "Block should be dead on arrival if it has no active children");
        Assert.DoesNotThrow(() =>
        {
            block.Draw(new(new FakeScreen(), 0, DrawLayer.UI));
            block.Tick(scene, time);
            block.Glean(scene, time);
        });
    }

    [Test]
    public void Z_ObtainFromFirst()
    {
        SequentialBlock block = new(new(new([])), [new MockCutscene(false, 1, false, false, null, false, false)]);
        Assert.AreEqual(1, block.Z);
    }

    [Test]
    public void Create_AdvanceUntilActive()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(true, 0, false, false, null, false, true), // Ditto.
            new(false, 0, false, true, null, false, false), // Active; should be created
            new(false, 0, false, false, null, false, false), // Active; should not be created yet
            new(true, 0, true, false, null, false, false) // Inactive; should not be gleaned yet
        ];
        new SequentialBlock(new(new([])), children).Create(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Draw_DrawFirst()
    {
        MockCutscene[] children = [
            new(false, 0, false, false, 0, false, false),
            new(false, 0, false, false, null, false, false)
        ];
        new SequentialBlock(new(new([])), children).Draw(new(new FakeScreen(), 0, DrawLayer.UI));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Tick_AdvanceThenTick()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(true, 0, false, false, null, false, true), // Ditto.
            new(false, 0, false, true, null, true, false), // Active; should be created and ticked.
            new(false, 0, false, false, null, false, false), // Active; should not be created yet.
            new(true, 0, true, false, null, false, false) // Inactive; should not be gleaned yet.
        ];
        new SequentialBlock(new(new([])), children).Tick(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Glean_GleanRemaining()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Should be gleaned by Create.
            new(false, 0, false, true, null, false, true), // Active; should be created, then gleaned by Glean.
            new(false, 0, false, false, null, false, true), // Active; should not be created, but should be gleaned.
            new(true, 0, false, false, null, false, true) // Inactive; should be gleaned.
        ];
        SequentialBlock block = new(new(new([])), children);
        block.Create(new(new()), new(new()));
        block.Glean(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }
}

[TestFixture]
public class ConcurrentBlock_Test
{
    [Test]
    public void Act_NoChildren_NoError()
    {
        Scene scene = new(new());
        FrameTime time = new(new());
        ConcurrentBlock block = new(new(new([])), []);
        Assert.AreEqual(0, block.Z);
        Assert.DoesNotThrow(() => block.Create(scene, time));
        Assert.IsTrue(block.IsDead, "Block should be dead on arrival if it has no active children");
        Assert.DoesNotThrow(() =>
        {
            block.Draw(new(new FakeScreen(), 0, DrawLayer.UI));
            block.Tick(scene, time);
            block.Glean(scene, time);
        });
    }

    [Test]
    public void Z_UseMaxActive()
    {
        ConcurrentBlock block = new(new(new([])), [
            new MockCutscene(false, -1, false, true, null, false, false),
            new MockCutscene(false, 2, false, true, null, false, false),
            new MockCutscene(true, 3, false, false, null, false, true),
            new MockCutscene(false, 1, false, true, null, false, false)
        ]);
        block.Create(new(new()), new(new()));
        Assert.AreEqual(2, block.Z);
    }

    [Test]
    public void Create_CreateAllActive()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Should be gleaned.
            new(false, 0, false, true, null, false, false), // Active; should be created.
            new(false, 0, false, true, null, false, false), // Ditto.
            new(true, 0, false, false, null, false, true) // Same as first element.
        ];
        new ConcurrentBlock(new(new([])), children).Create(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Draw_DrawAll()
    {
        MockCutscene[] children = [
            new(false, 1, false, true, 1, false, false),
            new(false, 0, false, true, 0, false, false)
        ];
        ConcurrentBlock block = new(new(new([])), children);
        block.Create(new(new()), new(new()));
        block.Draw(new(new FakeScreen(), 0, DrawLayer.UI));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Tick_TickActiveGleanInactive()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(false, 0, false, true, null, false, true), // Active, but will be set to inactive between Create and Tick. Should be created, but gleaned after Tick.
            new(false, 0, false, true, null, true, false), // Active; should be created and ticked.
            new(false, 0, false, true, null, true, false), // Ditto.
            new(true, 0, false, false, null, false, true) // Same as first element.
        ];
        ConcurrentBlock block = new(new(new([])), children);
        block.Create(new(new()), new(new()));
        children[1].SetComplete(true);
        block.Tick(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }

    [Test]
    public void Glean_GleanRemaining()
    {
        MockCutscene[] children = [
            new(true, 0, false, false, null, false, true), // Inactive. Should be gleaned by Create.
            new(false, 0, false, true, null, false, true), // Active; should be created, then gleaned by Glean.
            new(false, 0, false, true, null, false, true), // Ditto.
            new(false, 0, false, true, null, false, true) // Will be set to inactive after Create. Should be gleaned.
        ];
        ConcurrentBlock block = new(new(new([])), children);
        block.Create(new(new()), new(new()));
        children[3].SetComplete(true);
        block.Glean(new(new()), new(new()));
        foreach (MockCutscene child in children)
            child.Validate();
    }
}

file class MockInstantCutscene(bool expTrip) : InstantCutscene(new(new([])))
{
    private bool tripped = false;

    protected override void Trip(Scene scene, FrameTime time)
    {
        tripped = true;
    }

    internal void Validate()
    {
        Assert.AreEqual(0, Z);
        Assert.AreEqual(expTrip, tripped, "tripped did not match expectation");
    }
}

[TestFixture]
public class InstantCutscene_Test
{
    [Test]
    public void Initial_NoTrip()
    {
        new MockInstantCutscene(false).Validate();
    }

    [Test]
    public void Create_Trip()
    {
        MockInstantCutscene cutscene = new(true);
        cutscene.Create(new(new()), new(new()));
        cutscene.Validate();
    }

    [Test]
    public void Draw_Throw()
    {
        Assert.Throws<UnreachableException>(() =>
            new MockInstantCutscene(false).Draw(new(new FakeScreen(), 0, DrawLayer.UI)));
    }

    [Test]
    public void Tick_Throw()
    {
        Assert.Throws<UnreachableException>(() =>
            new MockInstantCutscene(false).Tick(new(new()), new(new())));
    }
}