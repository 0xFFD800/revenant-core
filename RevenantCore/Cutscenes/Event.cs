using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Cutscenes;

/// <summary>
/// A filter which evaluates to true or false based on what events have and have not been completed.
/// </summary>
/// <param name="spec">The YAML-deserializable spec for this filter.</param>
public class EventFilter(EventFilterSpec spec)
{
    /// <summary>
    /// Evaluates the filter.
    /// </summary>
    /// <param name="events">The event collection to evaluate this filter in the context of.</param>
    /// <returns>Whether or not this filter's criteria are fulfilled.</returns>
    public bool Evaluate(EventCollection events) => !spec.HasNone.Any(events.IsComplete)
        && spec.HasAny.Any(events.IsComplete)
        && spec.HasAll.All(events.IsComplete);
}

/// <summary>
/// The universe of all possible events and which of them are complete.
/// </summary>
/// <param name="allEvents">A YAML-deserializable spec representing all events.</param>
public class EventCollection(EventSpec[] allEvents)
{
    /// <summary>
    /// All events in this gameplay universe, mapped by ID.
    /// </summary>
    private readonly ImmutableDictionary<string, Event> events = allEvents
        .Select(e => new KeyValuePair<string, Event>(e.ID, new(e)))
        .ToImmutableDictionary();

    /// <summary>
    /// A set of all completed event IDs.
    /// </summary>
    private readonly HashSet<string> completed = [];

    /// <summary>
    /// Completes the event, if it exists and its preconditions allow it.
    /// </summary>
    /// <param name="evt">The ID of the event to attempt to complete.</param>
    public void Complete(string evt)
    {
        if (events.TryGetValue(evt, out Event? e) && e.CanComplete(this))
            completed.Add(evt);
    }

    /// <summary>
    /// Checks whether the event mapped to the given ID has been completed.
    /// </summary>
    /// <param name="evt">The ID of the event to check for completion.</param>
    /// <returns>Whether <paramref name="evt"/> has been completed or not.</returns>
    public bool IsComplete(string evt) => completed.Contains(evt);

    /// <summary>
    /// Removes the event mapped to the given ID from the set of completed events. 
    /// If the event has not been completed, this method does nothing.
    /// </summary>
    /// <param name="evt"></param>
    public void Undo(string evt)
    {
        completed.Remove(evt);
    }

    /// <summary>
    /// Represents an event which can be completed in gameplay.
    /// </summary>
    /// <param name="spec">The YAML-deserializable spec representing this event.</param>
    private class Event(EventSpec spec)
    {
        private readonly Precondition precondition = spec.PreconditionType switch
        {
            PreconditionType.Error => new ErrorPrecondition(spec),
            PreconditionType.Ignore => new IgnorePrecondition(spec),
            PreconditionType.Force => new ForcePrecondition(spec),
            _ => throw new ArgumentException(string.Format("Unrecognized precondition type {0}", Enum.GetName(spec.PreconditionType)))
        };

        /// <summary>
        /// Whether this event can be completed, based on its precondition.
        /// </summary>
        /// <param name="events">The event context to check this precondition against.</param>
        /// <returns>Whether this event's precondition allows it to be created.</returns>
        public bool CanComplete(EventCollection events) => precondition.Evaluate(events);
    }
}

/// <summary>
/// A wrapper around EventFilter specifying the steps which should be taken 
/// if an event's precondition filter does not allow for it to be created.
/// </summary>
/// <param name="spec">The spec of the event which this precondition checks.</param>
public abstract class Precondition(EventSpec spec)
{
    /// <summary>
    /// The spec of the event which this precondition checks.
    /// </summary>
    protected readonly EventSpec spec = spec;
    private readonly EventFilter? filter = spec.Preconditions != null ? new(spec.Preconditions) : null;

    /// <summary>
    /// Evaluates the precondition.
    /// </summary>
    /// <param name="events">The event context in which to evaluate the precondition.</param>
    /// <returns>Whether the precondition evaluated successfully.</returns>
    public bool Evaluate(EventCollection events)
    {
        if (filter?.Evaluate(events) ?? true)
            return true;
        else
            return Bypass(events);
    } 

    /// <summary>
    /// Evaluates whether or not the event can still be completed if the filter does not pass.
    /// </summary>
    /// <param name="events">The event context to evaluate the bypass logic in.</param>
    /// <returns>Whether or not the event may still be completed.</returns>
    protected abstract bool Bypass(EventCollection events);
}

/// <summary>
/// Crashes the game if the precondition fails.
/// </summary>
public class ErrorPrecondition(EventSpec spec) : Precondition(spec)
{
    protected override bool Bypass(EventCollection events) =>
        throw new InvalidOperationException(string.Format("Cannot complete \"{0}\" as preconditions have not been met.", spec.Name));
}

/// <summary>
/// Doesn't complete this event if the precondition fails.
/// </summary>
public class IgnorePrecondition(EventSpec spec) : Precondition(spec)
{
    protected override bool Bypass(EventCollection events) => false;
}

/// <summary>
/// Forces the precondition to succeed by fulfilling its criteria automatically.
/// </summary>
public class ForcePrecondition(EventSpec spec) : Precondition(spec)
{
    protected override bool Bypass(EventCollection events)
    {
        foreach (string evt in spec.Preconditions?.HasNone ?? [])
            events.Undo(evt);
        foreach (string evt in spec.Preconditions?.HasAll ?? [])
            events.Complete(evt);
        if (!(spec.Preconditions?.HasAny?.Any(events.IsComplete) ?? true))
            events.Complete(spec.Preconditions.HasAny.First());

        return true;
    }
}