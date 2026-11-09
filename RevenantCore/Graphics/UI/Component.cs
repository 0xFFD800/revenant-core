using Microsoft.Xna.Framework;
using System.Collections.Generic;
using RevenantCore.Scenes;
using RevenantCore.Util;
using System.Linq;
using System;
using RevenantCore.Entities.Spec;
using RevenantCore.Entities;

namespace RevenantCore.Graphics.UI;

/// <summary>
/// The base interface for a UI component, which may be a container for other UI components, 
/// define its own behavior, or both.
/// </summary>
public interface IComponent : IVisible, ITickable, IControllable
{
    /// <summary>
    /// Indicates whether this component is focused or not; i.e., whether it should directly respond to controls.
    /// </summary>
    bool HasFocus { get; set; }

    /// <summary>
    /// Indicates the area of the viewport that this component covers.
    /// </summary>
    Rectangle Area { get; }

    /// <summary>
    /// Indicates whether the component can receive focus and/or input.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Indicates the component is currently active, and focus should not be shifted away from it.
    /// </summary>
    bool Active { get; }

    /// <summary>
    /// Adds an animation hook to this component to alter the display of its drawables.
    /// </summary>
    /// <param name="hook">The hook with which to animate this component.</param>
    void Animate(IAnimationHook hook, Scene scene, FrameTime time);

    /// <summary>
    /// Waits for any animations to complete, then sets IsDead to true.
    /// </summary>
    void Close();
}

/// <summary>
/// The base class for containers, which are components which manage other components.
/// </summary>
/// <param name="initialComponents"> The list of components which are initially part of this container.</param>
/// <param name="area">The area which bounds this container. Should contain all of its components' areas.</param>
/// <param name="controls">The controls which will be used to change focus via the keyboard or gamepad.</param>
public class Container(List<IComponent> initialComponents, Rectangle area, DirectionControlSpec controls) : Scythe, IComponent
{
    private readonly List<IComponent> components = [];
    private IComponent? prevMouseFocused = null, prevFocused = initialComponents.FindLast(c => c.HasFocus);

    public Container(List<IComponent> initialComponents)
        : this(initialComponents, initialComponents.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area)), new()) { }

    public Rectangle Area => area;
    public bool Enabled { get; set; } = true;
    public bool HasFocus
    {
        get => components.Any(c => c.HasFocus);
        set
        {
            if (value && prevFocused != null)
                prevFocused.HasFocus = true;
            else if (value)
                components.FirstOrDefault(c => c.Enabled)?.HasFocus = true;
            else
                components.ForEach(c => c.HasFocus = false);
        }
    }
    public bool Active => false;
    public override bool IsDead => components.Count == 0 || components.All(c => c.IsDead);
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => components.Max(c => c.Z);

    private void SetFocused(IComponent component)
    {
        component.HasFocus = true;
        prevFocused = component;
    }

    private bool TryFocusKeyboardSel(Scene scene, string control, Func<Rectangle, Rectangle, bool> canSwitchTo, Func<Rectangle, Rectangle, int> distance)
    {
        if (prevFocused == null || scene.GetControlState(this, control).Position != ControlPositions.Press)
            return false;

        // Select targets based on CanSwitchTo
        IComponent[] targets = [.. components.Where(c => c.Enabled && canSwitchTo(prevFocused.Area, c.Area))];
        // Sort targets by ascending distance
        targets.Sort((c1, c2) => Math.Sign(distance(prevFocused.Area, c1.Area) - distance(prevFocused.Area, c2.Area)));
        // Select closest target, if there are any
        IComponent? target = targets.FirstOrDefault();
        if (target != null)
            SetFocused(target);
        return target != null;
    }

    private bool TryUpdateFocus(Scene scene)
    {
        if (prevFocused?.Active ?? false) return false;
        if (TryFocusKeyboardSel(scene, controls.Left, (prev, target) => prev.X > target.X + target.Width, (prev, target) => prev.X - (target.X + target.Width))
         || TryFocusKeyboardSel(scene, controls.Right, (prev, target) => prev.X + prev.Width < target.X, (prev, target) => target.X - (prev.X + prev.Width))
         || TryFocusKeyboardSel(scene, controls.Up, (prev, target) => prev.Y > target.Y + target.Height, (prev, target) => prev.Y - (target.Y + target.Height))
         || TryFocusKeyboardSel(scene, controls.Down, (prev, target) => prev.Y + prev.Height < target.Y, (prev, target) => target.Y - (prev.Y + prev.Height)))
            return true;

        // Start searching from the end, because we want the highest Z value.
        IComponent? currMouseFocused = components.LastOrDefault(
            c => c.Enabled && c.Area.Contains(scene.Universe.Core.Inputs.Mouse.Position));
        bool ret = currMouseFocused != prevMouseFocused && currMouseFocused != null;
        if (ret && currMouseFocused != null)
            SetFocused(currMouseFocused);

        prevMouseFocused = currMouseFocused;
        return ret;
    }

    public override void Add(IMortal mortal, Scene scene, FrameTime time)
    {
        base.Add(mortal, scene, time);
        if (mortal is IComponent component)
        {
            components.Add(component);
            if (prevFocused == null && component.HasFocus)
                prevFocused = component;
        }
    }

    public override void Create(Scene scene, FrameTime time)
    {
        foreach (IComponent c in initialComponents)
            Add(c, scene, time);
    }

    public void Draw(View view, Camera camera)
    {
        view.Screen.Push(Matrix.CreateTranslation(new(Area.X, Area.Y, 0)));
        foreach (IComponent c in components)
            c.Draw(view, camera);
        view.Screen.Pop();
    }

    protected override void Reap(IMortal mortal, Scene scene, FrameTime time)
    {
        base.Reap(mortal, scene, time);
        if (mortal is IComponent component)
            components.Remove(component);
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        components.Sort((x, y) => Math.Sign(y.Z - x.Z));
        foreach (IComponent c in components)
        {
            c.Tick(scene, time);
            c.HasFocus = false;
        }
        if (!TryUpdateFocus(scene) && prevFocused != null)
            SetFocused(prevFocused);
        base.Tick(scene, time);
    }

    public bool Matches(IControllable other) => other == this || components.Any(c => c.Matches(other));

    public void Animate(IAnimationHook hook, Scene scene, FrameTime time)
    {
        foreach (IComponent component in components)
            component.Animate(hook, scene, time);
    }

    public void Close()
    {
        foreach (IComponent component in components)
            component.Close();
    }
}

