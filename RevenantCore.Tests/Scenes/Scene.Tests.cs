using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
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

    private class MockCollideable(bool isDead, bool expGlean) : IMockMortal, ICollideable
    {
        private bool created, gleaned = false;
    
        public BoundingBox CollisionBox => throw new NotImplementedException();

        public float? Mass => throw new NotImplementedException();

        public Vector3 Velocity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector3 Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
            Assert.AreEqual(expGlean, gleaned, "Collideable object was not in the correct gleaning state!");
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
}