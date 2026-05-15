using Microsoft.Xna.Framework;
using NUnit.Framework.Internal;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Scenes;

file class FakeMoveable(Vector3[] positions) : IMoveable
{
    private int index = 0;
    public Vector3 Position { get => positions[index++]; set => throw new NotImplementedException(); }

    public bool IsDead { get; private set; } = false;

    public void Create(Scene scene, FrameTime time)
    {
        IsDead = false;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        IsDead = true;
    }
}

[TestFixture]
public class MoveableTracker_Test
{
        [Test(Description = "If the movable being tracked dies, so should the tracker.")]
    public void IsDead_DeadMoveable_DeadTracker()
    {
        FakeMoveable m = new([new(), new()]);
        MoveableTracker t = new(m, 1, 1, 1, 1);
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        Assert.IsFalse(t.IsDead);
        m.Glean(s, f);
        Assert.IsTrue(t.IsDead);
    }

    [Test(Description = "Gleaning the tracker shouldn't glean the movable")]
    public void Glean_LiveMoveable_DontGleanMoveable()
    {
        FakeMoveable m = new([new(), new()]);
        MoveableTracker t = new(m, 1, 1, 1, 1);
        Scene s = new();
        FrameTime f = new(new());
        m.Create(s, f);
        t.Create(s, f);
        t.Glean(s, f);
        Assert.IsFalse(m.IsDead);
    }

    [Test(Description = "If it has not been queueInterval milliseconds since the last update, to not update the queue.")]
    public void Tick_NoUpdate_NoTarget()
    {
        FakeMoveable m = new([Vector3.Zero, Vector3.UnitX, Vector3.UnitZ]);
        MoveableTracker t = new(m, 1, 1, 1, 1);
        Assert.AreEqual(Vector3.Zero, t.CurrValue, "Sanity Check");
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, f);
        Assert.AreEqual(Vector3.Zero, t.CurrValue);
    }

    [Test(Description = "Do not dequeue a new target until the queue count has exceeded queueDepth")]
    public void Tick_PartialFullQueue_NoDequeue()
    {
        FakeMoveable m = new([Vector3.Zero, Vector3.UnitX, Vector3.UnitZ]);
        MoveableTracker t = new(m, 2, 1, 1, 1);
        Assert.AreEqual(Vector3.Zero, t.CurrValue, "Sanity Check");
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, new(new(new(0, 0, 0, 0, 2), new())));
        Assert.AreEqual(Vector3.Zero, t.CurrValue);
    }

    [TestCase(1,    false, 1, 1, 1,    true,  TestName = "Step of 1, speed of 1, no smoothing")]
    [TestCase(1,    false, 2, 1, 1,    true,  TestName = "Step of 1, speed of 2, no smoothing")]
    [TestCase(2,    false, 1, 1, 1,    false, TestName = "Step of 2, speed of 1, no smoothing")]
    [TestCase(2,    false, 2, 1, 2,    true,  TestName = "Step of 2, speed of 2, no smoothing")]
    [TestCase(2,    true,  2, 2, 1,    false, TestName = "Step of 2, speed of 2, smoothing")]
    [TestCase(1,    true,  2, 2, 0.5F, false, TestName = "Step of 1, speed of 2, smoothing")]
    [TestCase(2,    false, 2, 2, 2,    true,  TestName = "Don't smooth if the queue isn't empty")]
    [TestCase(0.5F, true,  2, 2, 0.5F, true,  TestName = "Don't smooth if the remaining length is less than dist / smoothing")]
    public void Tick_FullQueue_MoveTo(float step, bool queueEmpty, double speed, double smoothing, float expPos, bool expReachDest)
    {
        Vector3 stepVec = Vector3.UnitX * step;
        FakeMoveable m = new([Vector3.Zero, stepVec, queueEmpty ? stepVec : Vector3.UnitZ]);
        MoveableTracker t = new(m, 0, 1, speed, smoothing);
        Assert.AreEqual(Vector3.Zero, t.CurrValue, "Sanity Check");
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, new(new(new(0, 0, 0, 0, 2), new(0, 0, 0, 0, 1))));
        Vector3 expVec = Vector3.UnitX * expPos;
        Assert.AreEqual(expVec, t.CurrValue);

        // If we expect to reach our destination, a subsequent tick which doesn't enqueue a new position shouldn't change the position
        if (expReachDest)
        {
            t.Tick(s, new(new(new(0, 0, 0, 0, 2), new(0, 0, 0, 0, 1))));
            Assert.AreEqual(expVec, t.CurrValue);
        }
    }
}

file class FakeCollideable(Vector3[] positions, Vector3[] velocities) : FakeMoveable(positions), ICollideable
{
    private int index = 0;

    public BoundingBox CollisionBox => new(Position, Position);
    public float? Mass => null;
    public Vector3 Velocity { get => velocities[index++]; set => throw new NotImplementedException(); }
}

[TestFixture]
public class ForwardLookingTracker_Test
{
    [TestCase(1, 0, 0, 1, 0, TestName = "Velocity == 0")]
    [TestCase(0, 1, 1, 0, 1, TestName = "Position == 0")]
    [TestCase(1, 2, 1, 1, 2, TestName = "Velocity != Position")]
    [TestCase(1, 2, 2, 1, 4, TestName = "VelocityFactor != 0")]
    public void Tick_FullQueue_MoveTo(float step, float velocity, float velocityFactor, float expPosX, float expPosZ)
    {
        Vector3 stepVec = Vector3.UnitX * step;
        Vector3 velVec = Vector3.UnitZ * velocity;
        FakeCollideable c = new([Vector3.Zero, stepVec, stepVec], [Vector3.Zero, velVec, stepVec]);
        ForwardLookingTracker t = new(c, 0, 1, 10, 1, velocityFactor);
        Assert.AreEqual(Vector3.Zero, t.CurrValue, "Sanity Check");
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, new(new(new(0, 0, 0, 0, 2), new(0, 0, 0, 0, 1))));
        Vector3 expVec = new(expPosX, 0, expPosZ);
        Assert.AreEqual(expVec, t.CurrValue);
    }
}