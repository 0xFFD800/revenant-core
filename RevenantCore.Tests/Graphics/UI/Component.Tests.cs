using System.Collections.Frozen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Entities;
using RevenantCore.Graphics;
using RevenantCore.Graphics.UI;
using RevenantCore.Scenes;
using RevenantCore.Util;
using static RevenantCore.Tests.Entities.ControlTracker_Test;
using static RevenantCore.Tests.Scenes.Scene_Test;

namespace RevenantCore.Tests.Graphics.UI;

file class FakeScreen : IScreen
{
    public Drawable? lastDrawn = null;
    public int currDrawOrder = 0;
    public Matrix? matrix = null;

    public void Draw(Drawable drawable)
    {
        drawable.Draw(new MockSpriteBuffer(null, false));
        lastDrawn = drawable;
    }

    public void Pop()
    {
        matrix = null;
    }

    public void Push(Matrix transform)
    {
        matrix = transform;
    }
}

public class MockComponent(Rectangle area, bool initEnabled, bool initHasFocus, float z, bool isDead, bool expAnimate, bool expCreate, int? expDrawOrder, bool expMatched, bool expGlean, bool expTick, Matrix? expTranslation, bool expHasFocus) : IComponent
{
    private bool animated = false, created = false, gleaned = false, matched = false, ticked = false;
    private int? drawOrder = null;
    private Matrix? translation = null;

    public bool HasFocus { get; set; } = initHasFocus;
    public Rectangle Area => area;
    public bool Enabled { get; set; } = initEnabled;
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => z;

    public bool IsDead => isDead;

    public void Animate(IAnimationHook hook, Scene scene, FrameTime time)
    {
        animated = true;
    }

    public void Create(Scene scene, FrameTime time)
    {
        created = true;
    }

    public void Draw(View view, Camera camera)
    {
        FakeScreen screen = (FakeScreen)view.Screen;
        drawOrder = screen.currDrawOrder++;
        translation = screen.matrix;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        gleaned = true;
    }

    public bool Matches(IControllable other)
    {
        matched = true;
        return other == this;
    }

    public void Tick(Scene scene, FrameTime time)
    {
        ticked = true;
    }

    public void Validate()
    {
        Assert.AreEqual(expAnimate, animated);
        Assert.AreEqual(expCreate, created);
        Assert.AreEqual(expDrawOrder, drawOrder);
        Assert.AreEqual(expMatched, matched);
        Assert.AreEqual(expGlean, gleaned);
        Assert.AreEqual(expTick, ticked);
        Assert.AreEqual(expTranslation, translation);
        Assert.AreEqual(expHasFocus, HasFocus);
    }
}

public class MockAnimation(int expAppliedCt, bool expCreate, bool expGlean) : IAnimationHook
{
    private int appliedCt = 0;
    private bool created = false, gleaned = false;

    public bool IsDead { get; set; } = false;

    public void Apply(Drawable drawable, FrameTime time)
    {
        appliedCt++;
    }

    public void Create(Scene scene, FrameTime time)
    {
        created = true;
    }

    public void Glean(Scene scene, FrameTime time)
    {
        gleaned = true;
    }

    public void Validate()
    {
        Assert.AreEqual(expAppliedCt, appliedCt);
        Assert.AreEqual(expCreate, created);
        Assert.AreEqual(expGlean, gleaned);
    }
}

[TestFixture]
public class Container_Test
{
    private class FakeControllable(bool matches) : IControllable
    {
        public bool IsDead => throw new NotImplementedException();

        public void Create(Scene scene, FrameTime time)
        {
            throw new NotImplementedException();
        }

        public void Glean(Scene scene, FrameTime time)
        {
            throw new NotImplementedException();
        }

        public bool Matches(IControllable other) => matches;
    }

    [Test]
    public void EmptyDraw_SanityCheck()
    {
        Assert.DoesNotThrow(() => new Container([]).Draw(new(new FakeScreen(), new(new()), DrawLayer.UI), new(new(), new())));
    }

