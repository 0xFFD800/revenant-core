using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RevenantCore.Cutscenes.Spec;

namespace RevenantCore.Cutscenes;

public class EventFilter(EventFilterSpec spec)
{
    public bool Evaluate(EventCollection events) => !spec.HasNone.Any(events.IsComplete)
        && spec.HasAny.Any(events.IsComplete)
        && spec.HasAll.All(events.IsComplete);
}

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

    public void Complete(string evt)
    {
        if (events.TryGetValue(evt, out Event? e) && e.CanComplete(this))
            completed.Add(evt);
    }

    public bool IsComplete(string evt) => completed.Contains(evt);

    public void Undo(string evt)
    {
        completed.Remove(evt);
    }

    private class Event(EventSpec spec)
    {
        private readonly Precondition precondition = spec.PreconditionType switch
        {
            PreconditionType.Error => new ErrorPrecondition(spec),
            PreconditionType.Ignore => new IgnorePrecondition(spec),
            PreconditionType.Force => new ForcePrecondition(spec),
            _ => throw new ArgumentException(string.Format("Unrecognized precondition type {0}", Enum.GetName(spec.PreconditionType)))
        };

        public bool CanComplete(EventCollection events) => precondition.Evaluate(events);
    }
}

public abstract class Precondition(EventSpec spec)
{
    protected readonly EventSpec spec = spec;
    private readonly EventFilter? filter = spec.Preconditions != null ? new(spec.Preconditions) : null;

    public bool Evaluate(EventCollection events) => filter?.Evaluate(events) ?? true || Bypass(events);

    protected abstract bool Bypass(EventCollection events);
}

public class ErrorPrecondition(EventSpec spec) : Precondition(spec)
{
    protected override bool Bypass(EventCollection events) =>
        throw new InvalidOperationException(string.Format("Cannot complete \"{0}\" as preconditions have not been met.", spec.Name));
}

public class IgnorePrecondition(EventSpec spec) : Precondition(spec)
{
    protected override bool Bypass(EventCollection events) => false;
}

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