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
}

/// <summary>
/// The base class for containers, which are components which manage other components.
/// </summary>
public class Container(List<IComponent> components, Rectangle area, DirectionControlSpec controls) : Scythe, IComponent
{
    private IComponent? prevMouseFocused, prevFocused = null;

    public Container(List<IComponent> components)
        : this(components, components.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area)), new()) { }

    public Rectangle Area => area;
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
        IComponent[] targets = [.. components.Where(c => canSwitchTo(prevFocused.Area, c.Area))];
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
            c => c.Area.Contains(scene.Universe.Core.Inputs.Mouse.Position));
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