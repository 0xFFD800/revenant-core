using Microsoft.Xna.Framework.Input;

namespace RevenantCore.Entities.Spec;

/// <summary>
/// Represents a button on a mouse.
/// </summary>
public enum MouseButtons
{
    /// <summary>
    /// The left mouse button.
    /// </summary>
    Left,
    /// <summary>
    /// The right mouse button.
    /// </summary>
    Right,
    /// <summary>
    /// The middle mouse button (a press of the scroll wheel).
    /// </summary>
    Middle
}

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