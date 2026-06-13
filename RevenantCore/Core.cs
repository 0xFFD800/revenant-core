using System.IO;
using System.Linq;
using RevenantCore.Cutscenes;
using RevenantCore.Cutscenes.Spec;
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
/// The core implementation object, which registers the core behavior.
/// </summary>
public class CoreImpl : IImpl
{
    public CutsceneRegistryBuilder RegisterCutscenes(CutsceneRegistryBuilder registry) => registry
        .Register("sequentialBlock", typeof(SequentialBlockSpec))
        .Register("concurrentBlock", typeof(ConcurrentBlockSpec));
}

/// <summary>
/// The core object. Collates all impls along with the core behavior impl and uses it to load items from spec.
/// </summary>
public class Core
{
    private readonly CoreImpl coreImpl = new();
    private readonly ISpec cutsceneRegistry;

    public Core(IImpl[] impls)
    {
        IImpl[] allImpls = [..impls.Prepend(coreImpl)];

        CutsceneRegistryBuilder cutsceneBuilder = new();
        foreach (IImpl impl in allImpls)
            impl.RegisterCutscenes(cutsceneBuilder);
        cutsceneRegistry = cutsceneBuilder.Build();
    }

    /// <summary>
    /// Loads a list of cutscenes from a dataspec.
    /// This is likely a temporary method... in the future there won't just be files of cutscenes.
    /// </summary>
    /// <param name="universe">The universe in which to create the new cutscenes.</param>
    /// <param name="filePath">The path to the file containing the cutscene data.</param>
    /// <returns>The cutscenes created for the provided universe and cutscene data file.</returns>
    public Cutscene[] LoadCutscenes(Universe universe, string filePath)
    {
        string yaml = File.ReadAllText(filePath);
        IDeserializer deserializer = Serializers.CreateDeserializer([cutsceneRegistry]);
        CutsceneSpec[] specs = deserializer.Deserialize<CutsceneSpec[]>(yaml);
        return [..specs.Select(s => s.Create(universe))];
    }
}