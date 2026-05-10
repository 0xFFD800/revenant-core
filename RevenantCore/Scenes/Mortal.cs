using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents an object with a finite lifespan, which needs to be reaped once its life is complete.
/// </summary>
public interface IMortal
{
    /// <summary>
    /// Called when this mortal is first added to the scene.
    /// </summary>
    /// <param name="scene">The scene to which this mortal is being added.</param>
    /// <param name="millis">The total number of milliseconds for which this game has been running.</param>
    void Create(Scene scene, double millis);

    /// <summary>
    /// Must be called after this object or its parent dies; does any final processing that needs to be done.
    /// </summary>
    /// <param name="scene">The scene in which this mortal object exists.</param>
    /// <param name="millis">The total number of milliseconds for which this game has been running.</param>
    void Glean(Scene scene, double millis);

    /// <summary>
    /// Whether this object is dead.
    /// Dead objects need to be reaped and removed from the scene.
    /// </summary>
    bool IsDead { get; }
}

/// <summary>
/// Represents an object which can be drawn on screen.
/// </summary>
public interface IVisible : IMortal
{
    /// <summary>
    /// Called once per Draw loop to draw this object into the given viewport.
    /// <para>Preconditions:</para>
    /// <list type="bullet">
    /// Layer == <paramref name="view"/>.Layer
    /// </list>
    /// </summary>
    /// <param name="view">The graphics object for this run of the Draw loop.</param>
    void Draw(View view);

    /// <summary>
    /// The graphics layer in which this object will be drawn.
    /// <para>
    /// Layer is not allowed to differ between parents an children,
    /// nor is it allowed to vary during an object's lifespan.
    /// </para>
    /// </summary>
    DrawLayer Layer { get; }

    /// <summary>
    /// The Z-position of this object, defining its draw order.
    /// Since higher Z values are further from the viewport, lower Z values will be drawn last.
    /// If <see cref="Layer"/> == <see cref="DrawLayer.Scene"/>, this should match the
    /// Z-position of this object within its Scene.
    /// </summary>
    float Z { get; }
}

/// <summary>
/// Represents an object which needs to be updated each run of the Update loop.
/// </summary>
public interface ITickable : IMortal
{
    /// <summary>
    /// Updates this object for one run of the Update loop.
    /// </summary>
    /// <param name="scene">The scene in which this object exists.</param>
    /// <param name="millis">
    /// The total number of milliseconds for which this game has been running.
    /// Equal to <see cref="GameTime.TotalGameTime.TotalMilliseconds"/>.
    /// </param>
    void Tick(Scene scene, double millis);
}

/// <summary>
/// Represents an object which exists at a given position within the scene.
/// </summary>
public interface IMoveable : IMortal
{
    /// <summary>
    /// The position of this object within its scene, relative to the origin.
    /// The origin (0, 0, 0) is the bottom left corner of the accessible area of the scene, at floor level.
    /// Positive X values are towards the right, positive Y values are up, and positive Z values are further from the camera.
    /// If this object is an <see cref="ICollideable"/>, this position should be equal to the center of the base of its
    /// <see cref="ICollideable.CollisionBox"/>.
    /// <para>Invariants:</para>
    /// <list type="bullet">
    /// if (this is <see cref="IVisible"/> visible) <see cref="Position.Z"/> == visible.Z
    /// </list>
    /// </summary>
    Vector3 Position { get; set; }
}

/// <summary>
/// Represents an object with a collision box in a scene.
/// </summary>
public interface ICollideable : IMoveable
{
    /// <summary>
    /// The 3D collision box of this object. Used to implement physical constraints on object movement.
    /// <para>Invariants:</para>
    /// <list type="bullet"> 
    /// <see cref="Position"/> == center of base of <see cref="CollisionBox"/> 
    /// </list>
    /// </summary>
    BoundingBox CollisionBox { get; }
}

/// <summary>
/// An object which is responsible for tracking the state of a list of mortals.
/// Subclasses are responsible for maintaining any type-specific lists by overriding the
/// Reap method.
/// </summary>
public abstract class Scythe : ITickable
{
    /// <summary>
    /// The list of all mortals tracked by this object.
    /// Must contain any mortals which exist in other object lists, 
    /// but not any mortals which are tracked by any other scythes..
    /// </summary>
    private readonly IList<IMortal> mortals = [];
    
    public abstract bool IsDead { get; }

    public abstract void Create(Scene scene, double millis);

    public virtual void Tick(Scene scene, double millis)
    {
        foreach (IMortal mortal in mortals.Where(m => m.IsDead).ToArray())
            Reap(mortal, scene, millis);
    }

    public virtual void Glean(Scene scene, double millis)
    {
        IMortal[] subobjects = [..mortals];
        foreach (IMortal mortal in subobjects)
            Reap(mortal, scene, millis);
    }

    /// <summary>
    /// Adds a mortal subobject to be tracked.
    /// A mortal should not be added to more than one scythe.
    /// </summary>
    /// <param name="mortal">The mortal to be tracked by this object.</param>
    /// <param name="scene">The scene in which this mortal exists.</param>
    /// <param name="millis">The total number of milliseconds for which this game has been running.</param>
    public virtual void Add(IMortal mortal, Scene scene, double millis)
    {
        mortals.Add(mortal);
        mortal.Create(scene, millis);
    }

    /// <summary>
    /// Reaps a mortal object. 
    /// Must call the object's Glean method and remove it from any lists this Scythe maintains.
    /// </summary>
    /// <param name="mortal">The mortal being reaped.</param>
    /// <param name="scene">The scene in which the reaping is being performed.</param>
    /// <param name="millis">The total number of milliseconds for which the game has been running.</param>
    protected virtual void Reap(IMortal mortal, Scene scene, double millis)
    {
        mortals.Remove(mortal);
        mortal.Glean(scene, millis);
    }
}