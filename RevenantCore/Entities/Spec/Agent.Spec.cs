namespace RevenantCore.Entities.Spec;

/// <summary>
/// The YAML-deserializable spec for an agent which determines its movements according to control inputs.
/// </summary>
public class InputAgentSpec
{
    /// <summary>
    /// The ID of the control which causes this agent to move left.
    /// </summary>
    public string Left { get; set; } = "walkLeft";

    /// <summary>
    /// The ID of the control which causes this agent to move right.
    /// </summary>
    public string Right { get; set; } = "walkRight";

    /// <summary>
    /// The ID of the control which causes this agent to move up.
    /// </summary>
    public string Up { get; set; } = "walkUp";

    /// <summary>
    /// The ID of the control which causes this agent to move down.
    /// </summary>
    public string Down { get; set; } = "walkDown";

    /// <summary>
    /// The rate at which the agent should accelerate while moving.
    /// </summary>
    public float Acceleration { get; set; } = 0.0002F;

    /// <summary>
    /// The top speed at which inputs should cause this agent to move.
    /// </summary>
    public float TopSpeed { get; set; } = 0.2F;
}