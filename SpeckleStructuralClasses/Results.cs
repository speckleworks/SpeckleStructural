using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SpeckleCore;

namespace SpeckleStructuralClasses
{
  [Serializable]
  public abstract class StructuralResultBase : SpeckleObject
  {
    /// <summary>(optional)Description of result.</summary>
    [JsonProperty("description", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string Description { get; set; }

    /// <summary>ApplicationID of object referred to.</summary>
    [JsonProperty("targetRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string TargetRef { get; set; }

    /// <summary>(optional)Load case of the results.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }

    /// <summary>(optional)Indicates whether the results are in the global or local axis.</summary>
    [JsonProperty("isGlobal", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public bool IsGlobal { get; set; }

    /// <summary>(optional)String indicating source of result.</summary>
    [JsonProperty("resultSource", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string ResultSource { get; set; }
  }

  [Serializable]
  public partial class StructuralNodeResult : StructuralResultBase, IStructural
  {
    public override string Type { get => "StructuralNodeResult"; }

    /// <summary>Results.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    //[JsonConverter(typeof(SpecklePropertiesConverter))]
    public Dictionary<string, object> Value { get; set; }
  }

  [Serializable]
  public partial class Structural1DElementResult : StructuralResultBase, IStructural
  {
    public override string Type { get => "Structural1DElementResult"; }

    /// <summary>Results.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    //[JsonConverter(typeof(SpecklePropertiesConverter))]
    public Dictionary<string, object> Value { get; set; }
  }

  [Serializable]
  public partial class Structural2DElementResult : StructuralResultBase, IStructural
  {
    public override string Type { get => "Structural2DElementResult"; }

    /// <summary>Results.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(SpecklePropertiesConverter))]
    public Dictionary<string, object> Value { get; set; }
  }

  [Serializable]
  public partial class StructuralMiscResult : StructuralResultBase, IStructural
  {
    public override string Type { get => "StructuralMiscResult"; }

    /// <summary>Results.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    //[JsonConverter(typeof(SpecklePropertiesConverter))]
    public Dictionary<string, object> Value { get; set; }
  }
}
