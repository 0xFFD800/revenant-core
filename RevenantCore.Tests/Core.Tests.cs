using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Entities;
using RevenantCore.Entities.Spec;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests;

file class FakeDrawable(string path) : Drawable
{
    internal string Path => path;

    protected override Vector2 Size => throw new NotImplementedException();

    public override void Draw(ISpriteBuffer buffer)
    {
        throw new NotImplementedException();
    }

    protected override Drawable CopyData() => new FakeDrawable(path);
}

file class FakeLoader : ILoader
{
    public Drawable LoadSprite(string path) => new FakeDrawable(path);
}

file class FakeInputs : IInputs
{
    public KeyboardState Keyboard => throw new NotImplementedException();

    public MouseState Mouse => throw new NotImplementedException();

    public GamePadState GamePad(PlayerIndex player)
    {
        throw new NotImplementedException();
    }
}

public class FakeCore(IInputs inputs, IImpl[] impls) : Core(new FakeLoader(), inputs, impls)
{
    public FakeCore(IImpl[] impls) : this(new FakeInputs(), impls) { }
    public FakeCore() : this([]) { }
}

file class FakeCutscene(Universe universe, FakeCutsceneSpec spec) : Cutscene(universe, spec)
{
    public override float Z => spec.Z;
    public string Foo => spec.Foo;

    public override void Create(Scene scene, FrameTime time)
    {
        // do nothing
    }

    public override void Draw(View view, Camera camera)
    {
        // do nothing
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        // do nothing
    }
}

file class FakeCutsceneSpec : CutsceneSpec
{
    public float Z { get; set; } = 0;
    public string Foo { get; set; } = "";

    public override Cutscene Create(Universe universe) => new FakeCutscene(universe, this);
}

file class FakeImpl : IImpl
{
    public CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry) => registry.Register("fake", typeof(FakeCutsceneSpec));

    public ControlRegistryBuilder RegisterControls(ControlRegistryBuilder registry) => registry.Register(new()
    {
        ID = "fake",
        Name = "Fake",
        Descr = "Fake Control",
        Default = new()
        {
            Keys = [Keys.A, Keys.B],
            Buttons = [
                new()
                {
                    Button = Buttons.A
                },
                new()
                {
                    Button = Buttons.B
                }
            ],
            MouseButtons = [MouseButtons.Left, MouseButtons.Right]
        }
    });
}

