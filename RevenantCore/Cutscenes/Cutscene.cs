using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevenantCore.Graphics;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Cutscenes;

/// <summary>
/// Represents a Cutscene, a sequence of repeatable instructions loaded from spec
/// which map to automated changes in the game world.
/// </summary>
public abstract class Cutscene : IVisible, ITickable
{    
    public DrawLayer Layer => DrawLayer.UI;
    public abstract float Z { get; }
    
    /// <summary>
    /// Whether this cutscene has completed all its cutscene logic.
    /// </summary>
    protected bool complete = false;
    // TODO: this will also need to take into account its filter status (i.e., IsDead => !filter.Evaluate() || complete).
    public bool IsDead => complete;

    public abstract void Create(Scene scene, FrameTime time);
    public abstract void Draw(View view);
    public abstract void Tick(Scene scene, FrameTime time);

    public virtual void Glean(Scene scene, FrameTime time)
    {
        complete = false;
    }
}

/// <summary>
/// A block cutscene which triggers its children one after another, in order.
/// </summary>
/// <param name="children">The list of children to trigger in order.</param>
public class SequentialBlock(Cutscene[] children) : Cutscene
{
    /// <summary>
    /// The index of the currently active child.
    /// </summary>
    private int index = 0;

    /// <summary>
    /// The currently active child, or null if all the children have been consumed.
    /// </summary>
    private Cutscene? ActiveChild => index < children.Length ? children[index] : null;

    public override float Z => ActiveChild?.Z ?? 0;

    /// <summary>
    /// Consumes children until an active one is found or the entire list has been consumed, then
    /// sets the <see cref="complete"/> flag accordingly.
    /// </summary>
    /// <param name="scene">The context scene of the calling code.</param>
    /// <param name="time">The time of the frame in which this method is being called.</param>
    private void Advance(Scene scene, FrameTime time)
    {
        while (ActiveChild?.IsDead ?? false)
        {
            ActiveChild.Glean(scene, time);
            index++;
            if (!(ActiveChild?.IsDead ?? true))
                ActiveChild.Create(scene, time);
        }

        complete = ActiveChild == null;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        index = 0;
        Advance(scene, time);
    }

    public override void Draw(View view)
    {
        ActiveChild?.Draw(view);
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        Advance(scene, time);
        ActiveChild?.Tick(scene, time);
    }
    
    public override void Glean(Scene scene, FrameTime time)
    {
        for (int i = index; i < children.Length; i++)
            children[i].Glean(scene, time);
        base.Glean(scene, time);
    }
}

/// <summary>
/// A block cutscene which triggers all its children concurrently.
/// </summary>
/// <param name="children">The list of children to be triggered concurrently as part of this cutscene.</param>
public class ConcurrentBlock(Cutscene[] children) : Cutscene
{
    private readonly List<Cutscene> activeChildren = [];
    public override float Z => activeChildren.Count == 0 ? 0 : activeChildren.Max(c => c.Z);

    private void UpdateState()
    {
        activeChildren.Sort((c1, c2) => c1.Z.CompareTo(c2.Z));
        complete = activeChildren.Count == 0;
    }

    public override void Create(Scene scene, FrameTime time)
    {
        foreach (Cutscene child in children)
            if (child.IsDead)
                child.Glean(scene, time);
            else
            {
                child.Create(scene, time);
                activeChildren.Add(child);
            }

        UpdateState();
    }

    public override void Draw(View view)
    {
        foreach (Cutscene child in activeChildren)
            child.Draw(view);    
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        foreach (Cutscene child in activeChildren.Where(c => c.IsDead).ToArray())
        {
            activeChildren.Remove(child);
            child.Glean(scene, time);
        }

        foreach (Cutscene child in activeChildren)
            child.Tick(scene, time);

        UpdateState();
    }

    public override void Glean(Scene scene, FrameTime time)
    {
        foreach (Cutscene child in activeChildren)
            child.Glean(scene, time);
        base.Glean(scene, time);
    }
}

/// <summary>
/// A base cutscene class which instantaneously triggers, then moves on to the next cutscene.
/// </summary>
public abstract class InstantCutscene : Cutscene
{
    public override float Z => 0;

    public override void Create(Scene scene, FrameTime time)
    {
        Trace.Assert(!IsDead, "Dead cutscenes should never be created!");
        Trip(scene, time);
        complete = true;
    }

    public override void Draw(View view)
    {
        throw new UnreachableException("Draw should never be called on an instant cutscene!");
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        throw new UnreachableException("Tick should never be called on an instant cutscene!");
    }

    /// <summary>
    /// Performs the work of this cutscene. 
    /// Called from the parent scene's Tick loop.
    /// </summary>
    /// <param name="scene">The scene which triggered this cutscene.</param>
    /// <param name="time">The time of the frame in which this method was called.</param>
    protected abstract void Trip(Scene scene, FrameTime time);
}