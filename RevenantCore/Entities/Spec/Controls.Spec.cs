using Microsoft.Xna.Framework.Input;

namespace RevenantCore.Entities.Spec;

public enum MouseButton
{
    Left,
    Right,
    Middle
}

public class ControlBindSpec
{
    public Keys? Key { get; set; }
    public Buttons? Button { get; set; }
    public MouseButton? MouseButton { get; set; }
}

public class ControlSpec
{
    public string ID { get; set; } = "default";
    public string Name { get; set; } = "Default";
    public string Descr { get; set; } = "Undefined";
    public ControlBindSpec Default { get; set; } = new();
}