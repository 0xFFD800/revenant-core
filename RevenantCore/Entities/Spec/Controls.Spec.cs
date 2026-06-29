using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace RevenantCore.Entities.Spec;

/// <summary>
/// Represents a binding for a control. 
/// May consist of keyboard bindings, mouse buttons, or gamepad buttons.
/// </summary>
public class ControlBindSpec
{
    /// <summary>
    /// The keyboard keys to which this control is bound.
    /// </summary>
    public Keys[] Key { get; set; } = [];
    
    /// <summary>
    /// The gamepad buttons to which this control is bound.
    /// </summary>
    public Buttons[] Button { get; set; } = [];

    /// <summary>
    /// The mouse buttons to which this control is bound.
    /// </summary>
    public MouseButtons[] MouseButton { get; set; } = [];
}

/// <summary>
/// Represents a control which may be bound to multiple keys or buttons, but which should have a consistent effect in game.
/// </summary>
public class ControlSpec
{
    /// <summary>
    /// The ID by which this control should be referenced in code.
    /// </summary>
    public string ID { get; set; } = "default";

    /// <summary>
    /// The name by which this control should be referred to in the UI.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// The description which should accompany this control in the UI.
    /// </summary>
    public string Descr { get; set; } = "";

    /// <summary>
    /// The default binding for this control.
    /// </summary>
    public ControlBindSpec Default { get; set; } = new();
}

/// <summary>
/// A finalized control registry. Contains all control mappings populated during the registry phase.
/// </summary>
/// <param name="registry">The finalized tag to type map.</param>
public class ControlRegistry(FrozenDictionary<string, ControlSpec> registry)
{
    /// <summary>
    /// All control IDs for which a mapping exists in the registry.
    /// </summary>
    public string[] Controls => [..registry.Keys];

    /// <summary>
    /// Gets a single control spec by ID.
    /// </summary>
    /// <param name="id">The ID of the control to get the spec for.</param>
    /// <returns>The control spec for the provided control ID.</returns>
    public ControlSpec Get(string id) => registry[id];
}

/// <summary>
/// The builder object used to create the control registry.
/// </summary>
public class ControlRegistryBuilder
{
    private readonly Dictionary<string, ControlSpec> registry = [];

    /// <summary>
    /// Registers a new control spec.
    /// </summary>
    /// <param name="control">The control spec to register.</param>
    /// <returns>This builder object.</returns>
    /// <exception cref="ArgumentException">Thrown if a control has already been registered with this ID.</exception>
    public ControlRegistryBuilder Register(ControlSpec control)
    {
        if (!registry.TryAdd(control.ID, control))
            throw new ArgumentException("Duplicate control ID " + control.ID, nameof(control));
        return this;
    }

    /// <summary>
    /// Builds and finalizes the control registry.
    /// </summary>
    /// <returns>The finalized control registry.</returns>
    public ControlRegistry Build() => new(registry.ToFrozenDictionary());
}