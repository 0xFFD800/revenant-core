using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;
using YamlDotNet.Core.Tokens;

namespace RevenantCore.Tests.Scenes;

[TestFixture]
public class Scene_Test
{
    public class FakeScene(Universe universe, IControlTracker tracker, IControlTracker keyboard, SceneSpec spec, string trigger) : Scene(universe, tracker, keyboard, spec, trigger)
    {
        internal FakeScene(Universe universe, IControlTracker tracker, SceneSpec spec, string trigger) : this(universe, tracker, new KeyboardTracker(), spec, trigger) { }
        internal FakeScene(Universe universe, SceneSpec spec, string trigger) : this(universe, new ControlTracker(), spec, trigger) { }
        internal FakeScene(IControlTracker tracker) : this(new(new FakeCore(), new([])), tracker, new(), "default") { }
        internal FakeScene(SceneSpec spec) : this(new(new FakeCore(), new([])), spec, "default") { }
        internal FakeScene() : this(new SceneSpec()) { }

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

        public void Draw(View view, Camera camera)
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

    private class MockTickable(bool isDead, bool expTick, bool expGlean) : IMockMortal, ITickable
    {
        private bool created, ticked, gleaned = false;
        public bool IsDead => isDead;

        public void Create(Scene scene, FrameTime time)
        {
            created = true;
        }

        public void Glean(Scene scene, FrameTime time)
        {
            gleaned = true;
        }

        public void Tick(Scene scene, FrameTime time)
        {
            ticked = true;
        }

        public void Validate()
        {
            Assert.IsTrue(created, "Tickable object was never created!");
            Assert.AreEqual(expGlean, gleaned, "Tickable object was not in the correct gleaning state!");
            Assert.AreEqual(expTick, ticked, "Tickable object was not in the correct ticking state!");
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
            // do nothing
        }

        /// <summary>
        /// Should be called after the draw loop has been run to verify calls to Push and Pop were balanced.
        /// </summary>
        public void Validate()
        {
            Assert.AreEqual(0, matrices, "Unbalanced calls to Push and Pop after draw loop!");
        }
    }

    private class MockCollideable(string id, Vector3 pos, Vector3 velocity, Vector3 acceleration, Vector3 size, MaterialSpec material, bool isDead, bool expGlean, Vector3 expPos, Vector3 expVelocity) : IMockMortal, ICollideable
    {
        private bool created, gleaned = false;

        /// <summary>
        /// Constructor overload for tests which don't care about position and velocity information.
        /// </summary>
        internal MockCollideable(string id, bool isDead, bool expGlean) : this(id, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, new() { Mass = 1 }, isDead, expGlean, Vector3.Zero, Vector3.Zero)
        {

        }

        private Vector3 BottomLeft => Position - new Vector3(size.X / 2, 0, size.Z / 2);
        public BoundingBox CollisionBox => new(BottomLeft, BottomLeft + size);

        public MaterialSpec Material => material;
        public Vector3 Acceleration { get; set; } = acceleration;
        public Vector3 Velocity { get; set; } = velocity;
        public Vector3 Position { get; set; } = pos;

        public bool IsDead => isDead;

        public string ID => id;

        public InteractionType[] Interactions => [];

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

    private class MockCutscene : Cutscene
    {
        private bool created, gleaned = false;
        private readonly MockCutsceneSpec spec;

        public MockCutscene(MockCutsceneSpec spec) : base(new(new FakeCore(), new([])), spec)
        {
            this.spec = spec;
            complete = spec.Complete;
        }

        public override float Z => throw new NotImplementedException();

        public override void Create(Scene scene, FrameTime time)
        {
            created = true;
        }

        public override void Draw(View view, Camera camera)
        {
            throw new NotImplementedException();
        }

        public override void Glean(Scene scene, FrameTime time)
        {
            base.Glean(scene, time);
            gleaned = true;
        }

        public override void Tick(Scene scene, FrameTime time)
        {
            throw new NotImplementedException();
        }

        public void Validate()
        {
            Assert.AreEqual(spec.ExpCreated, created, "Creation state did not match expectation");
            Assert.AreEqual(spec.ExpGleaned, gleaned, "Gleaning state did not match expectation");
        }
    }

