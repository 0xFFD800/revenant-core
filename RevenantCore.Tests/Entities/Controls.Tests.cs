using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NUnit.Framework.Internal.Execution;
using RevenantCore.Entities;
using RevenantCore.Entities.Spec;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Tests.Entities;

[TestFixture]
public class ControlTracker_Test
{
    public class FakeInputs : IInputs
    {
        public bool Pressed { get; set; } = false;
        public Point MousePos { get; set; } = new();

        public KeyboardState Keyboard => new(Pressed ? [Keys.A] : []);
        public MouseState Mouse => new(MousePos.X, MousePos.Y, 0, Pressed ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        public GamePadState GamePad(PlayerIndex player) => new(new(), new(), new(player == PlayerIndex.Two && Pressed ? Buttons.A : Buttons.None), new());
    }

    public class FakeImpl(string id) : IImpl
    {
        internal FakeImpl() : this("test") { }

        public SpecRegistryBuilder RegisterAgents(SpecRegistryBuilder registry) => registry;

        public ControlRegistryBuilder RegisterControls(ControlRegistryBuilder registry) => registry.Register(new()
        {
            ID = id
        });

        public SpecRegistryBuilder RegisterCutscenes(SpecRegistryBuilder registry) => registry;

        public SpecRegistryBuilder RegisterTrackers(SpecRegistryBuilder registry) => registry;
    }

    public enum InputType { Keyboard, GamePad, Mouse }

    [TestCase(InputType.Keyboard, false, false, ControlPositions.Up, 10, TestName = "CalcStates_Keyboard (Up, not pressed)")]
    [TestCase(InputType.Keyboard, false, true, ControlPositions.Press, 0, TestName = "CalcStates_Keyboard (Up, pressed)")]
    [TestCase(InputType.Keyboard, true, false, ControlPositions.Release, 0, TestName = "CalcStates_Keyboard (Down, not pressed)")]
    [TestCase(InputType.Keyboard, true, true, ControlPositions.Down, 10, TestName = "CalcStates_Keyboard (Down, pressed)")]
    [TestCase(InputType.GamePad, false, false, ControlPositions.Up, 10, TestName = "CalcStates_GamePad (Up, not pressed)")]
    [TestCase(InputType.GamePad, false, true, ControlPositions.Press, 0, TestName = "CalcStates_GamePad (Up, pressed)")]
    [TestCase(InputType.GamePad, true, false, ControlPositions.Release, 0, TestName = "CalcStates_GamePad (Down, not pressed)")]
    [TestCase(InputType.GamePad, true, true, ControlPositions.Down, 10, TestName = "CalcStates_GamePad (Down, pressed)")]
    [TestCase(InputType.Mouse, false, false, ControlPositions.Up, 10, TestName = "CalcStates_Mouse (Up, not pressed)")]
    [TestCase(InputType.Mouse, false, true, ControlPositions.Press, 0, TestName = "CalcStates_Mouse (Up, pressed)")]
    [TestCase(InputType.Mouse, true, false, ControlPositions.Release, 0, TestName = "CalcStates_Mouse (Down, not pressed)")]
    [TestCase(InputType.Mouse, true, true, ControlPositions.Down, 10, TestName = "CalcStates_Mouse (Down, pressed)")]
    public void CalcStates(InputType inputType, bool prevPressed, bool pressed, ControlPositions expState, double expMillis)
    {
        ControlTracker tracker = new();
        FakeInputs inputs = new();
        Scene scene = new(new(new FakeCore(inputs, [new FakeImpl()]), new([])), tracker, new KeyboardTracker(), new(), "default");
        ControlBindSpec binding = new();
        switch (inputType) 
        {
            case InputType.Keyboard: 
                binding.Keys = [Keys.A];
                break;
            case InputType.Mouse: 
                binding.MouseButtons = [MouseButtons.Left];
                break;
            case InputType.GamePad: 
                binding.Buttons = [new()
                {
                    Button = Buttons.A,
                    Player = PlayerIndex.Two
                }];
                break;
        }
        scene.Universe.Bindings.Add("test", binding);
        inputs.Pressed = prevPressed;
        tracker.Create(scene, new(new()));
        inputs.Pressed = pressed;
        tracker.Tick(scene, new(new(new(), new(0, 0, 0, 0, 10))));
        ControlState state = tracker.States["test"];
        Assert.AreEqual(expState, state.Position);
        Assert.AreEqual(expMillis, state.Millis);
    }
}

[TestFixture]
public class KeyboardTracker_Test
{
    private class FakeInputs : IInputs
    {
        internal string? Key { get; set; }
        internal bool CapsLock { get; set; } = false;
        internal bool Shift { get; set; } = false;
        private Keys? KeyBase => Key != null ? Enum.Parse<Keys>(Key) : null;
        public KeyboardState Keyboard => new(KeyBase.HasValue ? (Shift ? [KeyBase.Value, Keys.LeftShift] : [KeyBase.Value]) : [], CapsLock, false);
        public MouseState Mouse => new();
        public GamePadState GamePad(PlayerIndex player) => new();
    }

    [TestCase(false, "A", false, false, "a", ControlPositions.Press, 0, TestName = "LowercaseA")]
    public void CalcStates(bool prevKey, string key, bool capsLock, bool shift, string expText, ControlPositions expState, double expMillis)
    {
        KeyboardTracker tracker = new();
        FakeInputs inputs = new();
        Scene scene = new(new(new FakeCore(inputs, []), new([])), new ControlTracker(), tracker, new(), "default");
        if (prevKey)
            inputs.Key = key;
        tracker.Create(scene, new(new()));
        inputs.Key = key;
        inputs.CapsLock = capsLock;
        inputs.Shift = shift;
        tracker.Tick(scene, new(new(new(), new(0, 0, 0, 0, 10))));
        Dictionary<string, ControlState> states = tracker.States.Where(p => p.Value.Position != ControlPositions.Up).ToDictionary();
        Assert.AreEqual(1, states.Count);
        KeyValuePair<string, ControlState> state = states.First();
        Assert.AreEqual(expText, state.Key);
        Assert.AreEqual(expState, state.Value.Position);
        Assert.AreEqual(expMillis, state.Value.Millis);
    }
}

[TestFixture]
public class ControlRegistryBuilder_Test
{
    [Test]
    public void RegisterDuplicate_Error()
    {
        ControlRegistryBuilder registry = new();
        ControlSpec spec = new() { ID = "test" };
        registry.Register(spec);
        Assert.Throws<ArgumentException>(() => registry.Register(spec));
    }

    [Test]
    public void Build_ContainsRegisteredKeys()
    {
        ControlRegistryBuilder builder = new();
        ControlSpec spec = new() { ID = "test" };
        builder.Register(spec);
        ControlRegistry registry = builder.Build();
        Assert.IsTrue(registry.IDs.Contains("test"));
        Assert.IsFalse(registry.IDs.Contains("not-test"));
    }
}