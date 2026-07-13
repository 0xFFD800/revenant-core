using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Graphics;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

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
    /// <param name="time">The time record of the current frame, derived from <see cref="GameTime"/>.</param>
    void Create(Scene scene, FrameTime time);

    /// <summary>
    /// Must be called after this object or its parent dies; does any final processing that needs to be done.
    /// </summary>
    /// <param name="scene">The scene in which this mortal object exists.</param>
    /// <param name="time">The time record of the current frame, derived from <see cref="GameTime"/>.</param>
    void Glean(Scene scene, FrameTime time);

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
    /// <param name="camera">The camera object defining how this object is being viewed.</param>
    void Draw(View view, Camera camera);

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
    /// <param name="time">The time record of the current frame, derived from <see cref="GameTime"/>.</param>
    void Tick(Scene scene, FrameTime time);
}

/// <summary>
/// Represents an object which exists at a given position within the scene.
/// </summary>
public interface IMoveable : IMortal
{
    /// <summary>
    /// A unique string identifier which can be used to identify this moveable object within the scene.
    /// Used to reference this object from cutscenes.
    /// </summary>
    string ID { get; }

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

    /// <summary>
    /// The material of this object, including information about its friction and mass.
    /// </summary>
    MaterialSpec Material { get; }

    /// <summary>
    /// The average acceleration of this object over a single run of the Tick loop.
    /// The scene handles the application of gravity.
    /// This is cleared out after each run of the Tick loop (as it is an average value).
    /// </summary>
    Vector3 Acceleration { get; set; }

    /// <summary>
    /// The velocity of this object in px/ms. 
    /// Since there are 32 pixels per meter, 1 px/ms = 31.25 m/s.
    /// </summary>
    Vector3 Velocity { get; set; }
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
    /// but not any mortals which are tracked by any other scythes.
    /// </summary>
    private readonly IList<IMortal> mortals = [];

    public abstract bool IsDead { get; }

    public abstract void Create(Scene scene, FrameTime time);

    public virtual void Tick(Scene scene, FrameTime time)
    {
        foreach (IMortal mortal in mortals.Where(m => m.IsDead).ToArray())
            Reap(mortal, scene, time);
    }

    public virtual void Glean(Scene scene, FrameTime time)
    {
        IMortal[] subobjects = [.. mortals];
        foreach (IMortal mortal in subobjects)
            Reap(mortal, scene, time);
    }

    /// <summary>
    /// Adds a mortal subobject to be tracked.
    /// A mortal should not be added to more than one scythe.
    /// </summary>
    /// <param name="mortal">The mortal to be tracked by this object.</param>
    /// <param name="scene">The scene in which this mortal exists.</param>
    /// <param name="time">The time record of the current frame, derived from <see cref="GameTime"/>.</param>
    public virtual void Add(IMortal mortal, Scene scene, FrameTime time)
    {
        mortals.Add(mortal);
        mortal.Create(scene, time);
    }

    /// <summary>
    /// Reaps a mortal object. 
    /// Must call the object's Glean method and remove it from any lists this Scythe maintains.
    /// </summary>
    /// <param name="mortal">The mortal being reaped.</param>
    /// <param name="scene">The scene in which the reaping is being performed.</param>
    /// <param name="time">The time record of the current frame, derived from <see cref="GameTime"/>.</param>
    protected virtual void Reap(IMortal mortal, Scene scene, FrameTime time)
    {
        mortals.Remove(mortal);
        mortal.Glean(scene, time);
    }
}