    [Test]
    public void DrawOrder()
    {
        Vector3 pos = new(4, 6, 0);
        Matrix matrix = Matrix.CreateTranslation(pos);
        MockComponent mock1 = new(new(0, 1, 4, 2), true, false, 1, false, false, true, 1, false, false, true, matrix, false);
        MockComponent mock2 = new(new(7, 0, 2, 4), true, false, 2, false, false, true, 0, false, false, true, matrix, false);
        Container container = new([mock1, mock2], new(4, 6, 4, 4), new());
        Scene scene = new FakeScene();
        container.Create(scene, new(new()));
        container.Tick(scene, new(new()));
        container.Draw(new(new FakeScreen(), new(new()), DrawLayer.UI), new(new(), new()));
        mock1.Validate();
        mock2.Validate();
    }

    [Test]
    public void EmptyTick_SanityCheck()
    {
        Assert.DoesNotThrow(() => new Container([]).Tick(new FakeScene(), new(new())));
    }

    [TestCase("left", false, 0, true, false, TestName = "Tick_NoPress_NoFocusChange")]
    [TestCase("left", true, 0, true, false, TestName = "Tick_PressNoneInDir_NoFocusChange")]
    [TestCase("right", true, 0, false, true, TestName = "Tick_PressInDir_FocusChange")]
    [TestCase("left", false, 3, false, true, TestName = "Tick_MouseOver_FocusChange")]
    public void Tick_FocusChange(string input, bool pressed, int mouseX, bool expLeftFocus, bool expRightFocus)
    {
        MockComponent mock1 = new(new(1, 1, 1, 1), true, true, 1, false, false, true, null, false, false, true, null, expLeftFocus);
        MockComponent mock2 = new(new(3, 1, 1, 1), true, false, 2, false, false, true, null, false, false, true, null, expRightFocus);
        Container container = new([mock1, mock2])
        {
            HasFocus = true
        };
        FakeInputs inputs = new()
        {
            Pressed = pressed,
            MousePos = new(mouseX, 1)
        };
        Core core = new FakeCore(inputs, [new FakeImpl(input)]);
        Universe universe = new(core, new([]));
        universe.Bindings.Add(input, new() { Keys = [Keys.A] });
        Scene scene = new FakeScene(universe, new(), "default");
        scene.Create(scene, new(new()));
        container.Create(scene, new(new()));
        container.Tick(scene, new(new()));
        mock1.Validate();
        mock2.Validate();
    }

    [Test]
    public void Matches_Fake_False()
    {
        Assert.IsFalse(new Container([]).Matches(new FakeControllable(false)));
    }

    [Test]
    public void Matches_Same_True()
    {
        Container c = new([]);
        Assert.IsTrue(c.Matches(c));
    }

    [Test]
    public void Matches_Subobj_True()
    {
        IComponent component = new MockComponent(new(), false, false, 0, false, false, false, null, false, false, false, null, false);
        Container c = new([component]);
        Assert.IsTrue(c.Matches(component));
    }

    [Test]
    public void EmptyAnimate_SanityCheck()
    {
        Assert.DoesNotThrow(() => new Container([]).Animate(new FadeAnimation(0, false), new FakeScene(), new(new())));
    }

    [Test]
    public void Animate_ApplyToSubobjs()
    {
        MockComponent mock1 = new(new(), true, false, 0, false, true, true, null, false, false, false, null, false);
        MockComponent mock2 = new(new(), true, false, 0, false, true, true, null, false, false, false, null, false);
        Container c = new([mock1, mock2]);
        Scene scene = new FakeScene();
        c.Create(scene, new(new()));
        c.Animate(new FadeAnimation(0, false), scene, new(new()));
        mock1.Validate();
        mock2.Validate();
    }

