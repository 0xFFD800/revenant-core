using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Tests.Scenes;

[TestFixture]
public class Scene_Test
{
    private class FakeScene(SceneSpec spec) : Scene(spec)
    {
        internal FakeScene() : this(new())
        {

        }

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

    private class MockCollideable(Vector3 pos, Vector3 velocity, Vector3 acceleration, Vector3 size, MaterialSpec material, bool isDead, bool expGlean, Vector3 expPos, Vector3 expVelocity) : IMockMortal, ICollideable
    {
        private bool created, gleaned = false;

        /// <summary>
        /// Constructor overload for tests which don't care about position and velocity information.
        /// </summary>
        internal MockCollideable(bool isDead, bool expGlean) : this(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, new(), isDead, expGlean, Vector3.Zero, Vector3.Zero)
        {

        }

        private Vector3 BottomLeft => Position - new Vector3(size.X / 2, 0, size.Z / 2);
        public BoundingBox CollisionBox => new(BottomLeft, BottomLeft + size);

        public MaterialSpec Material => material;
        public Vector3 Acceleration { get; set; } = acceleration;
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
            Assert.AreEqual(Vector3.Zero, Acceleration);
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
        scene.Tick(scene, new(new(new(0, 0, 0, 0, 10), new())));
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
            new(Vector3.Zero,        Vector3.UnitX, Vector3.Zero, Vector3.One, new(), false, false, Vector3.UnitX *  10, Vector3.UnitX),
            new(Vector3.UnitZ * 100, Vector3.UnitZ, Vector3.Zero, Vector3.One, new(), false, false, Vector3.UnitZ * 110, Vector3.UnitZ)
        ]);
    }

    [TestCase(0.2F, 0, 20, 0, 0, 0, 0, 0, 10, 0, -2, TestName = "Gravity (current velocity at 0)")]
    [TestCase(0.1F, 0, 1, 5, 10, 0, 0, 50, 96, 5, 9, TestName = "Gravity (current velocity at 1)")]
    [TestCase(0.1F, 0, 10, 0, 0, 1, 1, 50, 55, 10, 9, TestName = "Gravity (nonzero acceleration)")]
    [TestCase(0.0F, 0, 1, 1, 0, 0, 0, 1, 1, 1, 0, TestName = "Gravity (scene with zero gravity)")]
    public void Tick_NoCollisions_Gravity(float gravity, float currPosX, float currPosY, float currVelX, float currVelY, float currAccX, float currAccY, float expPosX, float expPosY, float expVelX, float expVelY)
    {
        FakeScene scene = new(new()
        {
            Gravity = gravity
        });
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(new(currPosX, currPosY, 0), new(currVelX, currVelY, 0), new(currAccX, currAccY, 0), Vector3.One, new(), false, false, new(expPosX, expPosY, 0), new(expVelX, expVelY, 0))]);
    }

    [TestCase(0, 0, 1, 0, 10, 1, TestName = "Friction (frictionless scene)")]
    [TestCase(1, 0, 1, 1, 0, 0, TestName = "Friction (full friction floor)")]
    [TestCase(0, 1, 1, 1, 0, 0, TestName = "Friction (full friction collideable)")]
    [TestCase(0.5F, 0.5F, 1, 0.075F, 10, 1, TestName = "Friction (friction erases acceleration)")]
    [TestCase(0.5F, 0.5F, 1, 0, 6.25F, 0.25F, TestName = "Friction (friction decelerates)")]
    [TestCase(0.5F, 0.5F, 0, 0, 0, 0, TestName = "Friction (unmoving object)")]
    public void Tick_Floor_Friction(float floorFriction, float collideableFriction, float currVel, float currAcc, float expPos, float expVel)
    {
        FakeScene scene = new(new()
        {
            Floor = new()
            {
                Friction = floorFriction
            }
        });
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX * currVel, Vector3.UnitX * currAcc, Vector3.One, new() { Friction = collideableFriction }, false, false, Vector3.UnitX * expPos, Vector3.UnitX * expVel)]);
    }

    [Test(Description = "An object should not move if its velocity does not exceed its static friction")]
    public void Tick_Floor_StaticFriction()
    {
        FakeScene scene = new(new());
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX * 0.0025F, Vector3.Zero, Vector3.One, new() { StaticFriction = 0.0025F }, false, false, Vector3.Zero, Vector3.Zero)]);
    }

    [Test(Description = "An object falling with walls on two sides and other collideables on two sides should input friction from all of them.")]
    public void Tick_Collideable_Friction()
    {
        MaterialSpec material = new()
        {
            Friction = 0.75F
        };
        FakeScene scene = new(new()
        {
            Bounds = new()
            {
                X = 10,
                Y = 10, 
                Z = 10
            },  
            Gravity = 0.1F,
            LeftWall = material,
            FarWall = material
        });
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [
            // The object which is descending
            new(new(0, 5, 9), new(0, -1, 0), Vector3.Zero, Vector3.One,   material, false, false, new(0, -11.1875234375F, 9), new(0, -1.2373046875F, 0)),
            // The object on the near side of the descending object
            new(new(0, 0, 8), Vector3.Zero,  Vector3.Zero, new(1, 10, 1), material, false, false, new(0, 0, 8), Vector3.Zero),
            // The object to the right of the descending object
            new(new(1, 0, 9), Vector3.Zero,  Vector3.Zero, new(1, 10, 1), material, false, false, new(1, 0, 9), Vector3.Zero)
        ]);
    }

    [Test(Description = "Dead objects should be gleaned and not ticked")]
    public void Tick_Dead_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX, Vector3.Zero, Vector3.One, new(), true, true, Vector3.Zero, Vector3.UnitX)]);
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