    private class MockCutsceneSpec(bool complete, bool expSpecCreated, bool expCreated, bool expGleaned) : CutsceneSpec
    {
        public bool Complete => complete;
        public bool ExpCreated => expCreated;
        public bool ExpGleaned => expGleaned;

        private MockCutscene? cutscene;

        public override Cutscene Create(Universe universe)
        {
            cutscene = new(this);
            return cutscene;
        }

        public void Validate()
        {
            Assert.AreEqual(expSpecCreated, cutscene != null, "Cutscene creation state did not match expectation");
            cutscene?.Validate();
        }
    }

    private class MockControlTracker(ControlPositions pos, double millis, bool expCreated, bool expGleaned, bool expTick) : IControlTracker
    {
        private bool created = false, gleaned = false, ticked = false;

        internal MockControlTracker(bool expCreated, bool expGleaned, bool expTicked) : this(ControlPositions.Down, 10, expCreated, expGleaned, expTicked) { }

        public FrozenDictionary<string, ControlState> States => new List<KeyValuePair<string, ControlState>>([
            new KeyValuePair<string, ControlState>("foo", new(pos, millis))]).ToFrozenDictionary();

        public bool IsDead => false;

        public void Create(Scene scene, FrameTime time)
        {
            Assert.IsFalse(created, "Control tracker should not be created twice");
            created = true;
        }

        public void Glean(Scene scene, FrameTime time)
        {
            Assert.IsFalse(gleaned, "Control tracker should not be gleaned twice");
            gleaned = true;
        }

        public void Tick(Scene scene, FrameTime time)
        {
            ticked = true;
        }

        internal void Validate()
        {
            Assert.AreEqual(expCreated, created);
            Assert.AreEqual(expGleaned, gleaned);
            Assert.AreEqual(expTick, ticked);
        }
    }

    private class MockControllable(bool matches, bool expCreated, bool expGleaned) : IControllable, IMockMortal
    {
        private bool created = false, gleaned = false;

        public bool IsDead { get; set; } = false;

        public void Create(Scene scene, FrameTime time)
        {
            created = true;
        }

        public void Glean(Scene scene, FrameTime time)
        {
            gleaned = true;
        }

        public bool Matches(IControllable other)
        {
            return matches;
        }

        public void Validate()
        {
            Assert.AreEqual(expCreated, created);
            Assert.AreEqual(expGleaned, gleaned);
        }
    }

    private static void RunDrawLoop(Scene scene, MockVisible[] visibles)
    {
        foreach (MockVisible visible in visibles)
            scene.Add(visible, scene, new(new()));
        scene.Tick(scene, new(new()));
        MockScreen screen = new();
        foreach (DrawLayer layer in Enum.GetValues<DrawLayer>())
            scene.Draw(new(screen, new(new()), layer));
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
            new MockCollideable("foo", false, true),                           // Live collideable
            new MockCollideable("bar", true, true),                            // Dead collideable
            new MockTickable(false, false, true),                              // Live tickable
            new MockTickable(false, false, true)                               // Dead tickable
        ];
        foreach (IMockMortal mortal in mortals)
            scene.Add(mortal, scene, new(new()));
        scene.Glean(scene, new(new()));
        foreach (IMockMortal mortal in mortals)
            mortal.Validate();
    }

    private static void RunTickLoop(Scene scene, IMockMortal[] mortals)
    {
        foreach (IMockMortal mortal in mortals)
            scene.Add(mortal, scene, new(new()));
        scene.Tick(scene, new(new(new(0, 0, 0, 0, 10), new(0, 0, 0, 0, 10))));
        foreach (IMockMortal mortal in mortals)
            mortal.Validate();

        Assert.False(scene.IsDead, "Scenes currently should never die");
    }

