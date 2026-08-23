using System;
using System.Linq;
using Microsoft.Xna.Framework;
using RevenantCore.Cutscenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;

namespace RevenantCore.Scenes;

/// <summary>
/// In-game implementation of an interaction area which triggers based on player behavior.
/// </summary>
/// <param name="spec">The YAML-deserializable spec on which to base this zone's parameters.</param>
public class InteractionArea(InteractionAreaSpec spec) : ITickable
{
    private Vector3 basePos = spec.Base.Data, size = spec.Bounds.Data;
    private Cutscene[] allCutscenes = [];
    private int index = 0;

    public BoundingBox Bounds => new(
        basePos - new Vector3(size.X / 2, 0, size.Z / 2),
        basePos + new Vector3(size.X / 2, size.Y, size.Z / 2));

    public bool IsDead { get; private set; } = false;

    public void Create(Scene scene, FrameTime time)
    {
        allCutscenes = [.. spec.Cutscenes.Select(c => c.Create(scene.Universe))];
        index = 0;
        IsDead = allCutscenes.Length == 0;
    }

    public void Glean(Scene scene, FrameTime time) { }

    public void Tick(Scene scene, FrameTime time)
    {
        if (allCutscenes.Length > 0 && ShouldTrigger(scene))
        {
            scene.Add(allCutscenes[index++], scene, time);
            if (index >= allCutscenes.Length)
            {
                if (spec.SubsequentBehavior == SubsequentBehavior.Loop)
                    index = 0;
                else if (spec.SubsequentBehavior == SubsequentBehavior.RepeatLast)
                    index = allCutscenes.Length - 1;
                else
                    IsDead = true;
            }
        }
    }

    private bool ShouldTrigger(Scene scene)
    {
        return scene.IsCapturing(null)
            && scene.CollisionsWith(Bounds).Any(c => c.Interactions.Contains(spec.Type));
    }
}