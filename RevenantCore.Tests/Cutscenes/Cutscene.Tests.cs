using RevenantCore.Cutscenes;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Cutscenes;

file class MockCutscene() : Cutscene
{
    private readonly float z;
    private readonly bool expDead, expCreate, expDraw, expTick = false;
    private bool created, drawn, ticked = false;

    public override float Z => z;

    MockCutscene(bool complete, float z, bool expDead, bool expCreate, bool expDraw, bool expTick) : this()
    {
        base.complete = complete;
        this.z = z;
        this.expDead = expDead;
        this.expCreate = expCreate;
        this.expDraw = expDraw;
        this.expTick = expTick;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        created = true;
    }

    public override void Draw(View view)
    {
        drawn = true;
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        ticked = true;
    }

    public void Validate()
    {
        Assert.AreEqual(expDead, IsDead);
        Assert.AreEqual(expCreate, created);
        Assert.AreEqual(expDraw, drawn);
        Assert.AreEqual(expTick, ticked);
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
}