using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
public interface IControlTracker : ITickable
{
    /// <summary>
    /// The current state of the controls, calculated as of the most recent tick.
    /// </summary>
    public FrozenDictionary<string, ControlState> States { get; }
}

/// <summary>
/// An implementation of IControlTracker which derives the current control state from the parent scene.
/// </summary>
public class ControlTracker : IControlTracker
{
    public bool IsDead => false;

    public FrozenDictionary<string, ControlState> States { get; protected set; } = FrozenDictionary<string, ControlState>.Empty;

    protected virtual void CalcStates(Universe universe, Core core, FrameTime time)
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
            currStates.Add(id, GetCurrState(prevState, time, pressed));
        }
        States = currStates.ToFrozenDictionary();
    }

    protected static ControlState GetCurrState(ControlState prevState, FrameTime time, bool pressed) =>
        prevState.Position switch
        {
            ControlPositions.Press or ControlPositions.Down => pressed
                ? new(ControlPositions.Down, prevState.Millis + time.MillisElapsed)
                : new(ControlPositions.Release, 0),
            ControlPositions.Release or ControlPositions.Up => pressed
                ? new(ControlPositions.Press, 0)
                : new(ControlPositions.Up, prevState.Millis + time.MillisElapsed),
            _ => throw new ArgumentException("Unsupported control position")
        };

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

/// <summary>
/// A special control tracker which tracks the keyboard input as the expected string output.
/// </summary>
public class KeyboardTracker : ControlTracker, IControlTracker
{
    public const string Back = "back";
    public const string Delete = "delete";
    public const string Home = "home";
    public const string End = "end";
    public readonly static DirectionControlSpec Directions = new();

    // This should probably be configurable in settings.
    public const int RepeatMillis = 1500;

    private static string? ShiftedNumKey(Keys key) => key switch
    {
        Keys.D0 => ")",
        Keys.D1 => "!",
        Keys.D2 => "@",
        Keys.D3 => "#",
        Keys.D4 => "$",
        Keys.D5 => "%",
        Keys.D6 => "^",
        Keys.D7 => "&",
        Keys.D8 => "*",
        Keys.D9 => "(",
        _ => throw new ArgumentException("Argument must be a number key (D0-D9)", nameof(key))
    };

    private static string? GetID(Keys key, bool shift, bool capsLock, bool numLock)
    {
        string name = Enum.GetName(key) ?? " ";
        return key switch
        {
            (>= Keys.A) and (<= Keys.Z) => shift ^ capsLock ? name.ToUpper() : name.ToLower(),
            (>= Keys.D0) and (<= Keys.D9) => shift ? ShiftedNumKey(key) : (key - Keys.D0).ToString(),
            (>= Keys.NumPad0) and (<= Keys.NumPad9) => numLock ? (key - Keys.NumPad0).ToString() : null,
            Keys.Space => " ",
            Keys.Back => Back,
            Keys.Delete => Delete,
            Keys.Home => Home,
            Keys.End => End,
            Keys.Enter => "\n",
            Keys.OemBackslash => shift ? "|" : "\\",
            Keys.OemCloseBrackets => shift ? "}" : "]",
            Keys.OemComma => shift ? "<" : ",",
            Keys.OemMinus => shift ? "_" : "-",
            Keys.OemOpenBrackets => shift ? "{" : "[",
            Keys.OemPeriod => shift ? ">" : ".",
            Keys.OemPlus => shift ? "+" : "=",
            Keys.OemQuestion => shift ? "?" : "/",
            Keys.OemQuotes => shift ? "\"" : "'",
            Keys.OemSemicolon => shift ? ":" : ";",
            Keys.OemTilde => shift ? "~" : "`",
            Keys.Tab => "\t",
            Keys.Right => Directions.Right,
            Keys.Left => Directions.Left,
            Keys.Up => Directions.Up,
            Keys.Down => Directions.Down,
            _ => null
        };
    }

    protected override void CalcStates(Universe universe, Core core, FrameTime time)
    {
        FrozenDictionary<string, ControlState> prevStates = States.ToFrozenDictionary();
        Dictionary<string, ControlState> currStates = [];
        KeyboardState state = core.Inputs.Keyboard;
        foreach (Keys key in Enum.GetValues<Keys>())
        {
            string? id = GetID(key, state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift), state.CapsLock, state.NumLock);
            if (id == null) continue;

            bool pressed = state.IsKeyDown(key);
            ControlState currState = GetCurrState(prevStates.GetValueOrDefault(id, new(ControlPositions.Up, 0)), time, pressed);
            if (currStates.Remove(id, out ControlState existing))
                currStates.Add(id, currState.Position.In(ControlPositions.Down, ControlPositions.Press) ? currState : existing);
            else
                currStates.Add(id, currState);
        }
        States = currStates.ToFrozenDictionary();
    }
}

/// <summary>
/// An object which can capture controls from the current scene to handle them exclusively.
/// </summary>
public interface IControllable : IMortal
{
    /// <summary>
    /// Whether the provided controllable object is part of this chain of controllables or not.
    /// Used to determine whether this object is part of the control-capturing chain.
    /// </summary>
    /// <param name="other">The object to attempt to match to this chain.</param>
    /// <returns>Whether the provided cutscene is part of this cutscene's active block.</returns>
    bool Matches(IControllable other);
}