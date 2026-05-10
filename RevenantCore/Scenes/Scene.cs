using System.Collections.Generic;
using System.Diagnostics;
using RevenantCore.Graphics;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents a gameplay area where entities exist and can interact.
/// </summary>
public class Scene : Scythe
{
    /// <summary>
    /// All visible objects in this scene, organized by <see cref="IVisible.Layer"/>.
    /// Each sublist should be sorted by <see cref="IVisible.Z"/>. 
    /// </summary>
    private readonly OrderedDict<DrawLayer, IVisible> visibles = new();

    public override bool IsDead => false;

    public override void Create(Scene scene, double millis)
    {
        Debug.Assert(scene == this);
    }

    public void Draw(View view)
    {
        // TODO: apply matrix to the SpriteBatch depending on the DrawLayer
        // view.Screen.Push(transform);
        foreach (IVisible visible in visibles.Get(view.Layer))
            visible.Draw(view);
        // view.Screen.Pop();
    }

    public override void Tick(Scene scene, double millis)
    {
        Debug.Assert(scene == this);
        visibles.Sort(Comparer<IVisible>.Create((x, y) => (int)(y.Z - x.Z)));
        base.Tick(scene, millis);
    }

    public override void Add(IMortal mortal, Scene scene, double millis)
    {
        Debug.Assert(scene == this);
        base.Add(mortal, scene, millis);
        if (mortal is IVisible visible)
            visibles.Add(visible.Layer, visible); 
    }

    protected override void Reap(IMortal mortal, Scene scene, double millis)
    {
        Debug.Assert(scene == this);
        base.Reap(mortal, scene, millis);
        if (mortal is IVisible visible)
            visibles.Remove(visible.Layer, visible);
    }
}