/// <summary>
/// A component which has its area and appearance defined by one or more drawables.
/// </summary>
/// <param name="toDraw">A list of drawables to be drawn as part of this component, in order.</param>
/// <param name="z">The z-value at which the provided drawables should be drawn.</param>
public class Label(Drawable[] toDraw, float z) : Scythe, IComponent
{
    private readonly List<IAnimationHook> hooks = [];
    private bool closing = false;

    public Rectangle Area => toDraw.Aggregate(new Rectangle(toDraw.FirstOrDefault()?.Pos.ToPoint() ?? new(), new()),
        (r, d) => Rectangle.Union(r, new(d.Pos.ToPoint(), d.Size.ToPoint())));
    public bool Enabled { get; set; } = false;
    public bool HasFocus { get; set; } = false;
    public virtual bool Active => false;
    public override bool IsDead => closing && hooks.Count == 0;
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => z;

    protected virtual Drawable[] ToDraw => toDraw;

    public override void Add(IMortal mortal, Scene scene, FrameTime time)
    {
        base.Add(mortal, scene, time);
        if (mortal is IAnimationHook hook)
            hooks.Add(hook);
    }

    public void Animate(IAnimationHook hook, Scene scene, FrameTime time)
    {
        Add(hook, scene, time);
    }

    public void Close()
    {
        closing = true;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        closing = false;
    }

    public virtual void Draw(View view, Camera camera)
    {
        foreach (Drawable drawable in ToDraw)
        {
            Drawable copy = drawable.ShallowCopy();
            foreach (IAnimationHook hook in hooks)
                hook.Apply(copy, view.Time);
            view.Screen.Draw(copy);
        }
    }

    public bool Matches(IControllable other) => other == this;

    protected override void Reap(IMortal mortal, Scene scene, FrameTime time)
    {
        base.Reap(mortal, scene, time);
        if (mortal is IAnimationHook hook)
            hooks.Remove(hook);
    }
}

public class AnimatedLabel(AnimationCollection animations, string id, Vector2 center, float z) : Label([animations.GetFrame(id, new(new()))], z)
{
    private FrameTime frame = new(new());
    protected override Drawable[] ToDraw => [animations.GetFrame(id, frame).ShallowCopy().SetCenter(center)];

    public override void Create(Scene scene, FrameTime time)
    {
        base.Create(scene, time);
        frame = new(new());
    }

    public override void Draw(View view, Camera camera)
    {
        // The animation collection will base the frame off the total game time, but we want
        // the amount of time since this component was created.
        frame = new(new(frame.GameTime.TotalGameTime + view.Time.GameTime.ElapsedGameTime, new()));
        base.Draw(view, camera);
    }
}

