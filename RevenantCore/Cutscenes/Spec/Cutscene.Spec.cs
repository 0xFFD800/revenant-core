using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevenantCore.Scenes;
using RevenantCore.Util;

namespace RevenantCore.Cutscenes.Spec;

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
/// The spec for a parameter, which allows reuse of a cutscene file.
/// </summary>
public class ParameterSpec
{
    /// <summary>
    /// The name of the parameter, by which it will be identified by load-type cutscenes.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The default value for this parameter. Will be used if the loading cutscene does not specify one.
    /// </summary>
    public string Default { get; set; } = "";
}

/// <summary>
/// The spec for a cutscene file, containing a reusable cutscene for use in other files.
/// </summary>
public class CutsceneFileSpec
{
    /// <summary>
    /// The specs for parameters, which allow values in the cutscene to differ based on provided values. 
    /// </summary>
    public ParameterSpec[] Parameters { get; set; } = [];

    /// <summary>
    /// The literal string YAML for the cutscene this file contains. May contain values which need to be substituted by parameters.
    /// </summary>
    public string Cutscene { get; set; } = "";
}

/// <summary>
/// The spec for a "load" cutscene, which creates a cutscene from a file given a list of parameters.
/// </summary>
public class LoadCutsceneSpec : CutsceneSpec
{
    /// <summary>
    /// The name of the file with the cutscene to load.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// The values to give to the parameters in the loaded cutscene file.
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];

    /// <summary>
    /// Replaces parameter references in the provided cutscene string with the provided values.
    /// </summary>
    /// <param name="paramValues">The populated dictionary of parameter values with which to replace parameter references.</param>
    /// <param name="cutscene">The full literal cutscene string of the loaded cutscene file.</param>
    /// <returns>The cutscene string with parameter references replaced with their corresponding values.</returns>
    private static string InsertParameters(Dictionary<string, string> paramValues, string cutscene)
    {
        string cutsceneYaml = cutscene;
        int scan = 0;
        while (scan < cutsceneYaml.Length && (scan = cutsceneYaml.IndexOf("${", scan)) >= 0)
        {
            int keyStart = scan + 2;
            int end = cutsceneYaml.IndexOf('}', keyStart);

            // If the bracket is unmatched, there can be no more parameter references in the file.
            if (end < 0) break;
            string key = cutsceneYaml[keyStart..end];

            // If there is no "parameter" object for this parameter, do not replace it.
            if (!paramValues.TryGetValue(key, out string? value))
            {
                scan = end + 1;
                continue;
            }
            cutsceneYaml = cutsceneYaml.Remove(scan, key.Length + 3);
            cutsceneYaml = cutsceneYaml.Insert(scan, value);
            scan += value.Length + 1;
        }
        return cutsceneYaml;
    }

    public override Cutscene Create(Universe universe)
    {
        string yaml = File.ReadAllText(FileName);
        CutsceneFileSpec fileSpec = Serializers.CreateDeserializer([]).Deserialize<CutsceneFileSpec>(yaml);
        Dictionary<string, string> paramValues = fileSpec.Parameters
            .Select(p => new KeyValuePair<string, string>(p.Name, p.Default)).ToDictionary();
        foreach (KeyValuePair<string, string> param in Parameters)
            paramValues[param.Key] = param.Value;
        return universe.Core.LoadCutscene(universe, InsertParameters(paramValues, fileSpec.Cutscene));
    }
}