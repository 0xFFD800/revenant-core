using RevenantCore.Graphics;
using RevenantCore.Scenes.Spec;

namespace RevenantCore.Entities.Spec;

/// <summary>
/// A YAML-deserializable spec which defines data for an entity to be included in the scene.
/// </summary>
public class EntitySpec
{
    /// <summary>
    /// The unique ID by which this entity will be identified within its scene.
    /// </summary>
    public string ID { get; set; } = "entity";

    /// <summary>
    /// The agent which controls this entity's behavior.
    /// </summary>
    public AgentSpec Agent { get; set; } = new NullAgentSpec();

    /// <summary>
    /// The path to the animation collection which defines this entity's appearance.
    /// </summary>
    public string Animations { get; set; } = "default";

    /// <summary>
    /// The spec for the material which defines physics parameters for this entity.
    /// </summary>
    public MaterialSpec Material { get; set; } = new();

    /// <summary>
    /// The size of this entity's bounding box along each axis.
    /// </summary>
    public Vector3Spec Bounds { get; set; } = new();

    /// <summary>
    /// The layer in which this entity should be drawn.
    /// </summary>
    public DrawLayer Layer { get; set; } = DrawLayer.Scene;
}