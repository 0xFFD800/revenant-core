using Microsoft.Xna.Framework;

namespace RevenantCore.Scenes.Spec;

/// <summary>
/// Defines the physical parameters of the material an object is made out of.
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
    /// The proportion of energy this object will absorb from an object moving parallel along its bounds.
    /// A null Friction means it will absorb all energy from such an object; 1 means it will absorb none.
    /// </summary>
    public float? Friction { get; set; } = 1;
}

/// <summary>
/// A JSON serializeable representation of a Vector3. 
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
/// Defines base data about a scene.
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
    /// The material of the scene's floor (Y = 0).
    /// </summary>
    public MaterialSpec Floor { get; set; } = new();

    /// <summary>
    /// The material of the scene's near wall (Z = 0).
    /// </summary>
    public MaterialSpec NearWall { get; set; } = new();

    /// <summary>
    /// The material of the scene's far wall (Z = Bounds.Z).
    /// </summary>
    public MaterialSpec FarWall { get; set; } = new();

    /// <summary>
    /// The material of the scene's left wall (X = 0).
    /// </summary>
    public MaterialSpec LeftWall { get; set; } = new();

    /// <summary>
    /// The material of the scene's right wall (X = Bounds.X).
    /// </summary>
    public MaterialSpec RightWall { get; set; } = new();

    /// <summary>
    /// The gravitational acceleration in this scene, in px/ms^2.
    /// Since there are 32 pixels per meter, the default value is equal to 9.8 m/s^2 (gravitational acceleration on Earth).
    /// </summary>
    public float Gravity { get; set; } = 0.3136F;
}