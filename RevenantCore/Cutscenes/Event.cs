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
        private readonly IPrecondition preconditionType = spec.PreconditionType switch
        {
        };

        private readonly EventFilter? preconditions = new(spec.Preconditions);

        public bool CanComplete(EventCollection events) => preconditions?.Evaluate(events) ?? true || preconditionType.TryBypass(events);
    }
}

public interface IPrecondition
{
    bool TryBypass(EventCollection events);
}