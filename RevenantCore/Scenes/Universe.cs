using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Cutscenes;
using RevenantCore.Entities;
using RevenantCore.Entities.Spec;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

public class Universe(Core core, EventCollection events)
{
    private readonly Dictionary<string, ControlBindSpec> bindings = [];
    private FrozenDictionary<string, ControlState> prevControlStates = FrozenDictionary<string, ControlState>.Empty;

    public Core Core => core;
    public EventCollection Events => events;

    public FrozenDictionary<string, ControlState> GetControlStates(FrameTime time)
    {
        Dictionary<string, ControlState> result = [];
        foreach (string id in Core.Controls.IDs)
        {
            ControlBindSpec spec = bindings.GetValueOrDefault(id, Core.Controls.Get(id).Default);
            bool pressed = spec.Keys.Any(k => Core.Inputs.Keyboard.IsKeyDown(k))
                || spec.Buttons.Any(b => Core.Inputs.GamePad(b.Player).IsButtonDown(b.Button))
                || spec.MouseButtons.Any(b => b switch
                {
                    MouseButtons.Left => Core.Inputs.Mouse.LeftButton,
                    MouseButtons.Right => Core.Inputs.Mouse.RightButton,
                    MouseButtons.Middle => Core.Inputs.Mouse.MiddleButton,
                    _ => throw new ArgumentException("Unsupported mouse button")
                } == ButtonState.Pressed);
            ControlState prevState = prevControlStates.GetValueOrDefault(id, new(ControlPositions.Up, 0));
            result.Add(id, prevState.Position switch
            {
                ControlPositions.Press | ControlPositions.Down => pressed 
                    ? new(ControlPositions.Down, prevState.Millis + time.MillisElapsed) 
                    : new(ControlPositions.Release, 0),
                ControlPositions.Release | ControlPositions.Up => pressed 
                    ? new(ControlPositions.Press, 0) 
                    : new(ControlPositions.Up, prevState.Millis + time.MillisElapsed),
                _ => throw new ArgumentException("Unsupported control position")
            });
        }
        prevControlStates = result.ToFrozenDictionary();
        return prevControlStates;
    }
}