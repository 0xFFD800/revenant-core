using System.Collections.Generic;
using System.Diagnostics;
using RevenantCore.Graphics;
using RevenantCore.Util;

namespace RevenantCore.Scene;

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

    public override void Tick(Scene scene, double millis)
    {
        Debug.Assert(scene == this);
        visibles.Sort(Comparer<IVisible>.Create((x, y) => (int)(x.Z - y.Z)));
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