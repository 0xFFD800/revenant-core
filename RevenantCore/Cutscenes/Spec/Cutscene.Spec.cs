using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using RevenantCore.Util;
using YamlDotNet.Serialization;

namespace RevenantCore.Cutscenes.Spec;

// TODO: Documentation, more unit tests
public class CutsceneSpec
{
    public EventFilterSpec Filter { get; set; } = new();
}

public class CutsceneRegistry(FrozenDictionary<string, Type> registry) : ISpec
{
    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T>
    {
        foreach (KeyValuePair<string, Type> item in registry)
            builder.WithTagMapping(item.Key, item.Value);
        return builder;
    }
}

public class CutsceneRegistryBuilder
{
    private readonly Dictionary<string, Type> registry = [];

    public CutsceneRegistryBuilder Register(string tag, Type type)
    {
        if (!registry.TryAdd("!" + tag, type))
            throw new ArgumentException("Duplicate tag name " + tag, nameof(tag));
        return this;
    }

    public CutsceneRegistry Build() => new(registry.ToFrozenDictionary());
}