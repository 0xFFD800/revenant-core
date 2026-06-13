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
        Cutscene[] c = core.LoadCutscenes(new(new([])), "../../../TestAssets/Cutscenes/Sequential.yml");
        foreach (Cutscene cutscene in c)
            cutscene.Create(new(new()), new(new()));
        Assert.AreEqual(2, c.Length, "Expected 2 sequential cutscenes");
        Assert.IsFalse(c[0].IsDead, "The first cutscene in the file should have active children and therefore not be dead.");
        Assert.AreEqual(1, c[0].Z, "The first cutscene in the file should match its first child with a Z of 1.");
        Assert.IsTrue(c[1].IsDead, "The second cutscene in the file should not have active children and therefore be dead.");
        Assert.AreEqual(0, c[1].Z, "The second cutscene in the file should have no children, so its Z should default to 0.");
    }

    [Test]
    public void LoadCutscenes_Concurrent_CreateConcurrent()
    {
        Core core = new([new FakeImpl()]);
        Cutscene[] c = core.LoadCutscenes(new(new([])), "../../../TestAssets/Cutscenes/Concurrent.yml");
        foreach (Cutscene cutscene in c)
            cutscene.Create(new(new()), new(new()));
        Assert.AreEqual(2, c.Length, "Expected 2 concurrent cutscenes");
        Assert.IsFalse(c[0].IsDead, "The first cutscene in the file should have active children and therefore not be dead.");
        Assert.AreEqual(2, c[0].Z, "The first cutscene in the file should match its childrens' maximum Z of 2.");
        Assert.IsTrue(c[1].IsDead, "The second cutscene in the file should not have active children and therefore be dead.");
        Assert.AreEqual(0, c[1].Z, "The second cutscene in the file should have no children, so its Z should default to 0.");
    }

    [Test]
    public void LoadCutscenes_Fake_CreateFake()
    {
        Core core = new([new FakeImpl()]);
        Cutscene[] c = core.LoadCutscenes(new(new([])), "../../../TestAssets/Cutscenes/Fake.yml");
        foreach (Cutscene cutscene in c)
            cutscene.Create(new(new()), new(new()));
        Assert.AreEqual(2, c.Length, "Expected 2 fake cutscenes");
        Assert.AreEqual(1, c[0].Z, "The first cutscene in the file should have a Z of 1.");
        Assert.AreEqual(0, c[1].Z, "The second cutscene in the file should have a Z of 0.");
    }
}