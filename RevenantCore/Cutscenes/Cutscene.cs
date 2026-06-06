using System.Collections.Generic;
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
    private int index = 0;
    private Cutscene? ActiveChild => index < children.Length ? children[index] : null;

    public override float Z => ActiveChild?.Z ?? 0;

    public override void Create(Scene scene, FrameTime time)
    {
        index = 0;
        ActiveChild?.Create(scene, time);
        complete = ActiveChild != null;
    }

    public override void Draw(View view)
    {
        ActiveChild?.Draw(view);
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        while (ActiveChild?.IsDead ?? false)
        {
            ActiveChild.Glean(scene, time);
            index++;
            if (!(ActiveChild?.IsDead ?? true))
                ActiveChild.Create(scene, time);
        }

        ActiveChild?.Tick(scene, time);
        complete = ActiveChild != null;
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
    private List<Cutscene> activeChildren = [];
    public override float Z => activeChildren.Count == 0 ? 0 : activeChildren.Max(c => c.Z);

    public override void Create(Scene scene, FrameTime time)
    {
        activeChildren = [..children.Where(c => !c.IsDead)];
        complete = activeChildren.Count > 0;
        foreach (Cutscene child in activeChildren)
            child.Create(scene, time);
    }

    public override void Draw(View view)
    {
        foreach (Cutscene child in activeChildren)
            child.Draw(view);    
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        foreach (Cutscene child in activeChildren.Where(c => c.IsDead))
        {
            activeChildren.Remove(child);
            child.Glean(scene, time);
        }

        foreach (Cutscene child in activeChildren)
            child.Tick(scene, time);

        complete = activeChildren.Count == 0;
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
        if (!IsDead)
            Trip(scene, time);
        complete = true;
    }

    public override void Draw(View view)
    {
        // nothing to do
    }

    public override void Tick(Scene scene, FrameTime time)
    {
        // nothing to do
    }

    /// <summary>
    /// Performs the work of this cutscene. 
    /// Called from the parent scene's Tick loop.
    /// </summary>
    /// <param name="scene">The scene which triggered this cutscene.</param>
    /// <param name="time">The time of the frame in which this method was called.</param>
    protected abstract void Trip(Scene scene, FrameTime time);
}