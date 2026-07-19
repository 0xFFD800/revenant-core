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
public interface IComponent : IVisible, ITickable
{
    /// <summary>
    /// Indicates whether this component is focused or not; i.e., whether it should directly respond to controls.
    /// </summary>
    bool HasFocus { set; }

    /// <summary>
    /// Indicates the area of the viewport that this component covers.
    /// </summary>
    Rectangle Area { get; }

    /// <summary>
    /// Indicates whether the component can receive focus and/or input.
    /// </summary>
    bool Enabled { get; set; }
}

/// <summary>
/// The base class for containers, which are components which manage other components.
/// </summary>
/// <param name="components"> The list of components which are part of this container.</param>
/// <param name="area">The area which bounds this container. Should contain all of its components' areas.</param>
/// <param name="controls">The controls which will be used to change focus via the keyboard or gamepad.</param>
public class Container(List<IComponent> components, Rectangle area, DirectionControlSpec controls) : Scythe, IComponent
{
    private IComponent? prevMouseFocused, prevFocused = null;

    public Container(List<IComponent> components)
        : this(components, components.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area)), new()) { }

    public Rectangle Area => area;
    public bool Enabled { get; set; } = true;
    public bool HasFocus { private get; set; } = false;
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
        if (prevFocused == null || scene.GetControlState(control).Position != ControlPositions.Press)
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


    private void UpdateFocus(Scene scene)
    {
        if (TryFocusKeyboardSel(scene, controls.Left, (prev, target) => prev.X > target.X + target.Width, (prev, target) => prev.X - (target.X + target.Width))
         || TryFocusKeyboardSel(scene, controls.Right, (prev, target) => prev.X + prev.Width < target.X, (prev, target) => target.X - (prev.X + prev.Width))
         || TryFocusKeyboardSel(scene, controls.Up, (prev, target) => prev.Y > target.Y + target.Height, (prev, target) => prev.Y - (target.Y + target.Height))
         || TryFocusKeyboardSel(scene, controls.Down, (prev, target) => prev.Y + prev.Height < target.Y, (prev, target) => target.Y - (prev.Y + prev.Height)))
            return;

        // Start searching from the end, because we want the highest Z value.
        IComponent? currMouseFocused = components.LastOrDefault(
            c => c.Enabled && c.Area.Contains(scene.Universe.Core.Inputs.Mouse.Position));
        if (currMouseFocused != prevMouseFocused && currMouseFocused != null)
            SetFocused(currMouseFocused);
        prevMouseFocused = currMouseFocused;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        foreach (IComponent c in components)
            Add(c, scene, time);
    }

    public void Draw(View view, Camera camera)
    {
        view.Screen.Push(Matrix.CreateTranslation(new(Area.X, 0, Area.Y)));
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
        UpdateFocus(scene);
        base.Tick(scene, time);
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
/// <param name="area">The area which bounds this control.</param>
/// <param name="toDraw">A record containing the drawable components of this button.</param>
/// <param name="click">The control which triggers this button's action.</param>
/// <param name="onClick">The action taken when this control is interacted with.</param>
/// <param name="z">The Z-value of this control, identifying where in the draw order it should be drawn.</param>
public class Button(Rectangle area, ButtonDrawables toDraw, string click, Action onClick, float z) : IComponent
{
    private bool isClicked = false;

    public Button(ButtonDrawables toDraw, string click, Action onClick, float z) : this(
        toDraw.Unfocused.Aggregate(new Rectangle(), (r, d) => Rectangle.Union(r, new(d.Pos.ToPoint(), d.Size.ToPoint()))),
        toDraw, click, onClick, z)
    { }

    public Rectangle Area => area;
    public bool Enabled { get; set; } = true;
    public bool HasFocus { private get; set; } = false;
    public bool IsDead => false;
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => z;

    public virtual void Create(Scene scene, FrameTime time) { }

    public virtual void Draw(View view, Camera camera)
    {
        Drawable[] drawables = !Enabled ? toDraw.Disabled : isClicked ? toDraw.Clicked : HasFocus ? toDraw.Focused : toDraw.Unfocused;
        foreach (Drawable drawable in drawables)
            view.Screen.Draw(drawable);
    }

    public virtual void Glean(Scene scene, FrameTime time) { }

    public virtual void Tick(Scene scene, FrameTime time)
    {
        ControlPositions position = scene.GetControlState(click).Position;
        isClicked = position is ControlPositions.Press or ControlPositions.Down;
        if (Enabled && HasFocus && position == ControlPositions.Release)
            onClick();
    }
}