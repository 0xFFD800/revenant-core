using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Entities;

file class FakeAnimationCollection() : AnimationCollection(new List<KeyValuePair<string, Animation>>().ToFrozenDictionary(), null);

file class FakeLoader() : ILoader
{
    public Drawable LoadSprite(string path)
    {
        throw new NotImplementedException();
    }
}

file class FakeScene() : Scene(new(new(new FakeLoader(), []), new([])), new(), "default");

[TestFixture]
public class NullAgent_Test
{
    [Test]
    public void Apply_DoNothing()
    {
        IAgent agent = new NullAgent();
        Entity entity = new(agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene);
        entity.Tick(new FakeScene(), new(new()));
        Assert.AreEqual(Vector3.Zero, entity.Position);
        Assert.AreEqual(Vector3.Zero, entity.Velocity);
        Assert.AreEqual(Vector3.Zero, entity.Acceleration);
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
        IAgent agent = new TrackingAgent(new FakeTracker(Vector3.UnitX * 5), 1, 10);
        Entity entity = new(agent, new FakeAnimationCollection(), new(), Vector3.One, DrawLayer.Scene)
        {
            Velocity = Vector3.UnitX * vel,
            Acceleration = Vector3.UnitX * acc
        };
        entity.Tick(new FakeScene(), new FrameTime(new()));
        Assert.AreEqual(Vector3.UnitX * expAcc, entity.Acceleration);
    }
}