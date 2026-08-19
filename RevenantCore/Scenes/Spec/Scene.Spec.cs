using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Scenes.Spec;

/// <summary>
/// A YAML-serializable object which defines the physical parameters of the material an object is made out of.
/// </summary>
public class MaterialSpec
{
    /// <summary>
    /// The mass of this object in kg. A null mass means this object is fixed in position or travelling at a fixed rate.
    /// </summary>
    public float? Mass { get; set; } = null;

    /// <summary>
    /// The proportion of energy this object will absorb from a collision with it.
    /// A null MaterialAbsorption means it will absorb all energy from collisions; 1 means all energy will be reflected.
    /// </summary>
    public float? MaterialAbsorption { get; set; } = null;

    /// <summary>
    /// The proportion of energy this object will absorb from a colliding object traveling parallel to it.
    /// Must be between 0 and 1.
    /// 0 means no friction, 1 means it will instantaneously absorb all energy.
    /// Friction applied from multiple objects using the formula: 1 - ((1 - Friction_1) * (1 - Friction_2) * ...)
    /// Friction is applied to acceleration using the following formula: Acceleration = sign(Acceleration) * (Abs(Acceleration) - Abs(Velocity * Friction / time.ElapsedMillis))
    /// </summary>
    public float Friction { get; set; } = 0;

    /// <summary>
    /// The threshold of velocity at which this object will be stopped from moving, in px/ms.
    /// </summary>
    public float StaticFriction { get; set; } = 0.001F;
}

/// <summary>
/// A YAML-serializeable representation of a Vector3. 
/// </summary>
public class Vector3Spec
{
    /// <summary>
    /// The X-component of the vector.
    /// </summary>
    public float X { get; set; } = 0;

    /// <summary>
    /// The Y-component of the vector.
    /// </summary>
    public float Y { get; set; } = 0;

    /// <summary>
    /// The Z-component of the vector.
    /// </summary>
    public float Z { get; set; } = 0;

    /// <summary>
    /// The vector represented by this data object.
    /// </summary>
    public Vector3 Data => new(X, Y, Z);
}

/// <summary>
/// A YAML-serializeable representation of a Vector3. 
/// </summary>
public class Vector2Spec
{
    /// <summary>
    /// The X-component of the vector.
    /// </summary>
    public float X { get; set; } = 0;

    /// <summary>
    /// The Y-component of the vector.
    /// </summary>
    public float Y { get; set; } = 0;

    /// <summary>
    /// The vector represented by this data object.
    /// </summary>
    public Vector2 Data => new(X, Y);
}

/// <summary>
/// An enum representing the sides on which walls border the scene.
/// </summary>
public enum WallSide { Floor, Near, Far, Left, Right }

/// <summary>
/// An enum representing different ways a player could interact with a given interaction area.
/// </summary>
public enum InteractionType { Enter, Interact }

/// <summary>
/// A YAML-serializable object which defines an area which may trigger a cutscene based on player behavior.
/// </summary>
public class InteractionAreaSpec
{
    /// <summary>
    /// The type of behavior which will trigger this interaction.
    /// </summary>
    public InteractionType Type { get; set; } = InteractionType.Interact;

    /// <summary>
    /// The bottom left of the interaction object within this scene.
    /// </summary>
    public Vector3Spec BottomLeft { get; set; } = new();

    /// <summary>
    /// The size of the interaction object within this scene.
    /// </summary>
    public Vector3Spec Bounds { get; set; } = new();

    /// <summary>
    /// The cutscene to trigger when this interaction takes place.
    /// </summary>
    public CutsceneSpec Cutscene { get; set; } = new SequentialBlockSpec();
}

/// <summary>
/// A YAML-serializable object which defines base data about a scene.
/// </summary>
public class SceneSpec
{
    /// <summary>
    /// The bounds of the scene, measured in pixels.
    /// </summary>
    public Vector3Spec Bounds { get; set; } = new()
    {
        X = 320,
        Y = 160,
        Z = 80
    };

    /// <summary>
    /// The size of the viewport into the scene, measured in pixels.
    /// </summary>
    public Vector2Spec ViewportSize { get; set; } = new()
    {
        X = 320,
        Y = 160
    };

    /// <summary>
    /// The materials for the walls to this scene.
    /// </summary>
    public Dictionary<WallSide, MaterialSpec> Walls { get; set; } = new()
    {
        { WallSide.Floor, new() },
        { WallSide.Near, new() },
        { WallSide.Far, new() },
        { WallSide.Left, new() },
        { WallSide.Right, new() }
    };

    /// <summary>
    /// The cutscenes which can be triggered on entering this scene.
    /// </summary>
    public Dictionary<string, CutsceneSpec> Triggers { get; set; } = [];

    /// <summary>
    /// Interactions which can be triggered by player behavior in this scene.
    /// </summary>
    public InteractionAreaSpec[] Interactions { get; set; } = [];

    /// <summary>
    /// The gravitational acceleration in this scene, in px/ms^2.
    /// Since there are 32 pixels per meter, the default value is equal to 9.8 m/s^2 (gravitational acceleration on Earth).
    /// </summary>
    public float Gravity { get; set; } = 0.0003136F;
}
