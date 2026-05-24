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
        internal MockCollideable(bool isDead, bool expGlean) : this(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, new() { Mass = 1 }, isDead, expGlean, Vector3.Zero, Vector3.Zero)
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
        scene.Tick(scene, new(new(new(0, 0, 0, 0, 10), new(0, 0, 0, 0, 10))));
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
            new(Vector3.Zero,        Vector3.UnitX, Vector3.Zero, Vector3.One, new() { Mass = 1 }, false, false, Vector3.UnitX *  10, Vector3.UnitX),
            new(Vector3.UnitZ * 100, Vector3.UnitZ, Vector3.Zero, Vector3.One, new() { Mass = 1 }, false, false, Vector3.UnitZ * 110, Vector3.UnitZ)
        ]);
    }

    [TestCase(0.2F, 10, 20, 0, 0, 0, 0, 10, 18, 0, -2, TestName = "Gravity (current velocity at 0)")]
    [TestCase(0.1F, 10, 1, 5, 10, 0, 0, 60, 100.5F, 5, 9, TestName = "Gravity (current velocity at 1)")]
    [TestCase(0.1F, 10, 10, 0, 0, 1, 1, 60, 50.5F, 10, 9, TestName = "Gravity (nonzero acceleration)")]
    [TestCase(0.0F, 10, 1, 1, 0, 0, 0, 20, 1, 1, 0, TestName = "Gravity (scene with zero gravity)")]
    public void Tick_NoCollisions_Gravity(float gravity, float currPosX, float currPosY, float currVelX, float currVelY, float currAccX, float currAccY, float expPosX, float expPosY, float expVelX, float expVelY)
    {
        FakeScene scene = new(new()
        {
            Gravity = gravity
        });
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(new(currPosX, currPosY, 10), new(currVelX, currVelY, 0), new(currAccX, currAccY, 0), Vector3.One, new() { Mass = 1 }, false, false, new(expPosX, expPosY, 10), new(expVelX, expVelY, 0))]);
    }

    [TestCase(0, 0, 1, 0, 10, 1, TestName = "Friction (frictionless scene)")]
    [TestCase(1, 0, 1, 1, 0, 0, TestName = "Friction (full friction floor)")]
    [TestCase(0, 1, 1, 1, 0, 0, TestName = "Friction (full friction collideable)")]
    [TestCase(0.5F, 0.5F, 1, 0.075F, 10, 1, TestName = "Friction (friction erases acceleration)")]
    [TestCase(0.5F, 0.5F, 1, 0, 6.25F, 0.25F, TestName = "Friction (friction decelerates)")]
    [TestCase(0.5F, 0.5F, 0, 0, 0, 0, TestName = "Friction (unmoving object)")]
    public void Tick_Floor_Friction(float floorFriction, float collideableFriction, float currVel, float currAcc, float expPos, float expVel)
    {
        SceneSpec spec = new();
        spec.Walls[WallSide.Floor] = new()
        {
            Friction = floorFriction
        };
        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX * currVel, Vector3.UnitX * currAcc, Vector3.One, new() { Friction = collideableFriction, Mass = 1 }, false, false, Vector3.UnitX * expPos, Vector3.UnitX * expVel)]);
    }

    [Test(Description = "An object should not move if its velocity does not exceed its static friction")]
    public void Tick_Floor_StaticFriction()
    {
        FakeScene scene = new(new());
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX * 0.0025F, Vector3.Zero, Vector3.One, new() { StaticFriction = 0.0025F, Mass = 1 }, false, false, Vector3.Zero, Vector3.Zero)]);
    }

    [Test(Description = "An object falling with walls on two sides and other collideables on two sides should input friction from all of them.")]
    public void Tick_Collideable_Friction()
    {
        MaterialSpec material = new()
        {
            Friction = 0.75F,
            Mass = 1
        };
        SceneSpec spec = new()
        {
            Bounds = new()
            {
                X = 10,
                Y = 10,
                Z = 10
            },
            Gravity = 0.1F,
        };
        spec.Walls[WallSide.Left] = material;
        spec.Walls[WallSide.Far] = material;

        FakeScene scene = new(spec);
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

    [TestCase(null, 1F, 5, 5, -1, 0, 0, 5, 0, 0, TestName = "Collide with Wall (direct; all absorbed by wall)")]
    [TestCase(1F, null, 5, 5, -1, 0, 0, 5, 0, 0, TestName = "Collide with Wall (direct; all absorbed by collideable)")]
    [TestCase(1F, 1F, 5, 5, -1, 0, 5, 5, 1, 0, TestName = "Collide with Wall (direct; all reflected)")]
    [TestCase(null, 1F, 5, 0, -1, 1, 0, 10, 0, 1, TestName = "Collide with Wall (indirect; all absorbed by wall)", Description = "Collision should only absorb velocity parallel to the wall")]
    [TestCase(1F, null, 5, 0, -1, 1, 0, 10, 0, 1, TestName = "Collide with Wall (indirect; all absorbed by collideable)", Description = "Collision should only absorb velocity parallel to the wall")]
    [TestCase(1F, 1F, 5, 0, -1, 1, 5, 10, 1, 1, TestName = "Collide with Wall (indirect; all reflected)")]
    [TestCase(2F, 2F, 5, 5, -1, 0, 1.25F, 5, 0.25F, 0, TestName = "Collide with Wall (direct; some absorbed)")]
    [TestCase(2F, 2F, 5, 0, -1, 1, 1.25F, 10, 0.25F, 1, TestName = "Collide with Wall (indirect; some absorbed)", Description = "Collision should only absorb velocity parallel to the wall")]
    public void Tick_Collideable_Wall(float? wallAbsorption, float? collideableAbsorption, float currPosX, float currPosZ, float currVelX, float currVelZ, float expPosX, float expPosZ, float expVelX, float expVelZ)
    {
        SceneSpec spec = new()
        {
            Bounds = new()
            {
                X = 10,
                Y = 10,
                Z = 10
            }
        };
        spec.Walls[WallSide.Left] = new()
        {
            MaterialAbsorption = wallAbsorption
        };
        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(new(currPosX, 0, currPosZ), new(currVelX, 0, currVelZ), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = collideableAbsorption, Mass = 1 }, false, false, new(expPosX, 0, expPosZ), new(expVelX, 0, expVelZ))]);
    }

    [TestCase(1F, 1F, 4, 17, 1, -1, 6, 15, -1, 1, TestName = "Collide Straight (same mass and speed, no absorption)")]
    [TestCase(1F, 0.5F, 4, 17, 1, -1, 8, 13, -0.5F, 0.5F, TestName = "Collide Straight (Absorption)")]
    [TestCase(0.5F, 1F, 0, 31, 3, -3, 5, 36, -2, 4, TestName = "Collide Straight (Different Masses)")]
    [TestCase(1F, 1F, 0, 31, 4, -2, 10, 41, -2, 4, TestName = "Collide Straight (Different Speeds)")]
    public void Tick_Collideable_CollideStraight(float? mass2, float? absorption2, float pos1, float pos2, float vel1, float vel2, float expPos1, float expPos2, float expVel1, float expVel2)
    {
        FakeScene scene = new(new());
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [
            new(new(pos1, 0, 0), new(vel1, 0, 0), Vector3.Zero, Vector3.One, new(), false, false, new(expPos1, 0, 0), new(expVel1, 0, 0)),
            new(new(pos2, 0, 0), new(vel2, 0, 0), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = absorption2, Mass = mass2 }, false, false, new(expPos2, 0, 0), new(expVel2, 0, 0))
        ]);
    }

    [TestCase(1F, 1F, 10, 10, 23, 27, 0.75F, 1, -0.75F, -1, 14.5F, 16, 18.5F, 21, -0.75F, -1, 0.75F, 1, TestName = "Collide Diagonal (same mass, no absorption)")]
    [TestCase(1F, 0.5F, 10, 10, 23, 27, 0.75F, 1, -0.75F, -1, 15.25F, 17, 17.75F, 20, -0.375F, -0.5F, 0.375F, 0.5F, TestName = "Collide Diagonal (Absorption)")]
    [TestCase(0.5F, 1F, 10, 10, 46, 59, 2.25F, 3, -2.25F, -3, 24.5F, 30, 33.5F, 42, -1.5F, -2, 3, 4, TestName = "Collide Diagonal (Different Masses)")]
    [TestCase(1F, 1F, 0, 10, 17, 18, 1.25F, 0, -0.75F, -1, 8.5F, 10, 13.5F, 8, -0.75F, 0, 1.25F, -1, TestName = "Collide Oblique (same mass, no absorption)")]
    [TestCase(1F, 0.5F, 0, 10, 17, 18, 1.25F, 0, -0.75F, -1, 9.25F, 10, 12.25F, 8, -0.375F, 0, 0.625F, -1F, TestName = "Collide Oblique (Absorption)")]
    [TestCase(0.5F, 1F, 0, 30, 49, 55, 3.75F, 0, -2.25F, -3, 27.75F, 30, 46, 28, -1.125F, 0, 7.5F, -1, TestName = "Collide Oblique (Different Masses)")]
    public void Tick_Collideable_CollideOblique(float? mass2, float? absorption2, float posX1, float posZ1, float posX2, float posZ2, float velX1, float velZ1, float velX2, float velZ2, float expPosX1, float expPosX2, float expPosZ1, float expPosZ2, float expVelX1, float expVelX2, float expVelZ1, float expVelZ2)
    {
        FakeScene scene = new(new());
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [
            new(new(posX1, 0, posZ1), new(velX1, 0, velZ1), Vector3.Zero, Vector3.One, new(), false, false, new(expPosX1, 0, expPosZ1), new(expVelX1, 0, expVelZ1)),
            new(new(posX2, 0, posZ2), new(velX2, 0, velZ2), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = absorption2, Mass = mass2 }, false, false, new(expPosX2, 0, expPosZ2), new(expVelX2, 0, expVelZ2))
        ]);
    }

    [Test(Description = "Dead objects should be gleaned and not ticked")]
    public void Tick_Dead_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunCollisionsLoop(scene, [new(Vector3.Zero, Vector3.UnitX, Vector3.Zero, Vector3.One, new() { Mass = 1 }, true, true, Vector3.Zero, Vector3.UnitX)]);
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

// Need to test suspended wall collisions...