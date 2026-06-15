using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests;

file class FakeCutscene(Universe universe, FakeCutsceneSpec spec) : Cutscene(universe, spec)
{
    public override float Z => spec.Z;

    public override void Create(Scene scene, FrameTime time)
    {
        // do nothing
    }

    public override void Draw(View view)
    {
        // do nothing
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        // do nothing
    }
}

public class FakeCutsceneSpec : CutsceneSpec
{
    public float Z { get; set; } = 0;

    public override Cutscene Create(Universe universe) => new FakeCutscene(universe, this);
}

file class FakeImpl : IImpl
{
    public CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry) => registry.Register("fake", typeof(FakeCutsceneSpec));
}

public class Core_Test
{
    [Test]
    public void LoadCutscenes_Sequential_CreateSequential()
    {
        Core core = new([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !sequentialBlock
        children:
          - !fake
            z: 1
          - !fake {}
        """);
        c.Create(new(new()), new(new()));
        Assert.IsFalse(c.IsDead, "The cutscene should have active children and therefore not be dead.");
        Assert.AreEqual(1, c.Z, "The cutscene should match its first child with a Z of 1.");
    }

    [Test]
    public void LoadCutscenes_Concurrent_CreateConcurrent()
    {
        Core core = new([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !concurrentBlock
        children:
          - !fake {}
          - !fake
            z: 2
        """);
        c.Create(new(new()), new(new()));
        Assert.IsFalse(c.IsDead, "The cutscene should have active children and therefore not be dead.");
        Assert.AreEqual(2, c.Z, "The cutscene should match its childrens' maximum Z of 2.");
    }

    [Test]
    public void LoadCutscenes_Fake_CreateFake()
    {
        Core core = new([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !fake
        z: 1
        """);
        c.Create(new(new()), new(new()));
        Assert.IsInstanceOf<FakeCutscene>(c, "Cutscene type did not match expectation");
        Assert.AreEqual(1, c.Z, "The cutscene should have a Z of 1.");
    }
}