/// <summary>
/// A record containing all the components which may be drawn with this button.
/// Each list should be organized in the desired draw order.
/// </summary>
/// <param name="Unfocused">The drawable components of the visible component when it is not focused or disabled.</param>
/// <param name="Disabled">The components to draw when this component is disabled.</param>
/// <param name="Focused">The components to draw when this component is focused.</param> 
/// <param name="Clicked">The components to draw when this component is clicked.</param>
public record struct ButtonDrawables(Drawable[] Unfocused, Drawable[] Disabled, Drawable[] Focused, Drawable[] Clicked);

/// <summary>
/// Base class for a component which performs an action when interacted with.
/// </summary>
/// <param name="toDraw">A record containing the drawable components of this button.</param>
/// <param name="click">The control which triggers this button's action.</param>
/// <param name="z">The Z-value of this control, identifying where in the draw order it should be drawn.</param>
public abstract class BaseButton(ButtonDrawables toDraw, string click, float z) : Label(toDraw.Unfocused, z), IComponent
{
    private bool isClicked = false;

    protected virtual ButtonDrawables Drawables => toDraw;
    protected override Drawable[] ToDraw => !Enabled ? Drawables.Disabled : isClicked ? Drawables.Clicked : HasFocus ? Drawables.Focused : Drawables.Unfocused;

    public override void Tick(Scene scene, FrameTime time)
    {
        base.Tick(scene, time);
        ControlPositions position = scene.GetControlState(this, click).Position;
        isClicked = Enabled && HasFocus && position is ControlPositions.Press or ControlPositions.Down;
        if (Enabled && HasFocus && position == ControlPositions.Release)
            OnClick(scene, time);
    }

    protected abstract void OnClick(Scene scene, FrameTime time);
}

/// <summary>
/// Convenience class for buttons which take an action which may be passed as a parameter
/// </summary>
/// <param name="onClick">The action taken when this control is interacted with.</param>
public class Button(ButtonDrawables toDraw, string click, Action<Scene, FrameTime> onClick, float z) : BaseButton(toDraw, click, z), IComponent
{
    protected override void OnClick(Scene scene, FrameTime time)
    {
        onClick(scene, time);
    }
}

/// <summary>
/// A space for the user to input some text.
/// </summary>
/// <param name="background">The background on which the text being input is drawn.</param>
/// <param name="font">The font in which to draw the text.</param>
/// <param name="pos">The position on screen at which the text should be drawn.</param>
/// <param name="maxChars">The maximum number of characters which may be entered into this input.</param>
/// <param name="interact">The control which, when this input is focused, marks it as active.</param>
/// <param name="cancel">The control which, when this input is active, marks it as inactive.</param>
/// <param name="textHint">An optional hint to display when the user has not entered any text.</param>
/// <param name="textColor">The color in which to draw the text.</param>
/// <param name="z">The z-value at which this component should be drawn.</param>
public class TextInput(ButtonDrawables background, IFont font, Point pos, int maxChars, string interact, string cancel, string textHint, Color textColor, float z) : BaseButton(background, interact, z), IComponent
{
    public string Buffer { get; private set; } = "";
    public override bool Active => active;
    private string[] Lines => Buffer.Split('\n');
    private Point cursor = Point.Zero;
    private int BufferIndex => Lines[..cursor.Y].Sum(s => s.Length) + cursor.X;
    private bool active = false, drawCursor = false;

    protected override Drawable[] ToDraw
    {
        get
        {
            List<Drawable> toDraw = [.. !Enabled ? Drawables.Disabled : active ? Drawables.Clicked : HasFocus ? Drawables.Focused : Drawables.Unfocused];
            if (Buffer.Length > 0)
                toDraw.Add(MakeDrawable(Buffer));
            else if (textHint.Length > 0)
                toDraw.Add(MakeDrawable(textHint));

            if (drawCursor && active)
                toDraw.Add(MakeDrawable("|").SetPos(pos.ToVector2() + new Vector2(
                    font.MeasureText(Lines[cursor.Y][..cursor.X]).X,
                    font.MeasureText(string.Join("\n", Lines[..cursor.Y])).Y)));
            return [.. toDraw];
        }
    }

    private Drawable MakeDrawable(string text) => font.CreateDrawable(text)
        .SetPos(pos.ToVector2())
        .SetMask(textColor);

