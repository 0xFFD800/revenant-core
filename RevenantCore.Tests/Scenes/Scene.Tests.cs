using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Tests.Scenes;

[TestFixture]
public class Scene_Test
{
    private class FakeScene : Scene
    {
        public int currDrawOrder = 0;
    }

    private interface IMockMortal : IMortal
    {
        void Validate();
    }

    private class MockVisible(FakeScene fakeScene, DrawLayer layer, float z, bool isDead, int? expDrawOrder, bool expGlean) : IMockMortal, IVisible
    {
        private readonly FakeScene scene = fakeScene;
        private bool created, gleaned, drawn = false;

        public DrawLayer Layer => layer;
        public float Z => z;
        public bool IsDead => isDead;

        public void Create(Scene scene, FrameTime time)
        {
            created = true;
            Assert.AreSame(this.scene, scene, "Visible was not created for the expected scene!");
        }

        public void Draw(View view)
        {
            drawn = true;
            Assert.AreEqual(Layer, view.Layer, "Visible was not drawn on the expected layer!");
            Assert.True(expDrawOrder.HasValue, "Visible did not expect to be drawn at all!");
            Assert.AreEqual(expDrawOrder.Value, scene.currDrawOrder++, "Visible was not drawn in the expected order!");
        }

        public void Glean(Scene scene, FrameTime time)
        {
            gleaned = true;
            Assert.IsTrue(expGlean, "This visible did not expect to be gleaned!");
        }

        /// <summary>
        /// Checks that the expected methods have been called (or not) for this object's state.
        /// </summary>
        public void Validate()
        {
            Assert.True(created, "Visible object was never created!");
            Assert.AreEqual(expGlean, gleaned, "Visible object was not in the correct gleaning state!");
            Assert.AreEqual(expDrawOrder.HasValue, drawn, "Visible object was not in the correct drawing state!");
        }
    }

    private class MockScreen : IScreen
    {
        private int matrices = 0;

        public void Push(Matrix transform)
        {
            matrices++;
        }

        public void Pop()
        {
            matrices--;
            Assert.GreaterOrEqual(matrices, 0, "IScreen.Pop called with no corresponding call to Push!");
        }

        public void Draw(Drawable sprite)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Should be called after the draw loop has been run to verify calls to Push and Pop were balanced.
        /// </summary>
        public void Validate()
        {
            Assert.AreEqual(0, matrices, "Unbalanced calls to Push and Pop after draw loop!");
        }
    }

    private class MockCollideable(Vector3 pos, Vector3 velocity, Vector3 size, MaterialSpec material, bool isDead, bool expGlean, Vector3 expPos, Vector3 expVelocity) : IMockMortal, ICollideable
    {
        private bool created, gleaned = false;

        /// <summary>
        /// Constructor overload for tests which don't care about position and velocity information.
        /// </summary>
        internal MockCollideable(bool isDead, bool expGlean) : this(Vector3.Zero, Vector3.Zero, Vector3.Zero, new(), isDead, expGlean, Vector3.Zero, Vector3.Zero)
        {

        }

        private Vector3 BottomLeft => Position - new Vector3(size.X / 2, 0, size.Z / 2);
        public BoundingBox CollisionBox => new(BottomLeft, BottomLeft + size);

        public MaterialSpec Material => material;
        public Vector3 Velocity { get; set; } = velocity;
        public Vector3 Position { get; set; } = pos;

        public bool IsDead => isDead;

        public void Create(Scene scene, FrameTime time)
        {
            created = true;
        }

        public void Glean(Scene scene, FrameTime time)
        {
            gleaned = true;
            Assert.IsTrue(expGlean, "This collideable did not expect to be gleaned!");
        }

        public void Validate()
        {
            Assert.True(created, "Collideable object was never created!");
            Assert.AreEqual(expGlean, gleaned);
            Assert.AreEqual(expPos, Position);
            Assert.AreEqual(expVelocity, Velocity);
        }
    }

    private static void RunDrawLoop(Scene scene, MockVisible[] visibles)
    {
        foreach (MockVisible visible in visibles)
            scene.Add(visible, scene, new(new()));
        scene.Tick(scene, new(new()));
        MockScreen screen = new();
        foreach (DrawLayer layer in Enum.GetValues<DrawLayer>())
            scene.Draw(new(screen, 0, layer));
        screen.Validate();
        foreach (MockVisible visible in visibles)
            visible.Validate();

        Assert.False(scene.IsDead, "Scenes currently should never die");
    }

