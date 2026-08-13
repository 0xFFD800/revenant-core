using Microsoft.Xna.Framework;
using System.Collections.Generic;
using RevenantCore.Scenes;
using RevenantCore.Util;
using System.Linq;
using System;
using RevenantCore.Entities.Spec;
using RevenantCore.Entities;
using Microsoft.Xna.Framework.Graphics;

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
    /// Adds an animation hook to this component to alter the display of its drawables.
    /// </summary>
    /// <param name="hook">The hook with which to animate this component.</param>
    void Animate(IAnimationHook hook, Scene scene, FrameTime time);
}

/// <summary>
/// The base class for containers, which are components which manage other components.
/// </summary>
/// <param name="components"> The list of components which are part of this container.</param>
/// <param name="area">The area which bounds this container. Should contain all of its components' areas.</param>
/// <param name="controls">The controls which will be used to change focus via the keyboard or gamepad.</param>
public class Container(List<IComponent> components, Rectangle area, DirectionControlSpec controls) : Scythe, IComponent
{
    private IComponent? prevMouseFocused = null, prevFocused = components.FindLast(c => c.HasFocus);

    public Container(List<IComponent> components)
        : this(components, components.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area)), new()) { }

    public Rectangle Area => area;
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; } = false;
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
        targets.Sort((c1, c2) => Math.Sign(distance(prevFocused.Area, c2.Area) - distance(prevFocused.Area, c1.Area)));
        // Select closest target, if there are any
        IComponent? target = targets.FirstOrDefault();
        if (target != null)
            SetFocused(target);
        return target != null;
    }


    private bool TryUpdateFocus(Scene scene)
    {
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

    public override void Create(Scene scene, FrameTime time)
    {
        foreach (IComponent c in components)
            Add(c, scene, time);
    }

    public void Draw(View view, Camera camera)
    {
        view.Screen.Push(Matrix.CreateTranslation(new(Area.X, Area.Y, 0)));
        foreach (IComponent c in components)
            c.Draw(view, camera);
        view.Screen.Pop();
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
}

/// <summary>
/// A component which has its area and appearance defined by one or more drawables.
/// </summary>
/// <param name="toDraw">A list of drawables to be drawn as part of this component, in order.</param>
/// <param name="z">The z-value at which the provided drawables should be drawn.</param>
public class Label(Drawable[] toDraw, float z) : Scythe, IComponent
{
    private readonly List<IAnimationHook> hooks = [];

    public Rectangle Area => toDraw.Aggregate(new Rectangle(toDraw.FirstOrDefault()?.Pos.ToPoint() ?? new(), new()),
        (r, d) => Rectangle.Union(r, new(d.Pos.ToPoint(), d.Size.ToPoint())));
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; } = false;
    public override bool IsDead => false;
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

    public override void Create(Scene scene, FrameTime time) { }

    public virtual void Draw(View view, Camera camera)
    {
        foreach (Drawable drawable in ToDraw)
        {
            Drawable copy = drawable.ShallowCopy();
            foreach (IAnimationHook hook in hooks)
                hook.Apply(copy);
            view.Screen.Draw(drawable);
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
/// <param name="onClick">The action taken when this control is interacted with.</param>
/// <param name="z">The Z-value of this control, identifying where in the draw order it should be drawn.</param>
public class Button(ButtonDrawables toDraw, string click, Action onClick, float z) : Label(toDraw.Unfocused, z), IComponent
{
    private bool isClicked = false;

    protected override Drawable[] ToDraw => !Enabled ? toDraw.Disabled : isClicked ? toDraw.Clicked : HasFocus ? toDraw.Focused : toDraw.Unfocused;

    public override void Tick(Scene scene, FrameTime time)
    {
        base.Tick(scene, time);
        ControlPositions position = scene.GetControlState(this, click).Position;
        isClicked = position is ControlPositions.Press or ControlPositions.Down;
        if (Enabled && HasFocus && position == ControlPositions.Release)
            onClick();
    }
}

/// <summary>
/// A space for the user to input some text.
/// </summary>
/// <param name="font">The font in which to draw the text.</param>
/// <param name="pos">The position on screen at which the text should be drawn.</param>
/// <param name="textHint">An optional hint to display when the user has not entered any text.</param>
/// <param name="textColor">The color in which to draw the text.</param>
/// <param name="z">The z-value at which this component should be drawn.</param>
public class TextInput(IFont font, Point pos, string textHint, Color textColor, float z) : Label([font.CreateDrawable("")], z), IComponent
{
    public string Buffer { get; private set; } = "";
    private string[] Lines => Buffer.Split('\n');
    private Point cursor = Point.Zero;
    private int BufferIndex => Lines[..cursor.Y].Sum(s => s.Length) + cursor.X;
    private bool drawCursor = false;

    protected override Drawable[] ToDraw
    {
        get
        {
            List<Drawable> toDraw = [];
            if (Buffer.Length > 0)
                toDraw.Add(MakeDrawable(Buffer));
            else if (textHint.Length > 0)
                toDraw.Add(MakeDrawable(textHint));

            if (drawCursor)
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

        foreach (string key in scene.GetPressedKeys(this))
        {
            string line = Lines[cursor.Y];
            if (key == KeyboardTracker.Directions.Left && cursor.X > 0)
                cursor.X--;
            else if (key == KeyboardTracker.Directions.Up && cursor.Y > 0)
            {
                cursor.Y--;
                FixCursor();
            }
            else if (key == KeyboardTracker.Directions.Right && cursor.X < line.Length - 1)
                cursor.X++;
            else if (key == KeyboardTracker.Directions.Down && cursor.Y < Lines.Length - 1)
            {
                cursor.Y++;
                FixCursor();
            }
            else if (key == KeyboardTracker.End)
                cursor.X = line.Length - 1;
            else if (key == KeyboardTracker.Home)
                cursor.X = 0;
            else if (key == KeyboardTracker.Back)
                Buffer = Buffer.Remove(Math.Max(0, BufferIndex - 1), 1);
            else if (key == KeyboardTracker.Delete)
                Buffer = Buffer.Remove(Math.Min(BufferIndex, Buffer.Length - 2), 1);
            else
                Buffer += key;
        }

        drawCursor = time.Millis / 1000 % 2 == 0;
    }

    private void FixCursor()
    {
        int length = Lines[cursor.Y].Length;
        cursor.X = Math.Min(cursor.X, length);
    }
}