using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Entities;

/// <summary>
/// An object which applies behavior to an entity.
/// Behavior may be applied in response to input, at random, 
/// based on the environment, or not at all.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Applies behavior for a given run of the Tick loop.
    /// </summary>
    /// <param name="entity">The entity to which to apply behavior to.</param>
    /// <param name="scene">The scene in which behavior is being applied.</param>
    /// <param name="time">The FrameTime of the current frame.</param>
    void Apply(Entity entity, Scene scene, FrameTime time);
}