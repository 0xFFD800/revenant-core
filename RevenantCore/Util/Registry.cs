using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace RevenantCore.Util;

/// <summary>
/// A finalized spec registry. Contains all type mappings populated during the registry phase.
/// </summary>
/// <param name="registry">The finalized tag to type map.</param>
internal class SpecRegistry(FrozenDictionary<string, Type> registry) : ISpec
{
    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T>
    {
        foreach (KeyValuePair<string, Type> item in registry)
            builder.WithTagMapping(item.Key, item.Value);
        return builder;
    }
}

/// <summary>
/// A builder object used to create the registry.
/// </summary>
public class SpecRegistryBuilder
{
    private readonly Dictionary<string, Type> registry = [];

    /// <summary>
    /// Registers a new tag-to-type mapping.
    /// </summary>
    /// <param name="tag">The tag to map, without the "!" prefix.</param>
    /// <param name="type">The spec type to map. Must have no constructor arguments.</param>
    /// <returns>This builder object.</returns>
    /// <exception cref="ArgumentException">Thrown if the tag has already been registered.</exception>
    public SpecRegistryBuilder Register(string tag, Type type)
    {
        if (!registry.TryAdd("!" + tag, type))
            throw new ArgumentException("Duplicate tag name " + tag, nameof(tag));
        return this;
    }

    /// <summary>
    /// Builds and finalizes the registry.
    /// </summary>
    /// <returns>The finalized registry.</returns>
    public ISpec Build() => new SpecRegistry(registry.ToFrozenDictionary());
}