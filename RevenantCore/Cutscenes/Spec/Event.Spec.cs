using System.Collections.Generic;

namespace RevenantCore.Cutscenes.Spec;

/// <summary>
/// A YAML-serializable representation of an Event.
/// </summary>
public class EventSpec
{
    /// <summary>
    /// An ID which uniquely identifies this event.
    /// </summary>
    public string ID { get; set; } = "";

    /// <summary>
    /// A display name for this event.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// A string which describes the gameplay event this object represents.
    /// </summary>
    public string Descr { get; set; } = "";

    /// <summary>
    /// The conditions which must be fulfilled for this event to be completed.
    /// </summary>
    public EventFilterSpec? Preconditions { get; set; }
}

/// <summary>
/// Represents options for how a failed precondition should be treated.
/// </summary>
public enum PreconditionType
{
    /// <summary>
    /// Crashes the game if the precondition fails.
    /// </summary>
    Error,
    /// <summary>
    /// Don't complete this event if the precondition fails.
    /// </summary>
    Ignore,
    /// <summary>
    /// Force: Complete this event. If the precondition fails:
    /// <list type="number">
    /// <item>First, remove all elements of HasNone.</item>
    /// <item>Second, add all elements of HasAll.</item>
    /// <item>Finally, if hasAny is not fulfilled, add its first element.</item>
    /// </list>
    /// </summary>
    Force
}

/// <summary>
/// A filter which evaluates what events have and have not been completed.
/// <list type="bullet">
/// <listheader>Invariants:</listheader>
/// <item>HasAny must not overlap with HasNone</item>
/// <item>HasAll must not overlap with HasNone</item>
/// <item>No field may have duplicates IDs</item>
/// </list>
/// </summary>
public class EventFilterSpec
{
    /// <summary>
    /// A list of event IDs, at least one of which must be completed for the filter to pass.
    /// If this set is empty, all values will pass the condition.
    /// </summary>
    public HashSet<string> HasAny { get; set; } = [];

    /// <summary>
    /// A list of event IDs, all of which must be completed for the filter to pass.
    /// </summary>
    public HashSet<string> HasAll { get; set; } = [];

    /// <summary>
    /// A list of event IDs, none of which must be completed for the filter to pass.
    /// </summary>
    public HashSet<string> HasNone { get; set; } = [];
}