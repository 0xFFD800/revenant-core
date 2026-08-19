using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
        internal Keys? Key { get; set; }
        internal bool CapsLock { get; set; } = false;
        internal bool Shift { get; set; } = false;
        public KeyboardState Keyboard => new(Key.HasValue ? (Shift ? [Key.Value, Keys.LeftShift] : [Key.Value]) : [], CapsLock, false);
        public MouseState Mouse => new();
        public GamePadState GamePad(PlayerIndex player) => new();
    }

    [TestCase(false, Keys.A, false, false, "a", ControlPositions.Press, 0, TestName = "LowercaseA")]
    [TestCase(true, Keys.A, false, false, "a", ControlPositions.Down, 10, TestName = "HeldA")]
    [TestCase(false, Keys.A, true, false, "A", ControlPositions.Press, 0, TestName = "CapsLockA")]
    [TestCase(false, Keys.A, false, true, "A", ControlPositions.Press, 0, TestName = "ShiftA")]
    [TestCase(false, Keys.A, true, true, "a", ControlPositions.Press, 0, TestName = "ShiftAndCapsLockA")]
    [TestCase(false, Keys.D1, false, false, "1", ControlPositions.Press, 0, TestName = "One")]
    [TestCase(false, Keys.D1, true, false, "1", ControlPositions.Press, 0, TestName = "CapsLockOne")]
    [TestCase(false, Keys.D1, false, true, "!", ControlPositions.Press, 0, TestName = "ShiftOne")]
    [TestCase(false, Keys.D2, false, true, "@", ControlPositions.Press, 0, TestName = "ShiftTwo")]
    [TestCase(false, Keys.D3, false, true, "#", ControlPositions.Press, 0, TestName = "ShiftThree")]
    [TestCase(false, Keys.D4, false, true, "$", ControlPositions.Press, 0, TestName = "ShiftFour")]
    [TestCase(false, Keys.D5, false, true, "%", ControlPositions.Press, 0, TestName = "ShiftFive")]
    [TestCase(false, Keys.D6, false, true, "^", ControlPositions.Press, 0, TestName = "ShiftSix")]
    [TestCase(false, Keys.D7, false, true, "&", ControlPositions.Press, 0, TestName = "ShiftSeven")]
    [TestCase(false, Keys.D8, false, true, "*", ControlPositions.Press, 0, TestName = "ShiftEight")]
    [TestCase(false, Keys.D9, false, true, "(", ControlPositions.Press, 0, TestName = "ShiftNine")]
    [TestCase(false, Keys.D0, false, true, ")", ControlPositions.Press, 0, TestName = "ShiftZero")]
    [TestCase(false, Keys.Space, false, false, " ", ControlPositions.Press, 0, TestName = "Space")]
    [TestCase(false, Keys.Enter, false, false, "\n", ControlPositions.Press, 0, TestName = "Enter")]
    [TestCase(false, Keys.Tab, false, false, "\t", ControlPositions.Press, 0, TestName = "Tab")]
    [TestCase(false, Keys.Back, false, false, "back", ControlPositions.Press, 0, TestName = "Back")]
    [TestCase(false, Keys.Delete, false, false, "delete", ControlPositions.Press, 0, TestName = "Delete")]
    [TestCase(false, Keys.Home, false, false, "home", ControlPositions.Press, 0, TestName = "Home")]
    [TestCase(false, Keys.End, false, false, "end", ControlPositions.Press, 0, TestName = "End")]
    [TestCase(false, Keys.OemBackslash, false, false, "\\", ControlPositions.Press, 0, TestName = "Backslash")]
    [TestCase(false, Keys.OemBackslash, false, true, "|", ControlPositions.Press, 0, TestName = "BackslashShift")]
    [TestCase(false, Keys.OemCloseBrackets, false, false, "]", ControlPositions.Press, 0, TestName = "CloseBrackets")]
    [TestCase(false, Keys.OemCloseBrackets, false, true, "}", ControlPositions.Press, 0, TestName = "CloseBracketsShift")]
    [TestCase(false, Keys.OemComma, false, false, ",", ControlPositions.Press, 0, TestName = "Comma")]
    [TestCase(false, Keys.OemComma, false, true, "<", ControlPositions.Press, 0, TestName = "CommaShift")]
    [TestCase(false, Keys.OemMinus, false, false, "-", ControlPositions.Press, 0, TestName = "Minus")]
    [TestCase(false, Keys.OemMinus, false, true, "_", ControlPositions.Press, 0, TestName = "MinusShift")]
    [TestCase(false, Keys.OemOpenBrackets, false, false, "[", ControlPositions.Press, 0, TestName = "OpenBrackets")]
    [TestCase(false, Keys.OemOpenBrackets, false, true, "{", ControlPositions.Press, 0, TestName = "OpenBracketsShift")]
    [TestCase(false, Keys.OemPeriod, false, false, ".", ControlPositions.Press, 0, TestName = "OemPeriod")]
    [TestCase(false, Keys.OemPeriod, false, true, ">", ControlPositions.Press, 0, TestName = "OemPeriodShift")]
    [TestCase(false, Keys.OemPlus, false, false, "=", ControlPositions.Press, 0, TestName = "Plus")]
    [TestCase(false, Keys.OemPlus, false, true, "+", ControlPositions.Press, 0, TestName = "PlusShift")]
    [TestCase(false, Keys.OemQuestion, false, false, "/", ControlPositions.Press, 0, TestName = "Question")]
    [TestCase(false, Keys.OemQuestion, false, true, "?", ControlPositions.Press, 0, TestName = "QuestionShift")]
    [TestCase(false, Keys.OemQuotes, false, false, "'", ControlPositions.Press, 0, TestName = "Quotes")]
    [TestCase(false, Keys.OemQuotes, false, true, "\"", ControlPositions.Press, 0, TestName = "QuotesShift")]
    [TestCase(false, Keys.OemSemicolon, false, false, ";", ControlPositions.Press, 0, TestName = "Semicolon")]
    [TestCase(false, Keys.OemSemicolon, false, true, ":", ControlPositions.Press, 0, TestName = "SemicolonShift")]
    [TestCase(false, Keys.OemTilde, false, false, "`", ControlPositions.Press, 0, TestName = "Tilde")]
    [TestCase(false, Keys.OemTilde, false, true, "~", ControlPositions.Press, 0, TestName = "TildeShift")]
    public void CalcStates(bool prevKey, Keys key, bool capsLock, bool shift, string expText, ControlPositions expState, double expMillis)
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