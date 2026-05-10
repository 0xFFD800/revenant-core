using Microsoft.Xna.Framework;
using RevenantCore.Graphics;

namespace RevenantCore.Scene;

/// <summary>
/// Represents an object with a finite lifespan, which needs to be reaped once its life is complete.
/// </summary>
public interface IMortal
{
    /// <summary>
    /// Must be called after this object dies; does any final processing that needs to be done.
    /// <para>Preconditions:</para>
    /// <list type="bullet"> 
    /// IsDead == true
    /// </list>
    /// </summary>
    /// <param name="scene">The scene in which this mortal object exists.</param>
    void Reap(Scene scene);

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
    /// Layer is not allowed to differ between parents an children.
    /// If Layer changes, the change must be reflected by any parent or owner of this object.
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