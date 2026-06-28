using RevenantCore.Scenes;

namespace RevenantCore.Entities;

/// <summary>
/// The result of an attempt to handle control input.
/// </summary>
public enum ControlResult
{
    /// <summary>
    /// Indicates that the specified input was invalid for this handler,
    /// but that handling should not continue for the control.
    /// </summary>
    Failure,
    /// <summary>
    /// Indicates that no action was taken for the specified input,
    /// and the control should be passed to the next handler.
    /// </summary>
    Pass,
    /// <summary>
    /// Indicates that the input was successfully handled.
    /// Handling for the control should not continue.
    /// </summary>
    Success
}

/// <summary>
/// A mortal object which should receive and possibly handle controls.
/// </summary>
public interface IControllable : IMortal
{
    /// <summary>
    /// Attempts to handle and take action based on the provided control input.
    /// </summary>
    /// <param name="control">The ID of the control being processed.</param>
    /// <returns>An enumerated result expressing how the control should be treated by future handlers.</returns>
    ControlResult Handle(string control);

    /// <summary>
    /// If this input could be consumed by multiple objects, Priority
    /// determines the order in which the objects receive the control
    /// input. Higher priority control handlers will have Handle called first.
    /// </summary>
    float Priority { get; }
}