    [Test]
    public void Layer_UI()
    {
        Assert.AreEqual(DrawLayer.UI, new Container([]).Layer);
    }

    [Test]
    public void IsDead_Empty_True()
    {
        Assert.IsTrue(new Container([]).IsDead);
    }

    [TestCase(false, TestName = "IsDead_OneLiving_False")]
    [TestCase(true, TestName = "IsDead_AllDead_True")]
    public void IsDead_AllDead(bool secondDead)
    {
        Container c = new([new MockComponent(new(), false, false, 0, true, false, false, null, false, false, false, null, false),
            new MockComponent(new(), false, false, 0, secondDead, false, false, null, false, false, false, null, false)]);
        Assert.AreEqual(secondDead, c.IsDead);
    }

    [Test]
    public void Z_MaxZ()
    {
        Container c = new([new MockComponent(new(), false, false, 1, false, false, false, null, false, false, false, null, false),
            new MockComponent(new(), false, false, 2, false, false, false, null, false, false, false, null, false)]);
        Assert.AreEqual(2, c.Z);
    }
}

[TestFixture]
public class Label_Test
{
    [Test]
    public void Area_UnionOfDrawables()
    {
        Assert.AreEqual(new Rectangle(1, 1, 11, 15), new Label([
            new MockDrawable(new(1, 1)).SetPos(new(1, 1)),
            new MockDrawable(new(2, 4)).SetPos(new(10, 12))
        ], 0).Area);
    }

    [Test]
    public void ConstsAndFields()
    {
        Label l = new([], 0);
        Assert.IsTrue(l.Enabled);
        l.Enabled = false;
        Assert.IsFalse(l.Enabled);
        Assert.IsFalse(l.HasFocus);
        l.HasFocus = true;
        Assert.IsTrue(l.HasFocus);
        Assert.IsFalse(l.IsDead);
        Assert.AreEqual(DrawLayer.UI, l.Layer);
        Assert.AreEqual(0, l.Z);
    }

    [Test]
    public void AnimateCycle()
    {
        Label l = new([new MockDrawable(new(), Vector2.One, true)], 0);
        Scene scene = new FakeScene();
        MockAnimation hook = new(1, true, true);
        l.Create(scene, new(new()));
        l.Animate(hook, scene, new(new()));
        FrameTime time = new(new(new(0, 0, 0, 0, 10), new(0, 0, 0, 0, 10)));
        l.Tick(scene, time);
        l.Draw(new(new FakeScreen(), time, DrawLayer.UI), new(Vector2.One, Vector2.One));
        l.Glean(scene, time);
        hook.Validate();
    }

    [Test]
    public void Matches_IsSame()
    {
        Label l1 = new([], 0);
        Label l2 = new([], 0);
        Assert.IsFalse(l1.Matches(l2));
        Assert.IsTrue(l1.Matches(l1));
    }
}

[TestFixture]
public class Button_Test
{
    private static Button SetUp(ButtonDrawables d, bool enabled, bool focused, bool clicked, bool released, Action onClick)
    {
        Button b = new(d, "click", onClick, 0)
        {
            Enabled = enabled,
            HasFocus = focused
        };
        FakeImpl impl = new("click");
        FakeInputs inputs = new()
        {
            Pressed = clicked
        };
        FakeCore core = new(inputs, [impl]);
        Universe universe = new(core, new([]));
        universe.Bindings.Add("click", new() { Keys = [Keys.A] });
        FakeScene scene = new(universe, new(), "default");
        scene.Create(scene, new(new()));
        scene.Tick(scene, new(new()));
        b.Create(scene, new(new()));
        if (released)
        {
            inputs.Pressed = false;
            scene.Tick(scene, new(new()));
        }
        b.Tick(scene, new(new()));
        return b;
    }

