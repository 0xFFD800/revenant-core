using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;
using static RevenantCore.Tests.Scenes.Scene_Test;

namespace RevenantCore.Tests.Scenes;

[TestFixture]
public class InteractionArea_Test
{
    [Test]
    public void NoCutscenes_Dead()
    {
        InteractionArea area = new(new() { Cutscenes = [] });
        Scene scene = new FakeScene();
        area.Create(scene, new(new()));
        area.Tick(scene, new(new()));
        Assert.IsTrue(area.IsDead);
        area.Glean(scene, new(new()));
    }

    private static void CreateFakes(InteractionType type, SubsequentBehavior behavior, out InteractionArea area, out Scene scene)
    {
        Vector3Spec bounds = new()
        {
            X = 1,
            Y = 1,
            Z = 1
        };
        area = new(new()
        {
            Cutscenes = [new SequentialBlockSpec()],
            Bounds = bounds,
            Base = bounds,
            Type = type,
            SubsequentBehavior = behavior
        });
        scene = new FakeScene();
        scene.Create(scene, new(new()));
        area.Create(scene, new(new()));
    }

    [Test]
    public void Tick_NoCapture_DoNothing()
    {
        CreateFakes(InteractionType.Enter, SubsequentBehavior.RemoveInteraction, out InteractionArea area, out Scene scene);
        scene.Add(new ConcurrentBlockSpec().Create(scene.Universe), scene, new(new()));
        scene.Add(new MockCollideable("foo", Vector3.One, Vector3.Zero, Vector3.Zero, Vector3.One, new(), false, false, Vector3.One, Vector3.Zero, [InteractionType.Enter, InteractionType.Interact]), scene, new(new()));
        area.Tick(scene, new(new()));
        Assert.IsFalse(area.IsDead);
    }

    [Test]
    public void Tick_NoCollisions_DoNothing()
    {
        CreateFakes(InteractionType.Enter, SubsequentBehavior.RemoveInteraction, out InteractionArea area, out Scene scene);
        area.Tick(scene, new(new()));
        Assert.IsFalse(area.IsDead);
    }

    [TestCase(false, false, InteractionType.Enter, false, TestName = "TickEnter_NoInteractions_DoNothing")]
    [TestCase(false, false, InteractionType.Interact, false, TestName = "TickInteract_NoInteractions_DoNothing")]
    [TestCase(true, false, InteractionType.Enter, true, TestName = "TickEnter_EnterInteraction_Trigger")]
    [TestCase(true, false, InteractionType.Interact, false, TestName = "TickInteract_EnterInteraction_DoNothing")]
    [TestCase(true, true, InteractionType.Interact, true, TestName = "TickInteract_Interaction_Trigger")]
    public void Tick_Interactions_Condition(bool enter, bool interact, InteractionType interaction, bool expTrigger)
    {
        CreateFakes(interaction, SubsequentBehavior.RemoveInteraction, out InteractionArea area, out Scene scene);
        InteractionType[] interactions = enter && interact ? [InteractionType.Enter, InteractionType.Interact] : enter ? [InteractionType.Enter] : [];
        scene.Add(new MockCollideable("foo", Vector3.One, Vector3.Zero, Vector3.Zero, Vector3.One, new(), false, false, Vector3.One, Vector3.Zero, interactions), scene, new(new()));
        area.Tick(scene, new(new()));
        Assert.AreEqual(expTrigger, area.IsDead);
    }

    private class MockCutscene(Universe universe, FakeCutsceneSpec spec, int expTripped) : InstantCutscene(universe, spec)
    {
        private int tripped = 0;
        protected override void Trip(Scene scene, FrameTime time) { tripped++; }

        internal void Validate()
        {
            Assert.AreEqual(expTripped, tripped);
        }
    };

    private class FakeCutsceneSpec(int expTripped) : CutsceneSpec()
    {
        internal MockCutscene? cutscene;
        public override Cutscene Create(Universe universe)
        {
            cutscene = new MockCutscene(universe, this, expTripped);
            return cutscene;
        }
    }

    [TestCase(SubsequentBehavior.Loop, 2, 1, TestName = "Tick_Loop_ReturnToStart")]
    [TestCase(SubsequentBehavior.RepeatLast, 1, 2, TestName = "Tick_RepeatLast_Repeat")]
    public void Tick_SubsequentBehavior(SubsequentBehavior behavior, int expFirstTripped, int expSecondTripped)
    {
        FakeCutsceneSpec[] specs = [new(expFirstTripped), new(expSecondTripped)];
        Vector3Spec bounds = new()
        {
            X = 1,
            Y = 1,
            Z = 1
        };
        InteractionArea area = new(new()
        {
            Cutscenes = specs,
            Bounds = bounds,
            Base = bounds,
            Type = InteractionType.Enter,
            SubsequentBehavior = behavior
        });
        Scene scene = new FakeScene();
        scene.Create(scene, new(new()));
        area.Create(scene, new(new()));
        scene.Add(new MockCollideable("foo", Vector3.One, Vector3.Zero, Vector3.Zero, Vector3.One, new(), false, false, Vector3.One, Vector3.Zero, [InteractionType.Enter, InteractionType.Interact]), scene, new(new()));
        area.Tick(scene, new(new()));
        scene.Tick(scene, new(new()));
        area.Tick(scene, new(new()));
        scene.Tick(scene, new(new()));
        area.Tick(scene, new(new()));
        foreach (FakeCutsceneSpec spec in specs)
            spec.cutscene?.Validate();
    }
}