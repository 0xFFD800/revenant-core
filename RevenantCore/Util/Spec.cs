using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RevenantCore.Util;

// TODO: Documentation, more unit tests
public interface ISpec
{
    /// <summary>
    /// Populates serialization options onto either a serializer or deserializer builder.
    /// </summary>
    /// <typeparam name="T">Either SerializerBuilder or DeserializerBuilder; the type of the builder having its options populated.</typeparam>
    /// <param name="builder">The builder on which to populate standard options onto.</param>
    /// <returns><paramref name="builder"/>, with the standard serialization options populated onto it.</returns>
    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T>;
}

/// <summary>
/// Defines standard serialization options to be used for all YAML (de)serialization tasks.
/// </summary>
public class StandardSpec : ISpec
{
    private StandardSpec()
    {

    }

    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T> => builder
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnumNamingConvention(CamelCaseNamingConvention.Instance);

    public static readonly StandardSpec Instance = new();
}

public static class Serializers
{
    /// <summary>
    /// Creates a new YAML serialization object given the spec sheet.
    /// </summary>
    public static ISerializer CreateSerializer(ISpec[] specs)
    {
        SerializerBuilder b = new();
        foreach (ISpec spec in specs)
            spec.PopulateOptions(b);
        return b.Build();
    }

    /// <summary>
    /// Creates a new YAML deserialization object given the spec sheet.
    /// </summary>
    public static IDeserializer CreateDeserializer(ISpec[] specs)
    {
        DeserializerBuilder b = new();
        foreach (ISpec spec in specs)
            spec.PopulateOptions(b);
        return b.Build();
    }
}