    public override void Tick(Scene scene, FrameTime time)
    {
        base.Tick(scene, time);

        if (active &= HasFocus && scene.GetControlState(this, cancel).Position != ControlPositions.Press)
            foreach (string key in scene.GetPressedKeys(this))
            {
                string line = Lines[cursor.Y];
                if (key == KeyboardTracker.Directions.Left)
                {
                    if (cursor.X > 0)
                        cursor.X--;
                }
                else if (key == KeyboardTracker.Directions.Up)
                {
                    if (cursor.Y > 0)
                        cursor.Y--;
                }
                else if (key == KeyboardTracker.Directions.Right)
                {
                    if (cursor.X < line.Length - 1)
                        cursor.X++;
                }
                else if (key == KeyboardTracker.Directions.Down)
                {
                    if (cursor.Y < Lines.Length - 1)
                        cursor.Y++;
                }
                else if (key == KeyboardTracker.End)
                    cursor.X = line.Length;
                else if (key == KeyboardTracker.Home)
                    cursor.X = 0;
                else if (key == KeyboardTracker.Back)
                {
                    if (BufferIndex > 0)
                    {
                        Buffer = Buffer.Remove(Math.Max(0, BufferIndex - 1), 1);
                        cursor.X--;
                    }
                }
                else if (key == KeyboardTracker.Delete)
                {
                    if (BufferIndex < Buffer.Length && Buffer.Length > 0)
                        Buffer = Buffer.Remove(Math.Min(BufferIndex, Buffer.Length - 1), 1);
                }
                else if (Buffer.Length + key.Length <= maxChars)
                {
                    Buffer = Buffer.Insert(BufferIndex, key);
                    cursor.X += key.Length;
                }

                FixCursor();
            }

        drawCursor = (int)time.Millis / 1000 % 2 == 0;
    }

    private void FixCursor()
    {
        if (cursor.X < 0 && cursor.Y > 0)
        {
            cursor.Y--;
            cursor.X = Lines[cursor.Y].Length;
        }
        else
            cursor.X = Math.Min(cursor.X, Lines[cursor.Y].Length);
    }

    protected override void OnClick(Scene scene, FrameTime time)
    {
        active = true;
    }
}

/// <summary>
/// A group of buttons of which at most one may be selected at a time.
/// </summary>
/// <param name="deselected">The drawables to show when the button is not selected.</param>
/// <param name="selected">The drawables to show when the button is selected.</param>
/// <param name="options">The drawables which constitute options to the radio button list.</param>
/// <param name="pos">The position of the top left of this radio button group.</param>
/// <param name="click">The "click" control for this group.</param>
/// <param name="z">The z-value at which the whole group should be displayed.</param>
public class RadioButtonGroup(ButtonDrawables deselected, Drawable[] selected, Drawable[] options, Point pos, string click, float z) 
    : Container([], new(pos, new(options.Max(o => o.Area.Width), (int)(options.Max(o => o.Area.Height) * 1.5F))), new())
{
    private readonly ButtonDrawables deselected = deselected;
    private readonly Drawable[] selected = selected;
    private readonly string click = click;
    private readonly float z = z;
    private RadioButton[]? buttons;

    /// <summary>
    /// The index of the selected button, if any.
    /// </summary>
    public int? SelIndex => buttons?.Index().First(b => b.Item.sel).Index;

    public override void Create(Scene scene, FrameTime time)
    {
        base.Create(scene, time);
        int height = selected.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area)).Height;
        buttons = [.. options.Select((o, i) => new RadioButton(this, o, new(pos + new Point(0, (int)(height * 1.5F * i)), Area.Size)) { Enabled = Enabled })];
        foreach (RadioButton b in buttons)
            Add(b, scene, time);
    }

    private void DeselectAll()
    {
        foreach (RadioButton b in buttons ?? [])
            b.sel = false;
    }

    private class RadioButton(RadioButtonGroup g, Drawable o, Rectangle a)
        : BaseButton(new(Process(g.deselected.Unfocused, o, a), Process(g.deselected.Disabled, o, a), Process(g.deselected.Focused, o, a), Process(g.deselected.Clicked, o, a)), g.click, g.z)
    {
        private readonly Drawable[] selected = Process(g.selected, o, a);
        internal bool sel = false;

        protected override Drawable[] ToDraw => sel ? selected : base.ToDraw;

        protected override void OnClick(Scene scene, FrameTime time)
        {
            g.DeselectAll();
            sel = true;
        }

        private static Drawable[] Process(Drawable[] drawables, Drawable option, Rectangle area) =>
            [.. drawables.Append(option.SetOffset(new(12, 0)))
                .Select(d => d.ShallowCopy().SetOffset(d.Offset - area.Size.ToVector2()).SetPos(area.Location.ToVector2()))];
    }
}