public class Core_Test
{
    [Test]
    public void LoadCutscene_Sequential_CreateSequential()
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !sequentialBlock
        children:
          - !fake
            z: 1
          - !fake {}
        """);
        c.Create(new(new(new FakeCore([]), new([])), new(), "default"), new(new()));
        Assert.IsFalse(c.IsDead, "The cutscene should have active children and therefore not be dead.");
        Assert.AreEqual(1, c.Z, "The cutscene should match its first child with a Z of 1.");
    }

    [Test]
    public void LoadCutscene_Concurrent_CreateConcurrent()
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !concurrentBlock
        children:
          - !fake {}
          - !fake
            z: 2
        """);
        c.Create(new(new(new FakeCore([]), new([])), new(), "default"), new(new()));
        Assert.IsFalse(c.IsDead, "The cutscene should have active children and therefore not be dead.");
        Assert.AreEqual(2, c.Z, "The cutscene should match its childrens' maximum Z of 2.");
    }

    [TestCase("Concurrent.yml", false, 2, TestName = "Load !load Cutscene (concurrent, provide values)")]
    [TestCase("Concurrent.yml", true, 1, TestName = "Load !load Cutscene (concurrent, use defaults)")]
    [TestCase("Sequential.yml", false, -1, TestName = "Load !load Cutscene (sequential, provide values)")]
    [TestCase("Sequential.yml", true, 0, TestName = "Load !load Cutscene (sequential, use defaults)")]
    public void LoadCutscene_Load_CreateFromParameters(string fileName, bool useDefaults, float expZ)
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), string.Format("""
        !load
        fileName: ../../../TestAssets/Cutscenes/{0}
        parameters: {1}
        """, fileName, useDefaults ? "{}" : """

          num: '-1'
          str: fake
          obj: '!concurrentBlock { children: [ !fake { z: 2 } ] }'
        """));
        c.Create(new(new(new FakeCore([]), new([])), new(), "default"), new(new()));
        Assert.IsFalse(c.IsDead, "The cutscene should have active children and therefore not be dead.");
        Assert.AreEqual(expZ, c.Z, "The cutscene did not match the expected Z value.");
    }

    [TestCase(false, -1, TestName = "Load !load Cutscene (fake, provide values)")]
    [TestCase(true, 1, TestName = "Load !load Cutscene (fake, use defaults)")]
    public void LoadCutscene_LoadFake_CreateFromParameters(bool useDefaults, float expZ)
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), string.Format("""
        !load
        fileName: ../../../TestAssets/Cutscenes/Fake.yml
        parameters: {0}
        """, useDefaults ? "{}" : """

          z: '-1'
        """));
        Assert.IsFalse(c.IsDead, "The cutscene should not be complete and therefore not be dead.");
        Assert.AreEqual(expZ, c.Z, "The cutscene did not match the expected Z value.");
    }

    [Test]
    public void LoadCutscene_NonReferencingParameter_LiteralStr()
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), string.Format("""
        !load
        fileName: ../../../TestAssets/Cutscenes/NonReferencingParameter.yml
        """));
        Assert.IsFalse(c.IsDead, "The cutscene should not be complete and therefore not be dead.");
        if (c is FakeCutscene fake)
            Assert.AreEqual("${nonReference}", fake.Foo, "The cutscene did not match the expected value of Foo.");
        else
            Assert.Fail("Expected created cutscene to be a fake cutscene");
    }

    [Test]
    public void LoadCutscene_UnmatchedBracket_CompleteParse()
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), string.Format("""
        !load
        fileName: ../../../TestAssets/Cutscenes/UnmatchedBracket.yml
        """));
        Assert.IsFalse(c.IsDead, "The cutscene should not be complete and therefore not be dead.");
        Assert.AreEqual(-1, c.Z, "The cutscene did not match the expected Z value.");
        if (c is FakeCutscene fake)
            Assert.AreEqual("${unmatched", fake.Foo, "The cutscene did not match the expected value of Foo.");
        else
            Assert.Fail("Expected created cutscene to be a fake cutscene");
    }

    [Test]
    public void LoadCutscene_Fake_CreateFake()
    {
        Core core = new FakeCore([new FakeImpl()]);
        Cutscene c = core.LoadCutscene(new(core, new([])), """
        !fake
        z: 1
        """);
        c.Create(new(new(new FakeCore([]), new([])), new(), "default"), new(new()));
        Assert.IsInstanceOf<FakeCutscene>(c, "Cutscene type did not match expectation");
        Assert.AreEqual(1, c.Z, "The cutscene should have a Z of 1.");
    }

    [TestCase("fooFirst", 0, "path/to/foo", TestName = "fooFirst at millis of 0")]
    [TestCase("barFirst", 0, "path/to/bar", TestName = "barFirst at millis of 0")]
    [TestCase("fooFirst", 101, "path/to/bar", TestName = "fooFirst at millis of 101")]
    [TestCase("barFirst", 101, "path/to/foo", TestName = "barFirst at millis of 101")]
    [TestCase("fooFirst", 201, "path/to/foo", TestName = "fooFirst at millis of 201 (default sprite)")]
    [TestCase("barFirst", 201, "path/to/foo", TestName = "barFirst at millis of 201 (default sprite)")]
    [TestCase("fooFirst", 301, "path/to/other", TestName = "fooFirst at millis of 301 (explicit path)")]
    [TestCase("notFound", 0, "path/to/foo", TestName = "animation should default to fooFirst if not present")]
    public void LoadAnimationCollection_Sprites(string key, int millis, string expPath)
    {
        Core core = new FakeCore([]);
        AnimationCollection c = core.LoadAnimationCollection("""
        sprites:
          foo: path/to/foo
          bar: path/to/bar
        animations:
          fooFirst:
            - sprite: foo
            - sprite: bar
            - sprite: 
            - sprite: path/to/other
          barFirst:
            - sprite: bar
            - sprite: foo
            - sprite: 
        millisPerFrame: 100
        defaultSprite: foo
        defaultAnimation: fooFirst
        """);
        Assert.AreEqual(expPath, ((FakeDrawable)c.GetFrame(key, millis)).Path);
    }

    [TestCase("foo", 0, 0, TestName = "foo at 0 millis")]
    [TestCase("foo", 100, 1, TestName = "foo at 100 millis")]
    [TestCase("bar", 0, 100, TestName = "bar at 0 millis")]
    [TestCase("bar", 100, 90, TestName = "bar at 100 millis")]
    public void LoadAnimationCollection_Source(string key, int millis, int? expSourceX)
    {
        Core core = new FakeCore([]);
        AnimationCollection c = core.LoadAnimationCollection("""
        sprites:
          base: path/to/base
        animations:
          foo:
            - source:
                x: 0
            - source:
                x: 1
          bar:
            - source:
                x: 100
            - source:
                x: 90
        millisPerFrame: 100
        """);
        Assert.AreEqual(expSourceX, c.GetFrame(key, millis).Source?.X);
    }
}