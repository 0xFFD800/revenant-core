using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
using RevenantCore.Entities;
using RevenantCore.Entities.Spec;
using RevenantCore.Graphics;
using RevenantCore.Graphics.Spec;
using RevenantCore.Scenes;
using RevenantCore.Scenes.Spec;
using RevenantCore.Util;
using YamlDotNet.Serialization;

namespace RevenantCore;

/// <summary>
/// An implementation of core functionality. Required to register necessary objects.
/// </summary>
public interface IImpl
{
    /// <summary>
    /// Called to register additional control spec mappings.
    /// </summary>
    /// <param name="registry">The registry builder to register new controls with.</param>
    /// <returns>The registry builder passed to this method.</returns>
    ControlRegistryBuilder RegisterControls(ControlRegistryBuilder registry);

    /// <summary>
    /// Called to register additional cutscene type mappings.
    /// </summary>
    /// <param name="registry">The registry builder to register new type mappings with.</param>
    /// <returns>The registry builder passed to this method.</returns>
    SpecRegistryBuilder RegisterCutscenes(SpecRegistryBuilder registry);

    /// <summary>
    /// Called to register additional agent type mappings.
    /// </summary>
    /// <param name="registry">The registry builder to register new type mappings with.</param>
    /// <returns>The registry builder passed to this method.</returns>
    SpecRegistryBuilder RegisterAgents(SpecRegistryBuilder registry);

    /// <summary>
    /// Called to register additional tracker type mappings.
    /// </summary>
    /// <param name="registry">The registry builder to register new type mappings with.</param>
    /// <returns>The registry builder passed to this method.</returns>
    SpecRegistryBuilder RegisterTrackers(SpecRegistryBuilder registry);
}

/// <summary>
/// A wrapper for the ContentBuilder object. Used to load XNB files into memory.
/// </summary>
public interface ILoader
{
    /// <summary>
    /// Loads a sprite XNB file into memory as a drawable object.
    /// </summary>
    /// <param name="path">The path of the file to load (relative to the Content directory, without a file extension).</param>
    /// <returns>The sprite loaded for the provided path.</returns>
    Drawable LoadSprite(string path);

    /// <summary>
    /// Loads a spritefont XNB file into memory as an IFont object.
    /// </summary>
    /// <param name="path">The path of the spritefont file to load (relative to the Content directory, without a file extension).</param>
    /// <returns>The font loaded for the provided path.</returns>
    IFont LoadFont(string path);
}

/// <summary>
/// A wrapper for the Keyboard, GamePad, and Mouse objects. Used to determine input states.
/// </summary>
public interface IInputs
{
    /// <summary>
    /// The current state of the keyboard object.
    /// </summary>
    KeyboardState Keyboard { get; }

    /// <summary>
    /// Gets the gamepad state for a given player index.
    /// </summary>
    /// <param name="player">The player to query the gamepad state for.</param>
    /// <returns>The gamepad state for the specified player index.</returns>
    GamePadState GamePad(PlayerIndex player);

    /// <summary>
    /// The current state of the mouse object.
    /// </summary>
    MouseState Mouse { get; }
}

/// <summary>
/// The core implementation object, which registers the core behavior.
/// </summary>
internal class CoreImpl : IImpl
{
    public ControlRegistryBuilder RegisterControls(ControlRegistryBuilder registry) => registry;

    public SpecRegistryBuilder RegisterCutscenes(SpecRegistryBuilder registry) => registry
        .Register("sequentialBlock", typeof(SequentialBlockSpec))
        .Register("concurrentBlock", typeof(ConcurrentBlockSpec))
        .Register("load", typeof(LoadCutsceneSpec));

    public SpecRegistryBuilder RegisterAgents(SpecRegistryBuilder registry) => registry
        .Register("nullAgent", typeof(NullAgentSpec))
        .Register("trackingAgent", typeof(TrackingAgentSpec))
        .Register("inputAgent", typeof(InputAgentSpec));
    
    public SpecRegistryBuilder RegisterTrackers(SpecRegistryBuilder registry) => registry
        .Register("moveableTracker", typeof(MoveableTrackerSpec))
        .Register("forwardLookingTracker", typeof(ForwardLookingTrackerSpec))
        .Register("wanderTracker", typeof(WanderTrackerSpec));
}

