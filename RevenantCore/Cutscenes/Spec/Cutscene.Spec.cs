using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using RevenantCore.Scenes;
using RevenantCore.Util;
using YamlDotNet.Serialization;

namespace RevenantCore.Cutscenes.Spec;

// TODO: more unit tests

/// <summary>
/// The base dataspec for cutscenes. All other cutscene specs should inherit from this one.
/// </summary>
public abstract class CutsceneSpec
{
    /// <summary>
    /// The spec for the filter which will decide if this cutscene is triggered.
    /// </summary>
    public EventFilterSpec Filter { get; set; } = new();

    /// <summary>
    /// Converts this spec into the corresponding in-memory cutscene object.
    /// </summary>
    /// <param name="universe">The universe in which this cutscene should be created.</param>
    /// <returns>The in-memory cutscene corresponding to this spec's type.</returns>
    public abstract Cutscene Create(Universe universe);
}

/// <summary>
/// The base spec for block cutscenes, which have other cutscenes as children.
/// </summary>
public abstract class BlockSpec : CutsceneSpec
{
    /// <summary>
    /// The specs for all the children of this cutscene.
    /// </summary>
    public CutsceneSpec[] Children { get; set; } = [];
}

/// <summary>
/// The spec for a sequential block, which will execute its children one by one in order.
/// </summary>
public class SequentialBlockSpec : BlockSpec
{
    public override Cutscene Create(Universe universe) => new SequentialBlock(universe, this);
}

/// <summary>
/// The spec for a concurrent block, which will execute its children all at the same time.
/// </summary>
public class ConcurrentBlockSpec : BlockSpec
{
    public override Cutscene Create(Universe universe) => new ConcurrentBlock(universe, this);
}

/// <summary>
/// A finalized cutscene spec registry. Contains all type mappings populated during the registry phase.
/// </summary>
/// <param name="registry">The finalized tag to type map.</param>
public class CutsceneRegistry(FrozenDictionary<string, Type> registry) : ISpec
{
    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T>
    {
        foreach (KeyValuePair<string, Type> item in registry)
            builder.WithTagMapping(item.Key, item.Value);
        return builder;
    }
}

/// <summary>
/// A builder object used to create the cutscene registry.
/// </summary>
public class CutsceneRegistryBuilder
{
    private readonly Dictionary<string, Type> registry = [];

    /// <summary>
    /// Registers a new tag-to-type mapping.
    /// </summary>
    /// <param name="tag">The tag to map, without the "!" prefix.</param>
    /// <param name="type">The spec type to map. Must have no constructor arguments.</param>
    /// <returns>This builder object.</returns>
    /// <exception cref="ArgumentException">Thrown if the tag has already been registered.</exception>
    public CutsceneRegistryBuilder Register(string tag, Type type)
    {
        if (!registry.TryAdd("!" + tag, type))
            throw new ArgumentException("Duplicate tag name " + tag, nameof(tag));
        return this;
    }

    /// <summary>
    /// Builds and finalizes the cutscene registry.
    /// </summary>
    /// <returns>The finalized cutscene registry.</returns>
    public CutsceneRegistry Build() => new(registry.ToFrozenDictionary());
}