    [Test(Description = "Draw should not raise an exception when drawn with an empty list.")]
    public void Draw_NoVisibles_NoRaise()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        Assert.DoesNotThrow(() => RunDrawLoop(scene, []));
    }

    [Test(Description = "Live objects should be drawn and not gleaned.")]
    public void Draw_Live_Drawn()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunDrawLoop(scene, [new(scene, DrawLayer.Scene, 0, false, 0, false)]);
    }

    [Test(Description = "Dead objects should be gleaned and not drawn.")]
    public void Draw_Dead_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunDrawLoop(scene, [new(scene, DrawLayer.Scene, 0, true, null, true)]);
    }

    [Test(Description = "Layers should be drawn in the expected order.")]
    public void Draw_Layer_Order()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunDrawLoop(scene, [
            new(scene, DrawLayer.UI,         0, false, 4, false),
            new(scene, DrawLayer.Background, 0, false, 1, false),
            new(scene, DrawLayer.Foreground, 0, false, 3, false),
            new(scene, DrawLayer.Base,       0, false, 0, false),
            new(scene, DrawLayer.Scene,      0, false, 2, false)
        ]);
    }

    [Test(Description = "Within a layer, objects with lower Z values should be drawn later.")]
    public void Draw_Z_Order()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunDrawLoop(scene, [
            new(scene, DrawLayer.UI,    1, false, 2, false),
            new(scene, DrawLayer.UI,    0, false, 3, false),
            new(scene, DrawLayer.Scene, 0, false, 1, false),
            new(scene, DrawLayer.Scene, 1, false, 0, false)
        ]);
    }

    [Test(Description = "All objects should be gleaned when the scene is gleaned.")]
    public void Glean_All_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        IMockMortal[] mortals = [
            new MockVisible(scene, DrawLayer.Scene, 0, false, null, true),     // Live visible
            new MockVisible(scene, DrawLayer.Background, 0, true, null, true), // Dead visible
            new MockCollideable(false, true),                                  // Live collideable
            new MockCollideable(false, true)                                   // Dead collideable
        ];
        foreach (IMockMortal mortal in mortals)
            scene.Add(mortal, scene, new(new()));
        scene.Glean(scene, new(new()));
        foreach (IMockMortal mortal in mortals)
            mortal.Validate();
    }

    private static void RunCollisionsLoop(Scene scene, MockCollideable[] collideables)
    {
        foreach (MockCollideable collideable in collideables)
            scene.Add(collideable, scene, new(new()));
        scene.Tick(scene, new(new()));
        foreach (MockCollideable collideable in collideables)
            collideable.Validate();

        Assert.False(scene.IsDead, "Scenes currently should never die");
    }

    [Test(Description = "If there are no collisions or friction and the objects are at floor level, objects should just be moved")]
    public void Tick_NoCollisions_Move()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [
            new(Vector3.Zero,       Vector3.UnitX, Vector3.One, new(), false, false, Vector3.UnitX,      Vector3.UnitX),
            new(Vector3.UnitZ * 10, Vector3.UnitZ, Vector3.One, new(), false, false, Vector3.UnitZ * 11, Vector3.UnitZ)
        ]);
    }

    [TestCase(0.3136F, 0, 1, 0, 0, 0, 0.6864F, 0, 0.3136F, TestName = "Gravity (current velocity at 0)")]
    [TestCase(0.1F, 0, 1, 1, 1, 1, 1.9F, 1, 0.9F, TestName = "Gravity (current velocity at 1)")]
    [TestCase(0F, 0, 1, 0, 0, 0, 1, 0, 0, TestName = "Gravity (scene with zero gravity)")]
    public void Tick_NoCollisions_Gravity(float gravity, float currPosX, float currPosY, float currVelX, float currVelY, float expPosX, float expPosY, float expVelX, float expVelY)
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(new(currPosX, currPosY, 0), new(currVelX, currVelY, 0), Vector3.One, new(), false, false, new(expPosX, expPosY, 0), new(expVelX, expVelY, 0))]);
    }

    [Test(Description = "Dead objects should be gleaned and not ticked")]
    public void Tick_Dead_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX, Vector3.One, new(), true, true, Vector3.Zero, Vector3.UnitX)]);
    }
}

[TestFixture]
public class Vector3Spec_Test
{
    [TestCase(0, 0, 0, TestName = "(0, 0, 0)")]
    [TestCase(1, 1, 1, TestName = "(1, 1, 1)")]
    public void DataMatchesSpec(float x, float y, float z)
    {
        Vector3Spec spec = new()
        {
            X = x,
            Y = y,
            Z = z
        };
        Assert.AreEqual(new Vector3(x, y, z), spec.Data);
    }
}