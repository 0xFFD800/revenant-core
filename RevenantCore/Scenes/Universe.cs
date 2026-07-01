using System.Collections.Generic;
using RevenantCore.Cutscenes;
using RevenantCore.Entities.Spec;

namespace RevenantCore.Scenes;

/// <summary>
/// Represents a single game file. Keeps track of all game events and settings across scenes.
/// </summary>
/// <param name="core">The core implementation object.</param>
/// <param name="events">The event collection corresponding to this universe.</param>
public class Universe(Core core, EventCollection events)
{
    /// <summary>
    /// The core implementation object.
    /// </summary>
    public Core Core => core;

    /// <summary>
    /// All events and their completion statuses in this universe.
    /// </summary>
    public EventCollection Events => events;

    /// <summary>
    /// The control bindings currently in use.
    /// </summary>
    public Dictionary<string, ControlBindSpec> Bindings { get; } = [];
}