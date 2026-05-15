using Microsoft.Xna.Framework;
using NUnit.Framework.Internal;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Scenes;

public class MoveableTracker_Test
{
    private class FakeMoveable(Vector3[] positions) : IMoveable
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
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, f);
        Assert.AreEqual(t.CurrValue, Vector3.Zero);
    }

    [Test(Description = "Do not dequeue a new target until the queue count has exceeded queueDepth")]
    public void Tick_PartialFullQueue_NoDequeue()
    {
        FakeMoveable m = new([Vector3.Zero, Vector3.UnitX, Vector3.UnitZ]);
        MoveableTracker t = new(m, 2, 1, 1, 1);
        Scene s = new();
        FrameTime f = new(new());
        t.Create(s, f);
        t.Tick(s, new(new(new(0, 0, 0, 0, 2), new())));
        Assert.AreEqual(t.CurrValue, Vector3.Zero);
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