    [TestCase(false, true, true, false, TestName = "Release_NotEnabled_NoClick")]
    [TestCase(true, false, true, false, TestName = "Release_NotFocused_NoClick")]
    [TestCase(true, true, false, false, TestName = "Release_NotReleased_NoClick")]
    [TestCase(true, true, true, true, TestName = "Release_EnabledFocused_OnClick")]
    public void OnClick_Test(bool enabled, bool focused, bool released, bool expClick)
    {
        bool clicked = false;
        SetUp(new(), enabled, focused, true, released, () => clicked = true);
        Assert.AreEqual(expClick, clicked);
    }

    [TestCase(false, true, true, true, true, false, false, false, TestName = "Draw_Disabled")]
    [TestCase(true, false, true, false, false, true, false, false, TestName = "Draw_Clicked")]
    [TestCase(true, false, true, true, false, false, false, true, TestName = "Draw_Released")]
    [TestCase(true, true, false, false, false, false, true, false, TestName = "Draw_Focused")]
    [TestCase(true, false, false, false, false, false, false, true, TestName = "Draw_Unfocused")]
    public void Draw_Test(bool enabled, bool focused, bool clicked, bool released, bool expDisabled, bool expClicked, bool expFocus, bool expUnfocus)
    {
        MockDrawable disable = new(new(), Vector2.One, expDisabled);
        MockDrawable click = new(new(), Vector2.One, expClicked);
        MockDrawable focus = new(new(), Vector2.One, expFocus);
        MockDrawable unfocus = new(new(), Vector2.One, expUnfocus);
        Button b = SetUp(new([unfocus], [disable], [focus], [click]), enabled, focused, clicked, released, () => { });
        b.Draw(new(new FakeScreen(), new(new()), DrawLayer.UI), new(Vector2.One, Vector2.One));
        disable.Validate();
        click.Validate();
        focus.Validate();
        unfocus.Validate();
    }
}

internal class FakeKeyboardTracker : IControlTracker
{
    internal ControlState? ControlState { get; set; } = null;
    internal string? StateName { get; set; } = null;

    public FrozenDictionary<string, ControlState> States => FrozenDictionary.Create<string, ControlState>(ControlState.HasValue && StateName != null
        ? [new(StateName, ControlState.Value)]
        : []);
    public bool IsDead => false;
    public void Create(Scene scene, FrameTime time) { }
    public void Glean(Scene scene, FrameTime time) { }
    public void Tick(Scene scene, FrameTime time) { }
}

internal class FakeFont(Vector2 textSize) : IFont
{
    internal readonly List<string> lastMeasured = [];

    public Drawable CreateDrawable(string text) => new MockDrawable(textSize, text);

    public Vector2 MeasureText(string text)
    {
        lastMeasured.Add(text);
        return textSize;
    }
}

[TestFixture]
public class TextInput_Test
{
    private static void TypeInto(FakeKeyboardTracker keyboard, Scene scene, TextInput input, string charToType, FrameTime time)
    {
        keyboard.ControlState = new(ControlPositions.Press, 0);
        keyboard.StateName = charToType;
        input.Tick(scene, time);
    }

    private static void TypeInto(FakeKeyboardTracker keyboard, Scene scene, TextInput input, string charToType)
    {
        TypeInto(keyboard, scene, input, charToType, new(new()));
    }

