using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using RevenantCore.Util;
using YamlDotNet.Serialization;

namespace RevenantCore.Cutscenes.Spec;

// TODO: more unit tests

/// <summary>
/// The base dataspec for cutscenes. All other cutscene specs should inherit from this one
/// </summary>
public class CutsceneSpec
{
    /// <summary>
    /// The spec for the filter which will decide if this cutscene is triggered.
    /// </summary>
    public EventFilterSpec Filter { get; set; } = new();
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