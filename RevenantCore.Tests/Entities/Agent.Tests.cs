using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Entities;

file class FakeAnimationCollection() : AnimationCollection(new List<KeyValuePair<string, Animation>>().ToFrozenDictionary(), null);

file class FakeScene() : Scene(new(new FakeCore(), new([])), new(), "default");

[TestFixture]
public class NullAgent_Test
{
    [Test]
    public void Apply_DoNothing()
    {
        NullAgent agent = new();
        Entity entity = new("foo", agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene);
        Scene scene = new FakeScene();
        FrameTime time = new(new());
        entity.Create(scene, time);
        entity.Tick(scene, time);
        Assert.IsFalse(agent.IsDead);
        Assert.AreEqual(Vector3.Zero, entity.Position);
        Assert.AreEqual(Vector3.Zero, entity.Velocity);
        Assert.AreEqual(Vector3.Zero, entity.Acceleration);
        entity.Glean(scene, time);
    }
}

file class FakeTracker(Vector3 trackerTarget) : Tracker<Vector3>(10, 0)
{
    public override bool IsDead => false;

    protected override Vector3 NextTarget => trackerTarget;

    protected override Vector3 Interpolate(Vector3 current, Vector3 target, FrameTime time) => trackerTarget;
}

[TestFixture]
public class TrackerAgent_Test
{
    [TestCase(0, 0, 1, TestName = "Apply (Stationary)")]
    [TestCase(0, -1, 0, TestName = "Apply (Change Direction)")]
    [TestCase(9, 0, 0, TestName = "Apply (At Top Speed)")]
    public void Apply_AccelTowardsTarget(float vel, float acc, float expAcc)
    {
        TrackingAgent agent = new(new FakeTracker(Vector3.UnitX * 5), 1, 10);
        Entity entity = new("foo", agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene)
        {
            Velocity = Vector3.UnitX * vel,
            Acceleration = Vector3.UnitX * acc
        };
        Scene scene = new FakeScene();
        FrameTime time = new(new());
        entity.Create(scene, time);
        entity.Tick(scene, time);
        Assert.IsFalse(agent.IsDead);
        Assert.AreEqual(Vector3.UnitX * expAcc, entity.Acceleration);
        entity.Glean(scene, time);
    }
}

[TestFixture]
public class InputAgent_Test
{
    private class FakeControlTracker(ControlPositions walkLeft, ControlPositions walkRight, ControlPositions walkUp, ControlPositions walkDown) : IControlTracker
    {
        public FrozenDictionary<string, ControlState> States => new List<KeyValuePair<string, ControlState>>([
            new("walkLeft", new(walkLeft, 0)),
            new("walkRight", new(walkRight, 0)),
            new("walkUp", new(walkUp, 0)),
            new("walkDown", new(walkDown, 0))
        ]).ToFrozenDictionary();

        public bool IsDead => false;

        public void Create(Scene scene, FrameTime time) { }

        public void Glean(Scene scene, FrameTime time) { }

        public void Tick(Scene scene, FrameTime time) { }
    }

    [TestCase(ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, 0, 0, 0, TestName = "Apply_Controls_AddAccel (None down -> no accel)")]
    [TestCase(ControlPositions.Down, ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, 0, -1, 0, TestName = "Apply_Controls_AddAccel (Left, X = -1)")]
    [TestCase(ControlPositions.Up, ControlPositions.Down, ControlPositions.Up, ControlPositions.Up, 0, 1, 0, TestName = "Apply_Controls_AddAccel (Right, X = 1)")]
    [TestCase(ControlPositions.Up, ControlPositions.Up, ControlPositions.Down, ControlPositions.Up, 0, 0, -1, TestName = "Apply_Controls_AddAccel (Up, Z = -1)")]
    [TestCase(ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, ControlPositions.Down, 0, 0, 1, TestName = "Apply_Controls_AddAccel (Down, Z = 1)")]
    [TestCase(ControlPositions.Down, ControlPositions.Down, ControlPositions.Up, ControlPositions.Up, 0, 0, 0, TestName = "Apply_Controls_AddAccel (Left and Right cancel out)")]
    [TestCase(ControlPositions.Up, ControlPositions.Up, ControlPositions.Down, ControlPositions.Down, 0, 0, 0, TestName = "Apply_Controls_AddAccel (Up and Down cancel out)")]
    [TestCase(ControlPositions.Release, ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, 0, 0, 0, TestName = "Apply_Controls_AddAccel (Release is Up)")]
    [TestCase(ControlPositions.Press, ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, 0, -1, 0, TestName = "Apply_Controls_AddAccel (Press is Down)")]
    [TestCase(ControlPositions.Down, ControlPositions.Up, ControlPositions.Up, ControlPositions.Up, 10, -1, 0, TestName = "Apply_Controls_AddAccel (Can Decelerate from Top Speed)")]
    [TestCase(ControlPositions.Up, ControlPositions.Down, ControlPositions.Up, ControlPositions.Up, 10, 0, 0, TestName = "Apply_Controls_AddAccel (Already at top speed -> no accel)")]
    public void Apply_Controls_AddAccel(ControlPositions walkLeft, ControlPositions walkRight, ControlPositions walkUp, ControlPositions walkDown, float vel, float expX, float expZ)
    {
        InputAgent agent = new(new()
        {
            Acceleration = 1F,
            TopSpeed = 10F
        });
        Entity entity = new("foo", agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene)
        {
            Velocity = Vector3.UnitX * vel
        };
        Scene scene = new(new(new FakeCore(), new([])), new FakeControlTracker(walkLeft, walkRight, walkUp, walkDown), new(), "default");
        FrameTime time = new(new());
        entity.Create(scene, time);
        entity.Tick(scene, time);
        Assert.IsFalse(agent.IsDead);
        Assert.AreEqual(new Vector3(expX, 0, expZ), entity.Acceleration);
        entity.Glean(scene, time);
    }
}