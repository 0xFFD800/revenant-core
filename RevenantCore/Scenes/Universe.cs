using RevenantCore.Cutscenes;

namespace RevenantCore.Scenes;

public class Universe(Core core, EventCollection events)
{
    public Core Core => core;
    public EventCollection Events => events;
}