/// <summary>
/// The core object. Collates all impls along with the core behavior impl and uses it to load items from spec.
/// </summary>
public class Core
{
    private readonly CoreImpl coreImpl = new();
    private readonly ISpec cutsceneRegistry, agentRegistry, trackerRegistry;
    private readonly ILoader loader;
    private readonly Dictionary<string, AnimationCollection> cachedAnimations = [];
    private readonly Dictionary<string, IFont> cachedFonts = [];

    /// <summary>
    /// The finalized control registry as created by the implementation objects.
    /// </summary>
    public ControlRegistry Controls { get; }

    /// <summary>
    /// A view into the external inputs into this application.
    /// </summary>
    public IInputs Inputs { get; }

    public Core(ILoader loader, IInputs inputs, IImpl[] impls)
    {
        this.loader = loader;
        Inputs = inputs;

        IImpl[] allImpls = [.. impls.Prepend(coreImpl)];

        ControlRegistryBuilder controlBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterControls(controlBuilder);
        Controls = controlBuilder.Build();

        SpecRegistryBuilder cutsceneBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterCutscenes(cutsceneBuilder);
        cutsceneRegistry = cutsceneBuilder.Build();

        SpecRegistryBuilder agentBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterAgents(agentBuilder);
        agentRegistry = agentBuilder.Build();

        SpecRegistryBuilder trackerBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterTrackers(trackerBuilder);
        trackerRegistry = trackerBuilder.Build();
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
    /// Loads an animation collection from spec, unless it has already been loaded,
    /// in which case it returns the existing collection.
    /// </summary>
    /// <param name="path">The path to the YAML file to deserialize and load into a new animation collection.</param>
    /// <returns>The animation collection created for the provided YAML file.</returns>
    public AnimationCollection LoadAnimationCollection(string path)
    {
        if (cachedAnimations.TryGetValue(path, out AnimationCollection? result))
            return result;
        string yaml = File.ReadAllText(path);
        IDeserializer deserializer = Serializers.CreateDeserializer([]);
        AnimationCollectionSpec spec = deserializer.Deserialize<AnimationCollectionSpec>(yaml);
        Dictionary<string, Drawable> sprites = spec.Sprites
            .Select(s => new KeyValuePair<string, Drawable>(s.Key, loader.LoadSprite(s.Value)))
            .ToDictionary();
        result = new(spec.Animations.Select(a =>
            new KeyValuePair<string, Animation>(a.Key, new([..a.Value.Select(f =>
                (sprites.GetValueOrDefault(f.Sprite ?? spec.DefaultSprite)
                        ?? loader.LoadSprite(f.Sprite ?? spec.DefaultSprite))
                    .ShallowCopy()
                    .SetSource(f.Source?.Data))], spec.MillisPerFrame)))
            .ToFrozenDictionary(), spec.DefaultAnimation);
        cachedAnimations.Add(path, result);
        return result;
    }

    /// <summary>
    /// Loads an entity from spec.
    /// </summary>
    /// <param name="yaml">The YAML to deserialize and load into a new entity.</param>
    /// <returns>The entity instance created for the provided YAML.</returns>
    public Entity LoadEntity(string yaml)
    {
        IDeserializer deserializer = Serializers.CreateDeserializer([agentRegistry, trackerRegistry]);
        EntitySpec spec = deserializer.Deserialize<EntitySpec>(yaml);
        return new Entity(spec, LoadAnimationCollection(spec.Animations));
    }

    /// <summary>
    /// Loads a font from file, unless it has already been loaded, in which case it loads the existing font.
    /// </summary>
    /// <param name="font">The path to load the font for.</param>
    /// <returns>The font with the specified path.</returns>
    public IFont LoadFont(string font) 
    {
        if (cachedFonts.TryGetValue(font, out IFont? result))
            return result;
        result = loader.LoadFont(font);
        cachedFonts.Add(font, result);
        return result;
    }

    /// <summary>
    /// Loads a scene from spec.
    /// </summary>
    /// <param name="universe">The universe in which to create this scene.</param>
    /// <param name="yaml">The YAML to deserialize and load into a new scene.</param>
    /// <param name="trigger">The trigger which this scene should load to begin with.</param>
    /// <returns>The scene instance created for the provided YAML.</returns>
    public Scene LoadScene(Universe universe, string yaml, string? trigger)
    {
        Trace.Assert(universe.Core == this);
        IDeserializer deserializer = Serializers.CreateDeserializer([cutsceneRegistry, agentRegistry, trackerRegistry]);
        SceneSpec spec = deserializer.Deserialize<SceneSpec>(yaml);
        return new Scene(universe, new ControlTracker(), new KeyboardTracker(), spec, trigger ?? "default");
    }
}