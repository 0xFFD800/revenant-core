using Microsoft.Xna.Framework;
using System.Collections.Generic;
using RevenantCore.Scenes;
using RevenantCore.Util;
using System.Linq;
using System;

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
public class Container(List<IComponent> components, Rectangle area) : Scythe, IComponent
{
    public Container(List<IComponent> components)   
        : this(components, components.Aggregate(new Rectangle(), (r, c) => Rectangle.Union(r, c.Area))) { }

    public Rectangle Area => area;
    public bool HasFocus { get; set; } = false;
    public override bool IsDead => components.Count == 0 || components.All(c => c.IsDead);
    public DrawLayer Layer => DrawLayer.UI;
    public float Z => components.Max(c => c.Z);

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
        // TODO: Handle focus...
        foreach (IComponent c in components)
            c.Tick(scene, time);
        base.Tick(scene, time);
    }
}