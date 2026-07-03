using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Entities;
using RevenantCore.Entities.Spec;
using RevenantCore.Scenes;

namespace RevenantCore.Tests.Entities;

[TestFixture]
public class ControlTracker_Test
{
    private class FakeInputs : IInputs
    {
        public bool Pressed { get; set; } = false;

        public KeyboardState Keyboard => new(Pressed ? [Keys.A] : []);
        public MouseState Mouse => new(0, 0, 0, Pressed ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        public GamePadState GamePad(PlayerIndex player) => new(new(), new(), new(player == PlayerIndex.Two && Pressed ? Buttons.A : Buttons.None), new());
    }

    private class FakeImpl : IImpl
    {
        public ControlRegistryBuilder RegisterControls(ControlRegistryBuilder registry) => registry.Register(new()
        {
            ID = "test"
        });

        public CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry) => registry;
    }

    [TestCase(false, false, ControlPositions.Up, 10, TestName = "CalcStates_Keyboard (Up, not pressed)")]
    [TestCase(false, true, ControlPositions.Press, 0, TestName = "CalcStates_Keyboard (Up, pressed)")]
    [TestCase(true, false, ControlPositions.Release, 0, TestName = "CalcStates_Keyboard (Down, not pressed)")]
    [TestCase(true, true, ControlPositions.Down, 10, TestName = "CalcStates_Keyboard (Down, pressed)")]
    public void CalcStates(bool prevPressed, bool pressed, ControlPositions expState, double expMillis)
    {
        ControlTracker tracker = new();
        FakeInputs inputs = new();
        Scene scene = new(new(new FakeCore(inputs, [new FakeImpl()]), new([])), new(), "default");
        scene.Universe.Bindings.Add("test", new()
        {
            Keys = [Keys.A],
            MouseButtons = [MouseButtons.Left],
            Buttons = [new()
            {
                Button = Buttons.A,
                Player = PlayerIndex.Two
            }]
        });
        inputs.Pressed = prevPressed;
        tracker.Create(scene, new(new()));
        inputs.Pressed = pressed;
        tracker.Tick(scene, new(new(new(), new(0, 0, 0, 0, 10))));
        ControlState state = tracker.States["test"];
        Assert.AreEqual(expState, state.Position);
        Assert.AreEqual(expMillis, state.Millis);
    }
}