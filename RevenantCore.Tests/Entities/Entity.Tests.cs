using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;

namespace RevenantCore.Tests.Entities;

[TestFixture]
public class Entity_Test
{
    [Test]
    public void IsDead_InitFalse()
    {
        Entity e = new(new(), new(FrozenDictionary.Create<string, Animation>([]), ""));
        Assert.IsFalse(e.IsDead);
        e.IsDead = true;
        Assert.IsTrue(e.IsDead);
    }

    [TestCase(0, TestName = "Z_PosZ_Zero")]
    [TestCase(1, TestName = "Z_PosZ_Nonzero")]
    public void Z_PosZ(float z)
    {
        Entity e = new(new(), new(FrozenDictionary.Create<string, Animation>([]), ""))
        {
            Position = Vector3.UnitZ * z
        };
        Assert.AreEqual(z, e.Z);
    }

    [Test]
    public void ID_SpecID()
    {
        Entity e = new(new() { Id = "Test" }, new(FrozenDictionary.Create<string, Animation>([]), ""));
        Assert.AreEqual("Test", e.ID);
    }

    private class FakeDrawable(string frameName) : Drawable
    {
        public string FrameName => frameName;

        protected override Vector2 Size => new(2, 4);
        public override void Draw(ISpriteBuffer buffer) { }
        protected override Drawable CopyData() => this;
    }

    private class MockScreen(string expFrameName, Vector2 expPos, Vector2 expOrigin) : IScreen
    {
        public bool hasBeenDrawn = false;
        public void Draw(Drawable drawable)
        {
            Assert.AreEqual(expFrameName, ((FakeDrawable)drawable).FrameName);
            Assert.AreEqual(expPos, drawable.Pos);
            Assert.AreEqual(expOrigin, drawable.Origin);
            hasBeenDrawn = true;
        }

        public void Pop() { }
        public void Push(Matrix transform) { }
    }

    [Test]
    public void Draw_PosFrame()
    {
        MockScreen screen = new("bar", new(1, 92), new(1, 2));
        View view = new(screen, 250, DrawLayer.Scene);
        Camera camera = new(new(100, 100), new(100, 100));
        Entity e = new(new(), new(FrozenDictionary.Create<string, Animation>([new("idle", new([new FakeDrawable("foo"), new FakeDrawable("bar")], 200))]), "idle"))
        {
            Position = new(2, 4, 0)
        };
        e.Draw(view, camera);
        Assert.IsTrue(screen.hasBeenDrawn);
    }
}