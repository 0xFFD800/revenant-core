using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Entities;

public interface IAgent
{
    void Apply(Entity entity, Scene scene, FrameTime time);
}