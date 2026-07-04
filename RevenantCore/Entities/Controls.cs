using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Entities.Spec;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Entities;

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
/// Represents the position of a given control.
/// </summary>
public enum ControlPositions
{
    /// <summary>
    /// A new press.
    /// Was not pressed the previous tick, but is pressed now.
    /// </summary>
    Press,
    /// <summary>
    /// A sustained press.
    /// Was pressed the previous tick and is pressed now.
    /// </summary>
    Down,
    /// <summary>
    /// A new release.
    /// Was pressed the previous tick, but is not pressed now.
    /// </summary>
    Release,
    /// <summary>
    /// Not pressed.
    /// Was pressed neither the previous tick nor this one.
    /// </summary>
    Up
}

/// <summary>
/// Represents the state of a given control.
/// </summary>
/// <param name="Position">The position which this control is currently in.</param>
/// <param name="Millis">The number of milliseconds for which this control has been pressed.</param>
public record struct ControlState(ControlPositions Position, double Millis);

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
/// A tracker which tracks controls and how long they have been in their current states.
/// </summary>
public class ControlTracker : ITickable
{
    public bool IsDead => false;

    /// <summary>
    /// The current state of the controls, calculated as of the most recent tick.
    /// </summary>
    public FrozenDictionary<string, ControlState> States { get; private set; } = FrozenDictionary<string, ControlState>.Empty;

    private void CalcStates(Universe universe, Core core, FrameTime time)
    {
        FrozenDictionary<string, ControlState> prevStates = States.ToFrozenDictionary();
        Dictionary<string, ControlState> currStates = [];
        foreach (string id in core.Controls.IDs)
        {
            ControlBindSpec spec = universe.Bindings.GetValueOrDefault(id, core.Controls.Get(id).Default);
            bool pressed = spec.Keys.Any(k => core.Inputs.Keyboard.IsKeyDown(k))
                || spec.Buttons.Any(b => core.Inputs.GamePad(b.Player).IsButtonDown(b.Button))
                || spec.MouseButtons.Any(b => b switch
                {
                    MouseButtons.Left => core.Inputs.Mouse.LeftButton,
                    MouseButtons.Right => core.Inputs.Mouse.RightButton,
                    MouseButtons.Middle => core.Inputs.Mouse.MiddleButton,
                    _ => throw new ArgumentException("Unsupported mouse button")
                } == ButtonState.Pressed);
            ControlState prevState = prevStates.GetValueOrDefault(id, new(ControlPositions.Up, 0));
            currStates.Add(id, prevState.Position switch
            {
                ControlPositions.Press or ControlPositions.Down => pressed
                    ? new(ControlPositions.Down, prevState.Millis + time.MillisElapsed)
                    : new(ControlPositions.Release, 0),
                ControlPositions.Release or ControlPositions.Up => pressed
                    ? new(ControlPositions.Press, 0)
                    : new(ControlPositions.Up, prevState.Millis + time.MillisElapsed),
                _ => throw new ArgumentException("Unsupported control position")
            });
        }
        States = currStates.ToFrozenDictionary();
    }

    public void Create(Scene scene, FrameTime time)
    {
        CalcStates(scene.Universe, scene.Universe.Core, time);
    }

    public void Glean(Scene scene, FrameTime time) { }

    public void Tick(Scene scene, FrameTime time)
    {
        CalcStates(scene.Universe, scene.Universe.Core, time);
    }
}