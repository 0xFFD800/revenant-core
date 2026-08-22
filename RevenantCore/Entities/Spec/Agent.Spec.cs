using RevenantCore.Scenes.Spec;

namespace RevenantCore.Entities.Spec;

/// <summary>
/// Base class for YAML-deserializable dataspec of entity agents.
/// </summary>
public abstract class AgentSpec
{
    /// <summary>
    /// Creates the agent which this spec represents.
    /// </summary>
    /// <returns>The agent represented by this data spec.</returns>
    public abstract IAgent Create();
}

/// <summary>
/// The spec for an agent which does not move or interact with other entities.
/// </summary>
public class NullAgentSpec : AgentSpec
{
    public override IAgent Create() => new NullAgent();
}

/// <summary>
/// The spec for an agent which determines its movements from a tracker.
/// </summary>
public class TrackingAgentSpec : AgentSpec
{
    /// <summary>
    /// The spec for the tracker which will determine this agent's movements.
    /// </summary>
    public Vec3TrackerSpec TrackerSpec { get; set; } = new WanderTrackerSpec();

    /// <summary>
    /// The rate at which the agent should accelerate while moving.
    /// </summary>
    public float Acceleration { get; set; } = 0.0002F;

    public override IAgent Create() => new TrackingAgent(TrackerSpec.Create(), Acceleration, (float) TrackerSpec.Speed);
}

/// <summary>
/// The spec for an agent which determines its movements according to control inputs.
/// </summary>
public class InputAgentSpec : AgentSpec
{
    /// <summary>
    /// The spec for the controls which specify the walk direction of this agent.
    /// </summary>
    public DirectionControlSpec Controls { get; set; } = new();

    /// <summary>
    /// The control which causes this input agent to start interacting with interaction areas which require input.
    /// </summary>
    public string InteractControl { get; set; } = "interact";

    /// <summary>
    /// The rate at which the agent should accelerate while moving.
    /// </summary>
    public float Acceleration { get; set; } = 0.0002F;

    /// <summary>
    /// The top speed at which inputs should cause this agent to move.
    /// </summary>
    public float TopSpeed { get; set; } = 0.2F;

    public override IAgent Create() => new InputAgent(this);
}