    [Test(Description = "If there are no collisions or friction and the objects are at floor level, objects should just be moved")]
    public void Tick_NoCollisions_Move()
    {
        FakeScene scene = new(new SceneSpec()
        {
            Bounds =
            {
                X = 1000,
                Y = 1000,
                Z = 1000
            }
        });
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            new MockCollideable("foo", new(1, 0, 1),   Vector3.UnitX, Vector3.Zero, Vector3.One, new() { Mass = 1 }, false, false, new(11, 0, 1),   Vector3.UnitX),
            new MockCollideable("bar", new(1, 0, 101), Vector3.UnitZ, Vector3.Zero, Vector3.One, new() { Mass = 1 }, false, false, new(1,  0, 111), Vector3.UnitZ)
        ]);
    }

    [TestCase(0.2F, 10, 20, 0, 0, 0, 0, 10, 10, 0, -2, TestName = "Gravity (current velocity at 0)")]
    [TestCase(0.1F, 10, 1, 5, 10, 0, 0, 60, 96, 5, 9, TestName = "Gravity (current velocity at 1)")]
    [TestCase(0.1F, 10, 10, 0, 0, 1, 1, 60, 55, 10, 9, TestName = "Gravity (nonzero acceleration)")]
    [TestCase(0.0F, 10, 1, 1, 0, 0, 0, 20, 1, 1, 0, TestName = "Gravity (scene with zero gravity)")]
    public void Tick_NoCollisions_Gravity(float gravity, float currPosX, float currPosY, float currVelX, float currVelY, float currAccX, float currAccY, float expPosX, float expPosY, float expVelX, float expVelY)
    {
        FakeScene scene = new(new SceneSpec()
        {
            Gravity = gravity
        });
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [new MockCollideable("foo", new(currPosX, currPosY, 10), new(currVelX, currVelY, 0), new(currAccX, currAccY, 0), Vector3.One, new() { Mass = 1 }, false, false, new(expPosX, expPosY, 10), new(expVelX, expVelY, 0))]);
    }

    [Test]
    public void Tick_Gravity_Bounce()
    {
        SceneSpec spec = new()
        {
            Gravity = 0.1F
        };
        spec.Walls[WallSide.Floor].MaterialAbsorption = 1;
        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [new MockCollideable("foo", new(40, 1, 40), new(), new(), Vector3.One, new() { Mass = 1, MaterialAbsorption = 1 }, false, false, new(40, 1, 40), new(0, 0.5F, 0))]);
    }

    [Test]
    public void Tick_Overlapping_Separate()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            new MockCollideable("foo", new(40, 0, 40), new(), new(), Vector3.One, new() { Mass = 1, MaterialAbsorption = 1 }, false, false, new(39.5F, 0, 40), new(0, 0, 0)),
            new MockCollideable("bar", new(40, 0, 40), new(), new(), Vector3.One, new() { Mass = 1, MaterialAbsorption = 1 }, false, false, new(40.5F, 0, 40), new(0, 0, 0))
        ]);
    }

    [TestCase(0, 0, 1, 0, 10, 1, TestName = "Friction (frictionless scene)")]
    [TestCase(1, 0, 1, 1, 0, 0, TestName = "Friction (full friction floor)")]
    [TestCase(0, 1, 1, 1, 60, 11, TestName = "Friction (full friction collideable)", Description = "A collideable's own friction should not impact its acceleration")]
    [TestCase(0.5F, 0.5F, 1, 1, 30, 5.5F, TestName = "Friction (friction affects acceleration)")]
    [TestCase(0.5F, 0.5F, 1, 0, 5, 0.5F, TestName = "Friction (friction decelerates)")]
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
        RunTickLoop(scene, [new MockCollideable("foo", new(1, 0, 1), Vector3.UnitX * currVel, Vector3.UnitX * currAcc, Vector3.One, new() { Friction = collideableFriction, Mass = 1 }, false, false, new Vector3(expPos + 1, 0, 1), Vector3.UnitX * expVel)]);
    }

    [Test(Description = "An object should not move if its velocity does not exceed its static friction")]
    public void Tick_Floor_StaticFriction()
    {
        FakeScene scene = new(new SceneSpec());
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [new MockCollideable("foo", new(1, 0, 1), Vector3.UnitX * 0.0025F, Vector3.Zero, Vector3.One, new() { StaticFriction = 0.0025F, Mass = 1 }, false, false, new(1, 0, 1), Vector3.Zero)]);
    }

    [Test(Description = "An object falling with walls on two sides and other collideables on two sides should input friction from all of them.")]
    public void Tick_Collideable_Friction()
    {
        MaterialSpec material = new()
        {
            Friction = 0.1F,
            Mass = 1
        };
        SceneSpec spec = new()
        {
            Bounds = new()
            {
                X = 10,
                Y = 100,
                Z = 10
            },
            Gravity = 0.1F,
        };
        spec.Walls[WallSide.Left] = material;
        spec.Walls[WallSide.Far] = material;

        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            // The object which is descending
            new MockCollideable("foo", new(0.5F, 19, 9.5F), new(0, -1, 0), Vector3.Zero, Vector3.One,   material, false, false, new(0.5F, 9.1585016F, 9.5F), new(0, -1.3121998F, 0)),
            // The object on the near side of the descending object
            new MockCollideable("bar", new(0.5F, 0, 8.5F), Vector3.Zero,  Vector3.Zero, new(1, 20, 1), material, false, false, new(0.5F, 0, 8.5F), Vector3.Zero),
            // The object to the right of the descending object
            new MockCollideable("baz", new(1.5F, 0, 9.5F), Vector3.Zero,  Vector3.Zero, new(1, 20, 1), material, false, false, new(1.5F, 0, 9.5F), Vector3.Zero)
        ]);
    }

    [TestCase(null, 1F, 5.5F, 6, -1, 0, 0.5F, 6, 0, 0, TestName = "Collide with Wall (direct; all absorbed by wall)")]
    [TestCase(1F, null, 5.5F, 6, -1, 0, 0.5F, 6, 0, 0, TestName = "Collide with Wall (direct; all absorbed by collideable)")]
    [TestCase(1F, 1F, 5.5F, 6, -1, 0, 5.5F, 6, 1, 0, TestName = "Collide with Wall (direct; all reflected)")]
    [TestCase(null, 1F, 5.5F, 1, -1, 1, 0.5F, 11, 0, 1, TestName = "Collide with Wall (indirect; all absorbed by wall)", Description = "Collision should only absorb velocity parallel to the wall")]
    [TestCase(1F, null, 5.5F, 1, -1, 1, 0.5F, 11, 0, 1, TestName = "Collide with Wall (indirect; all absorbed by collideable)", Description = "Collision should only absorb velocity parallel to the wall")]
    [TestCase(1F, 1F, 5.5F, 1, -1, 1, 5.5F, 11, 1, 1, TestName = "Collide with Wall (indirect; all reflected)")]
    [TestCase(2F, 2F, 5.5F, 6, -1, 0, 1.75F, 6, 0.25F, 0, TestName = "Collide with Wall (direct; some absorbed)")]
    [TestCase(2F, 2F, 5.5F, 1, -1, 1, 1.75F, 11, 0.25F, 1, TestName = "Collide with Wall (indirect; some absorbed)", Description = "Collision should only absorb velocity parallel to the wall")]
    public void Tick_Collideable_Wall(float? wallAbsorption, float? collideableAbsorption, float currPosX, float currPosZ, float currVelX, float currVelZ, float expPosX, float expPosZ, float expVelX, float expVelZ)
    {
        SceneSpec spec = new()
        {
            Bounds = new()
            {
                X = 12,
                Y = 12,
                Z = 12
            }
        };
        spec.Walls[WallSide.Left] = new()
        {
            MaterialAbsorption = wallAbsorption
        };
        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [new MockCollideable("foo", new(currPosX, 0, currPosZ), new(currVelX, 0, currVelZ), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = collideableAbsorption, Mass = 1 }, false, false, new(expPosX, 0, expPosZ), new(expVelX, 0, expVelZ))]);
    }

    [Test]
    public void Tick_SuspendedWall_TravelThrough()
    {
        SceneSpec spec = new()
        {
            Gravity = 0.1F
        };
        FakeScene scene = new(spec);
        scene.Create(scene, new(new()));
        Assert.AreEqual(false, scene.IsSuspended(WallSide.Floor));
        scene.SetSuspended(WallSide.Floor, true);
        Assert.AreEqual(true, scene.IsSuspended(WallSide.Floor));
        RunTickLoop(scene, [new MockCollideable("foo", new(40, 0, 40), new(), new(), Vector3.One, new() { Mass = 1 }, false, false, new(40, -5, 40), new(0, -1, 0))]);
    }

    [TestCase(1F, 1F, 14, 36, 1, -1, 16, 34, -1, 1, TestName = "Collide Straight (same mass and speed, no absorption)")]
    [TestCase(1F, 2F, 14, 36, 1, -1, 18, 32, -0.5F, 0.5F, TestName = "Collide Straight (Absorption)")]
    [TestCase(0.5F, 1F, 40, 76, 3, -3, 29.5F, 127, -1.5F, 6, TestName = "Collide Straight (Different Masses)")]
    [TestCase(1F, 1F, 40, 76, 4, -2, 26, 110, -2, 4, TestName = "Collide Straight (Different Speeds)")]
    public void Tick_Collideable_CollideStraight(float? mass2, float? absorption2, float pos1, float pos2, float vel1, float vel2, float expPos1, float expPos2, float expVel1, float expVel2)
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            new MockCollideable("foo", new(pos1, 0, 40), new(vel1, 0, 0), Vector3.Zero, Vector3.One * 10 * Math.Abs(vel1), new() { MaterialAbsorption = 1, Mass = 1 }, false, false, new(expPos1, 0, 40), new(expVel1, 0, 0)),
            new MockCollideable("bar", new(pos2, 0, 40), new(vel2, 0, 0), Vector3.Zero, Vector3.One * 10 * Math.Abs(vel2), new() { MaterialAbsorption = absorption2, Mass = mass2 }, false, false, new(expPos2, 0, 40), new(expVel2, 0, 0))
        ]);
    }

    [TestCase(1F, 1F, 10, 10, 23, 27, 0.75F, 1, -0.75F, -1, 14.5F, 16, 18.5F, 21, -0.75F, -1, 0.75F, 1, TestName = "Collide Diagonal (same mass, no absorption)")]
    [TestCase(1F, 2F, 10, 10, 23, 27, 0.75F, 1, -0.75F, -1, 15.25F, 16, 17.75F, 21, -0.375F, -1, 0.375F, 1, TestName = "Collide Diagonal (Absorption)")]
    [TestCase(0.5F, 1F, 10, 10, 46, 59, 2.25F, 3, -2.25F, -3, 25.75F, 31, 37, 47, -1.125F, -1.5F, 4.5F, 6, TestName = "Collide Diagonal (Different Masses)")]
    [TestCase(1F, 1F, 1, 10, 18, 18, 1.25F, 0, -0.75F, -1, 9.5F, 8, 14.5F, 10, -0.75F, -1, 1.25F, 0, TestName = "Collide Oblique (same mass, no absorption)")]
    [TestCase(1F, 2F, 1, 10, 18, 18, 1.25F, 0, -0.75F, -1, 10.25F, 8, 13.25F, 10, -0.375F, -1, 0.625F, 0, TestName = "Collide Oblique (Absorption)")]
    [TestCase(0.5F, 1F, 1, 30, 50, 55, 3.75F, 0, -2.25F, -3, 28.75F, 27, 47, 31, -1.125F, -1.5F, 7.5F, 0, TestName = "Collide Oblique (Different Masses)")]
    public void Tick_Collideable_CollideOblique(float? mass2, float? absorption2, float posX1, float posZ1, float posX2, float posZ2, float velX1, float velZ1, float velX2, float velZ2, float expPosX1, float expPosZ1, float expPosX2, float expPosZ2, float expVelX1, float expVelZ1, float expVelX2, float expVelZ2)
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            new MockCollideable("foo", new(posX1, 0, posZ1), new(velX1, 0, velZ1), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = 1, Mass = 1 }, false, false, new(expPosX1, 0, expPosZ1), new(expVelX1, 0, expVelZ1)),
            new MockCollideable("bar", new(posX2, 0, posZ2), new(velX2, 0, velZ2), Vector3.Zero, Vector3.One, new() { MaterialAbsorption = absorption2, Mass = mass2 }, false, false, new(expPosX2, 0, expPosZ2), new(expVelX2, 0, expVelZ2))
        ]);
    }

    [Test(Description = "Dead objects should be gleaned and not ticked")]
    public void Tick_Dead_Gleaned()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [
            new MockCollideable("foo", Vector3.Zero, Vector3.UnitX, Vector3.Zero, Vector3.One, new() { Mass = 1 }, true, true, Vector3.Zero, Vector3.UnitX),
            new MockTickable(true, false, true),
            new MockControllable(false, true, true)
            {
                IsDead = true
            }
        ]);
    }

    [Test(Description = "Living objects should be ticked and not gleaned")]
    public void Tick_Living_Tick()
    {
        FakeScene scene = new();
        scene.Create(scene, new(new()));
        RunTickLoop(scene, [new MockTickable(false, true, false)]);
    }

    [TestCase(false, TestName = "Create Trigger (Living)", Description = "Specified living trigger should be created and added")]
    [TestCase(true, TestName = "Create Trigger (Dead)", Description = "Specified dead trigger should be gleaned and not created")]
    public void Create_Trigger_Add(bool complete)
    {
        MockCutsceneSpec defSpec = new(false, false, false, false);
        MockCutsceneSpec testSpec = new(complete, true, !complete, complete);
        SceneSpec spec = new()
        {
            Triggers = new()
            {
                { "testTrigger", testSpec }
            }
        };
        FakeScene scene = new(new(new FakeCore(), new([])), spec, "testTrigger");
        scene.Create(scene, new(new()));
        defSpec.Validate();
        testSpec.Validate();
    }

    [Test]
    public void Control_Existing_Return()
    {
        MockControlTracker tracker = new(true, false, false);
        Scene scene = new(new(new FakeCore(), new([])), tracker, new KeyboardTracker(), new(), "default");
        scene.Create(scene, new(new()));
        Assert.AreEqual(new ControlState(ControlPositions.Down, 10), scene.GetControlState("foo"));
        tracker.Validate();
    }

    [Test]
    public void Control_None_Default()
    {
        MockControlTracker tracker = new(true, false, false);
        Scene scene = new(new(new FakeCore(), new([])), tracker, new KeyboardTracker(), new(), "default");
        scene.Create(scene, new(new()));
        Assert.AreEqual(new ControlState(ControlPositions.Up, 0), scene.GetControlState("bar"));
        tracker.Validate();
    }

    [TestCase(false, TestName = "TryGetMoveable_Absent_False")]
    [TestCase(true, TestName = "TryGetMoveable_Present_TrueOut")]
    public void TryGetMoveable(bool present)
    {
        FakeScene scene = new();
        if (present)
            scene.Add(new MockCollideable("foo", false, false), scene, new(new()));
        Assert.AreEqual(present, scene.TryGetMoveable("foo", out IMoveable? moveable));
        Assert.AreEqual(present, moveable != null);
    }

    [TestCase(false, false, false, ControlPositions.Down, TestName = "IsCapturing_NotCaptured_True")]
    [TestCase(true, false, true, ControlPositions.Down, TestName = "IsCapturing_CapturingMatches_True")]
    [TestCase(true, false, false, ControlPositions.Up, TestName = "IsCapturing_NoMatch_False")]
    [TestCase(true, true, false, ControlPositions.Up, TestName = "IsCapturing_NullMatch_False")]
    public void IsCapturing(bool hasCapture, bool matchIsNull, bool captureMatches, ControlPositions expPos)
    {
        FakeScene scene = new(new MockControlTracker(true, false, false));
        MockControllable capturing = new(captureMatches, hasCapture, false);
        if (hasCapture)
            scene.Add(capturing, scene, new(new()));
        MockControllable controllable = new(false, false, false);
        ControlState c = scene.GetControlState(matchIsNull ? null : controllable, "foo");
        Assert.AreEqual(expPos, c.Position);
        capturing.Validate();
        controllable.Validate();
    }

    [TestCase(false, ControlPositions.Press, 0, false, TestName = "GetPressedKeys_NoCapture_False")]
    [TestCase(true, ControlPositions.Press, 0, true, TestName = "GetPressedKeys_Press_True")]
    [TestCase(true, ControlPositions.Down, KeyboardTracker.RepeatMillis, false, TestName = "GetPressedKeys_DownShort_False")]
    [TestCase(true, ControlPositions.Down, KeyboardTracker.RepeatMillis + 1, true, TestName = "GetPressedKeys_DownSustained_True")]
    public void GetPressedKeys(bool capturing, ControlPositions pos, double millis, bool expHasKey)
    {
        FakeScene scene = new(new Universe(new FakeCore(), new([])), new ControlTracker(), new MockControlTracker(pos, millis, false, false, false), new(), "default");
        if (!capturing)
            scene.Add(new MockControllable(false, false, false), scene, new(new()));
        string[] keys = scene.GetPressedKeys(null);
        Assert.AreEqual(expHasKey, keys.Length > 0);
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