using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RevenantCore.Util;

/// <summary>
/// Specifies additional options to be used on a serializer or deserializer builder. 
/// </summary>
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
internal class StandardSpec : ISpec
{
    private StandardSpec()
    {

    }

    public T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T> => builder
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnumNamingConvention(CamelCaseNamingConvention.Instance);

    internal static readonly StandardSpec Instance = new();
}

/// <summary>
/// A static class with methods to create serializers given spec sheets.
/// Serializers will always include the standard spec.
/// </summary>
public static class Serializers
{
    /// <summary>
    /// Creates a new YAML serialization object given the spec sheet.
    /// </summary>
    public static ISerializer CreateSerializer(ISpec[] specs)
    {
        SerializerBuilder b = new();
        StandardSpec.Instance.PopulateOptions(b);
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
        StandardSpec.Instance.PopulateOptions(b);
        foreach (ISpec spec in specs)
            spec.PopulateOptions(b);
        return b.Build();
    }
}