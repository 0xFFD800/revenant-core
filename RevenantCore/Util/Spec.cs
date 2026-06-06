using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RevenantCore.Util;

/// <summary>
/// Defines standard serialization options to be used for all YAML (de)serialization tasks.
/// </summary>
public class Spec
{
    /// <summary>
    /// Populates standard options onto either a serializer or deserializer builder.
    /// </summary>
    /// <typeparam name="T">Either SerializerBuilder or DeserializerBuilder; the type of the builder having its options populated.</typeparam>
    /// <param name="builder">The builder on which to populate standard options onto.</param>
    /// <returns><paramref name="builder"/>, with the standard serialization options populated onto it.</returns>
    private static T PopulateOptions<T>(T builder) where T : BuilderSkeleton<T> => builder
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithEnumNamingConvention(CamelCaseNamingConvention.Instance);

    /// <summary>
    /// The standard YAML serialization object.
    /// </summary>
    public static readonly ISerializer Serializer = PopulateOptions(new SerializerBuilder()).Build();

    /// <summary>
    /// The standard YAML deserialization object.
    /// </summary>
    public static readonly IDeserializer Deserializer = PopulateOptions(new DeserializerBuilder()).Build();
}