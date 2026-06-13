using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
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

file class MockCutsceneSpec : CutsceneSpec
{
    private MockCutscene? cutscene;
    public MockCutscene Cutscene
    {
        get
        {
            if (cutscene == null)
                throw new NullReferenceException("Expected cutscene to be non-null, but it was not");
            return cutscene;
        }
    }

    public readonly float z = 0;
    public readonly bool expDead, expCreate, expTick, expGlean = false;
    public readonly int? expDrawOrder = null;
    public bool complete, created, ticked, gleaned = false;
    public int? drawOrder = null;

    internal MockCutsceneSpec(EventFilterSpec filter)
    {
        Filter = filter;
    }

    internal MockCutsceneSpec(EventFilterSpec filter, bool complete, float z, bool expDead, bool expCreate, int? expDrawOrder, bool expTick, bool expGlean) : this(filter)
    {
        this.complete = complete;
        this.z = z;
        this.expDead = expDead;
        this.expCreate = expCreate;
        this.expDrawOrder = expDrawOrder;
        this.expTick = expTick;
        this.expGlean = expGlean;
    }

    internal MockCutsceneSpec() : this(new())
    {

    }

    public override Cutscene Create(Universe universe)
    {
        cutscene = new(this);

        return cutscene;
    }
}

file class MockCutscene : Cutscene
{
    private readonly MockCutsceneSpec spec;
    public override float Z => spec.z;

    internal MockCutscene(MockCutsceneSpec spec) : base(new(new([])), spec)
    {
        this.spec = spec;
        SetComplete(spec.complete);
    }

    internal MockCutscene() : this(new()) { }

    public void SetComplete(bool complete)
    {
        spec.complete = complete;
        base.complete = complete;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        Assert.IsFalse(spec.created, "Cutscene was created twice!");
        spec.created = true;
    }

    public override void Draw(View view)
    {
        if (view.Screen is FakeScreen screen)
            spec.drawOrder = screen.drawOrder++;
        else
            throw new ArgumentException("MockCutscene must be drawn by a FakeScreen");
    }

    public override void Glean(Scene scene, FrameTime time)
    {
        Assert.IsFalse(spec.gleaned, "Cutscene was gleaned twice!");
        base.Glean(scene, time);
        spec.gleaned = true;
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        spec.ticked = true;
    }

    internal void Validate()
    {
        Assert.AreEqual(spec.expDead, IsDead, "IsDead did not match expectation");
        Assert.AreEqual(spec.expCreate, spec.created, "created did not match expectation");
        Assert.AreEqual(spec.expDrawOrder, spec.drawOrder, "drawOrder did not match expectation");
        Assert.AreEqual(spec.expTick, spec.ticked, "ticked did not match expectation");
        Assert.AreEqual(spec.expGlean, spec.gleaned, "gleaned did not match expectation");
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
        new MockCutscene(new(failedFilter ? new() { HasAll = ["INCOMPLETE"] } : new(), complete, 0, expDead, false, null, false, false)).Validate();
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
        Cutscene block = new SequentialBlockSpec().Create(new(new([])));
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
        Cutscene block = new SequentialBlockSpec() { Children = [new MockCutsceneSpec(new(), false, 1, false, false, null, false, false)] }.Create(new(new([])));
        Assert.AreEqual(1, block.Z);
    }

    [Test]
    public void Create_AdvanceUntilActive()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(new(), true, 0, false, false, null, false, true), // Ditto.
            new(new(), false, 0, false, true, null, false, false), // Active; should be created
            new(new(), false, 0, false, false, null, false, false), // Active; should not be created yet
            new(new(), true, 0, true, false, null, false, false) // Inactive; should not be gleaned yet
        ];
        new SequentialBlockSpec() { Children = children }.Create(new(new([]))).Create(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Draw_DrawFirst()
    {
        MockCutsceneSpec[] children = [
            new(new(), false, 0, false, false, 0, false, false),
            new(new(), false, 0, false, false, null, false, false)
        ];
        new SequentialBlockSpec() { Children = children }.Create(new(new([]))).Draw(new(new FakeScreen(), 0, DrawLayer.UI));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Tick_AdvanceThenTick()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(new(), true, 0, false, false, null, false, true), // Ditto.
            new(new(), false, 0, false, true, null, true, false), // Active; should be created and ticked.
            new(new(), false, 0, false, false, null, false, false), // Active; should not be created yet.
            new(new(), true, 0, true, false, null, false, false) // Inactive; should not be gleaned yet.
        ];
        new SequentialBlockSpec() { Children = children }.Create(new(new([]))).Tick(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Glean_GleanRemaining()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Should be gleaned by Create.
            new(new(), false, 0, false, true, null, false, true), // Active; should be created, then gleaned by Glean.
            new(new(), false, 0, false, false, null, false, true), // Active; should not be created, but should be gleaned.
            new(new(), true, 0, false, false, null, false, true) // Inactive; should be gleaned.
        ];
        Cutscene block = new SequentialBlockSpec() { Children = children }.Create(new(new([])));
        block.Create(new(new()), new(new()));
        block.Glean(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
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
        Cutscene block = new ConcurrentBlockSpec().Create(new(new([])));
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
        Cutscene block = new ConcurrentBlockSpec()
        {
            Children = [
                new MockCutsceneSpec(new(), false, -1, false, true, null, false, false),
                new MockCutsceneSpec(new(), false, 2, false, true, null, false, false),
                new MockCutsceneSpec(new(), true, 3, false, false, null, false, true),
                new MockCutsceneSpec(new(), false, 1, false, true, null, false, false)
            ]
        }.Create(new(new([])));
        block.Create(new(new()), new(new()));
        Assert.AreEqual(2, block.Z);
    }

    [Test]
    public void Create_CreateAllActive()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Should be gleaned.
            new(new(), false, 0, false, true, null, false, false), // Active; should be created.
            new(new(), false, 0, false, true, null, false, false), // Ditto.
            new(new(), true, 0, false, false, null, false, true) // Same as first element.
        ];
        new ConcurrentBlockSpec() { Children = children }.Create(new(new([]))).Create(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Draw_DrawAll()
    {
        MockCutsceneSpec[] children = [
            new(new(), false, 1, false, true, 1, false, false),
            new(new(), false, 0, false, true, 0, false, false)
        ];
        Cutscene block = new ConcurrentBlockSpec() { Children = children }.Create(new(new([])));
        block.Create(new(new()), new(new()));
        block.Draw(new(new FakeScreen(), 0, DrawLayer.UI));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Tick_TickActiveGleanInactive()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Will not be dead, because should have complete reset after being gleaned.
            new(new(), false, 0, false, true, null, false, true), // Active, but will be set to inactive between Create and Tick. Should be created, but gleaned after Tick.
            new(new(), false, 0, false, true, null, true, false), // Active; should be created and ticked.
            new(new(), false, 0, false, true, null, true, false), // Ditto.
            new(new(), true, 0, false, false, null, false, true) // Same as first element.
        ];
        Cutscene block = new ConcurrentBlockSpec() { Children = children }.Create(new(new([])));
        block.Create(new(new()), new(new()));
        children[1].Cutscene.SetComplete(true);
        block.Tick(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }

    [Test]
    public void Glean_GleanRemaining()
    {
        MockCutsceneSpec[] children = [
            new(new(), true, 0, false, false, null, false, true), // Inactive. Should be gleaned by Create.
            new(new(), false, 0, false, true, null, false, true), // Active; should be created, then gleaned by Glean.
            new(new(), false, 0, false, true, null, false, true), // Ditto.
            new(new(), false, 0, false, true, null, false, true) // Will be set to inactive after Create. Should be gleaned.
        ];
        Cutscene block = new ConcurrentBlockSpec() { Children = children }.Create(new(new([])));
        block.Create(new(new()), new(new()));
        children[3].Cutscene.SetComplete(true);
        block.Glean(new(new()), new(new()));
        foreach (MockCutsceneSpec child in children)
            child.Cutscene.Validate();
    }
}

file class MockInstantCutscene(bool expTrip) : InstantCutscene(new(new([])), new MockCutsceneSpec())
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