    [TestCase("", "right", 0, 0, null, TestName = "Empty_NoMove")]
    [TestCase("foo", "left", 0, 0, null, TestName = "LeftAt0,0_NoMove")]
    [TestCase("foo", "end,left", 2, 0, null, TestName = "LeftAt3,0_2,0")]
    [TestCase("foo", "right", 1, 0, null, TestName = "RightAt0,0_1,0")]
    [TestCase("foo", "up", 0, 0, null, TestName = "UpAt0,0_NoMove")]
    [TestCase("foo\nbar", "down", 0, 1, null, TestName = "DownAt0,0_0,1")]
    [TestCase("lorem ipsum dolor amet\nbar", "right,right,right,right,right,right,down", 3, 1, null, TestName = "Down_FixCursor")]
    [TestCase("bar\nlorem ipsum dolor amet", "down,right,right,right,right,right,right,up", 3, 0, null, TestName = "Up_FixCursor")]
    [TestCase("foo", "end", 3, 0, null, TestName = "EndAt0,0_EndOfLine")]
    [TestCase("foo", "end,home", 0, 0, null, TestName = "HomeAt3,0_StartOfLine")]
    [TestCase("baz", "back", 0, 0, "baz", TestName = "BackAt0,0_NoRemove")]
    [TestCase("baz", "right,right,back", 1, 0, "bz", TestName = "BackAt2,0_RemoveChar")]
    [TestCase("baz", "right,right,delete", 2, 0, "ba", TestName = "DeleteAt2,0_RemoveChar")]
    public void ArrowKeys_MoveCursor(string buffer, string directions, int expCursorX, int expCursorY, string? expBuffer)
    {
        FakeKeyboardTracker keyboard = new();
        FakeScene scene = new(new Universe(new FakeCore(), new([])), new ControlTracker(), keyboard, new(), "default");
        FakeFont font = new(new());
        TextInput input = new(font, new(1, 2), "", Color.White, 0);
        foreach (char c in buffer.ToCharArray())
            TypeInto(keyboard, scene, input, c.ToString());
        foreach (string s in directions.Split(','))
            TypeInto(keyboard, scene, input, s);
        input.Draw(new(new FakeScreen(), new(new()), DrawLayer.UI), new(new(), new()));
        Assert.AreEqual(2, font.lastMeasured.Count);
        Assert.AreEqual(expCursorX, font.lastMeasured[0].Length);
        Assert.AreEqual(expCursorY, font.lastMeasured[1].Count(c => c == '\n') + font.lastMeasured[1].Length > 0 ? 1 : 0);
        if (expBuffer != null)
            Assert.AreEqual(expBuffer, input.Buffer);
    }

    [TestCase("", TestName = "Tick_Chars_EmptySanityCheck")]
    [TestCase("abcdefghijklmnopqrstuvwxyz\nABCDEFGHIJKLMNOPQRSTUVWXYZ\n1234567890\n!@#$%^&*()\\|[]{};:'\",.<>/?`~-=_+", TestName = "Tick_Chars_TypeInto")]
    public void Tick_Chars_TypeIntoBuffer(string buffer)
    {
        FakeKeyboardTracker keyboard = new();
        FakeScene scene = new(new Universe(new FakeCore(), new([])), new ControlTracker(), keyboard, new(), "default");
        TextInput input = new(new FakeFont(new()), new(1, 2), "", Color.White, 0);
        foreach (char c in buffer.ToCharArray())
            TypeInto(keyboard, scene, input, c.ToString());
        Assert.AreEqual(buffer, input.Buffer);
    }

    [TestCase("buffer", "hint", "buffer", TestName = "FullBuffer_DrawBuffer")]
    [TestCase("", "hint", "hint", TestName = "EmptyBuffer_DrawHint")]
    [TestCase("", "", null, TestName = "EmptyBuffer_EmptyHint_DrawNone")]
    public void Draw(string buffer, string hint, string? expLastDrawnText)
    {
        FakeKeyboardTracker keyboard = new();
        FakeScene scene = new(new Universe(new FakeCore(), new([])), new ControlTracker(), keyboard, new(), "default");
        TextInput input = new(new FakeFont(new()), new(1, 2), hint, Color.White, 0);
        foreach (char c in buffer.ToCharArray())
            TypeInto(keyboard, scene, input, c.ToString(), new(new(new(0, 0, 0, 0, 10), new(0, 0, 0, 0, 10))));
        FakeScreen screen = new();
        input.Draw(new(screen, new(new()), DrawLayer.UI), new(new(), new()));
        Assert.AreEqual(expLastDrawnText, ((MockDrawable?)screen.lastDrawn)?.Text);
    }
}