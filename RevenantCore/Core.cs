using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Graphics;
using RevenantCore.Graphics.Spec;
using RevenantCore.Scenes;
using RevenantCore.Util;
using YamlDotNet.Serialization;

namespace RevenantCore;

/// <summary>
/// An implementation of core functionality. Required to register necessary objects.
/// </summary>
public interface IImpl
{
    /// <summary>
    /// Called to register additional cutscene type mappings.
    /// </summary>
    /// <param name="registry">The registry builder to register new type mappings with.</param>
    /// <returns>The registry builder passed to this method.</returns>
    CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry);
}

/// <summary>
/// A wrapper for the ContentBuilder object. Used to load XNB files into memory.
/// </summary>
public interface ILoader
{
    /// <summary>
    /// Loads a sprite XNB file into memory as a drawable object.
    /// </summary>
    /// <param name="path">The path of the file to load (relative to the base directory, without a file extension).</param>
    /// <returns>The sprite loaded for the provided path.</returns>
    Drawable LoadSprite(string path);
}

/// <summary>
/// The core implementation object, which registers the core behavior.
/// </summary>
internal class CoreImpl : IImpl
{
    public CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry) => registry
        .Register("sequentialBlock", typeof(SequentialBlockSpec))
        .Register("concurrentBlock", typeof(ConcurrentBlockSpec))
        .Register("load", typeof(LoadCutsceneSpec));
}

/// <summary>
/// The core object. Collates all impls along with the core behavior impl and uses it to load items from spec.
/// </summary>
public class Core
{
    private readonly CoreImpl coreImpl = new();
    private readonly ISpec cutsceneRegistry;
    private readonly ILoader loader;

    public Core(ILoader loader, IImpl[] impls)
    {
        this.loader = loader;

        IImpl[] allImpls = [..impls.Prepend(coreImpl)];

        CutsceneRegistryBuilder cutsceneBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterCutscenes(cutsceneBuilder);
        cutsceneRegistry = cutsceneBuilder.Build();
    }

    /// <summary>
    /// Loads a cutscene from a dataspec.
    /// </summary>
    /// <param name="universe">The universe in which to create the new cutscene.</param>
    /// <param name="yaml">The YAML to deserialize and load into a new cutscene object.</param>
    /// <returns>The cutscenes created for the provided universe and cutscene YAML.</returns>
    public Cutscene LoadCutscene(Universe universe, string yaml)
    {
        Trace.Assert(universe.Core == this);
        IDeserializer deserializer = Serializers.CreateDeserializer([cutsceneRegistry]);
        return deserializer.Deserialize<CutsceneSpec>(yaml).Create(universe);
    }

    /// <summary>
    /// Loads an animation collection from spec.
    /// </summary>
    /// <param name="yaml">The YAML to deserialize and load into a new animation collection.</param>
    /// <returns>The animation collection created for the provided YAML.</returns>
    public AnimationCollection LoadAnimationCollection(string yaml)
    {
        IDeserializer deserializer = Serializers.CreateDeserializer([]);
        AnimationCollectionSpec spec = deserializer.Deserialize<AnimationCollectionSpec>(yaml);
        Dictionary<string, Drawable> sprites = spec.Sprites
            .Select(s => new KeyValuePair<string, Drawable>(s.Key, loader.LoadSprite(s.Value)))
            .ToDictionary();
        return new(spec.Animations.Select(a => 
            new KeyValuePair<string, Animation>(a.Key, new([..a.Value.Select(f => 
                sprites.GetValueOrDefault(f.Sprite ?? spec.DefaultSprite, 
                    loader.LoadSprite(f.Sprite ?? spec.DefaultSprite)))], spec.MillisPerFrame)))
            .ToFrozenDictionary(), spec.DefaultAnimation);
    }
}