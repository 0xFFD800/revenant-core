using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents a gameplay area where entities exist and can interact.
/// </summary>
/// <param name="bounds">TODO: Here only temporarily, until SceneSpecs are created.</param>
public class Scene(BoundingBox bounds) : Scythe
{
    /// <summary>
    /// The outer bounding box of this scene.
    /// </summary>
    private BoundingBox bounds = bounds;

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

    /// <summary>
    /// Project <paramref name="vector"/> from the 3D area of this scene into the 2D viewport.
    /// </summary>
    /// <param name="vector">The 3D vector representing a position in this scene.</param>
    /// <returns>The projection of <paramref name="vector"/> onto the 2D viewport.</returns>
    public Vector2 ProjectToViewport(Vector3 vector)
    {
        const float ratioYZ = 0.5F;
        const float ratioXZ = 0.25F;

        Vector2 pos2 = Vector2.Zero;
        float zFactor = vector.Z / bounds.Max.Z;
        float zRatio = ratioYZ * ratioXZ;

        // impact of vector.Z on pos2.Y
        float impactZY = vector.Z * ratioYZ;

        // impact of vector.Y on pos2.Y
        float impactYY = vector.Y * zFactor * (1 - zRatio);

        pos2.Y = impactZY + impactYY;

        // center X of level
        float lCX = bounds.Max.X / 2;
        // distance from center X of level
        float dlCX = Math.Abs(vector.X - lCX);
        // distance from center X of level after perspective is applied
        float dlCXp = dlCX * (1 - (zFactor * zRatio));
        pos2.X = lCX + (vector.X < lCX ? -dlCXp : dlCXp);

        return pos2;
    }
}