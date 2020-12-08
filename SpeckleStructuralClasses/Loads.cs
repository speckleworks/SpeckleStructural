using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleCoreGeometryClasses;

namespace SpeckleStructuralClasses
{
  public enum StructuralLoadCaseType
  {
    NotSet,
    Generic,
    Dead,
    Soil,
    Live,
    Rain,
    Snow,
    Wind,
    Earthquake,
    Thermal
  }

  public enum StructuralLoadTaskType
  {
    NotSet,
    LinearStatic,
    NonlinearStatic,
    Modal,
    Buckling
  }

  public enum StructuralLoadComboType
  {
    NotSet,
    Envelope,
    LinearAdd
  }

  public enum StructuralLoadAxisType
  {
    NotSet,
    Global,
    Local
  }

  [Serializable]
  public partial class StructuralLoadCase : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralLoadCase"; }

    /// <summary>Type of load the case contains.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("caseType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralLoadCaseType CaseType { get; set; }
  }

  [Serializable]
  public partial class StructuralLoadTask : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralLoadTask"; }

    /// <summary>Type of analysis to perform.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("taskType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralLoadTaskType TaskType { get; set; }

    /// <summary>Application IDs of StructuralLoadCase to include.</summary>
    [JsonProperty("loadCaseRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> LoadCaseRefs { get; set; }

    /// <summary>Load factors for each load case.</summary>
    [JsonProperty("loadFactors", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<double> LoadFactors { get; set; }
  }

  [Serializable]
  public partial class StructuralLoadTaskBuckling : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralLoadTaskBuckling"; }

    /// <summary>Type of analysis to perform.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("taskType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralLoadTaskType TaskType { get => StructuralLoadTaskType.Buckling; }

    /// <summary>Number of modes.</summary>
    [JsonProperty("numModes", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? NumModes { get; set; }

    /// <summary>Maximum number of iterations.</summary>
    [JsonProperty("maxNumIterations", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxNumIterations { get; set; }

    /// <summary>Name of the combination case.</summary>
    [JsonProperty("resultCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string ResultCaseRef { get; set; }

    /// <summary>Stage definition for the task</summary>
    [JsonProperty("stageDefinitionRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string StageDefinitionRef { get; set; }
  }

  [Serializable]
  public partial class StructuralLoadCombo : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralLoadCombo"; }

    /// <summary>Type of combination method.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("comboType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralLoadComboType ComboType { get; set; }

    /// <summary>Application IDs of StructuralLoadTask to include.</summary>
    [JsonProperty("loadTaskRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> LoadTaskRefs { get; set; }

    /// <summary>Load factors for each load task.</summary>
    [JsonProperty("loadTaskFactors", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<double> LoadTaskFactors { get; set; }

    /// <summary>Application IDs of StructuralLoadCombo to include.</summary>
    [JsonProperty("loadComboRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> LoadComboRefs { get; set; }

    /// <summary>Load factors for each load combo.</summary>
    [JsonProperty("loadComboFactors", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<double> LoadComboFactors { get; set; }
  }

  [Serializable]
  public partial class StructuralGravityLoading : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralGravityLoading"; }

    /// <summary>A list of x, y, z factors</summary>
    [JsonProperty("gravityFactors", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorThree GravityFactors { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }
  }

  [Serializable]

  public partial class StructuralStorey : StructuralAxis, IStructural
  {
    /// <summary>Base SpecklePoint.</summary>
    [JsonIgnore]
    public StructuralAxis Axis
    {
      get => this as StructuralAxis;
      set
      {
        this.Xdir = value.Xdir;
        this.Ydir = value.Ydir;
        this.Origin = value.Origin;
        this.Normal = value.Normal;
      }
    }

    /// <summary>Tolerance for element inclusion.</summary>
    [JsonProperty("elevation", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Elevation { get; set; }

    /// <summary>Tolerance for element inclusion.</summary>
    [JsonProperty("toleranceAbove", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? ToleranceAbove { get; set; }

    /// <summary>Tolerance for element inclusion.</summary>
    [JsonProperty("toleranceBelow", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? ToleranceBelow { get; set; }
  }

  [Serializable]

  public partial class StructuralLoadPlane : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralLoadPlane"; }

    [JsonIgnore]
    private Dictionary<string, object> StructuralProperties
    {
      get
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        if (!base.Properties.ContainsKey("structural"))
          base.Properties["structural"] = new Dictionary<string, object>();

        return base.Properties["structural"] as Dictionary<string, object>;

      }
      set
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        base.Properties["structural"] = value;
      }

    }


    /// <summary>Type elements to apply load to (1D or 2D).</summary>
    [JsonProperty("elementDimension", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? ElementDimension { get; set; }

    /// <summary>Tolerance for element inclusion.</summary>
    [JsonProperty("tolerance", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Tolerance { get; set; }

    /// <summary>Span option.</summary>
    [JsonProperty("span", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? Span { get; set; }

    /// <summary>Span option.</summary>
    [JsonProperty("spanAngle", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? SpanAngle { get; set; }

    /// <summary>Application IDs of StructuralStoreys (planes) to apply load.</summary>
    [JsonProperty("storeyRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string StoreyRef { get; set; }

    /// <summary>Local axis of the node.</summary>
    [JsonIgnore]
    public StructuralAxis Axis
    {
      get => StructuralProperties.ContainsKey("axis") ? (StructuralProperties["axis"] as StructuralAxis) : null;
      set { if (value != null) StructuralProperties["axis"] = value; }
    }
  }

  [Serializable]
  public partial class Structural0DLoad : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural0DLoad"; }

    /// <summary>A list of Fx, Fy, Fz, Mx, My, and Mz loads.</summary>
    [JsonProperty("loading", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorSix Loading { get; set; }

    /// <summary>Application IDs of StructuralNodes to apply load.</summary>
    [JsonProperty("nodeRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NodeRefs { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }
  }

  [Serializable]
  public partial class Structural0DLoadPoint : SpecklePoint, IStructural
  {
    public override string Type { get => "Structural0DLoadPoint"; }

    /// <summary>Base SpecklePoint.</summary>
    [JsonIgnore]
    public SpecklePoint LoadPoint
    {
      get => this as SpecklePoint;
      set
      {
        this.Value = value.Value;
      }
    }

    /// <summary>Plane of load.</summary>
    [JsonProperty("loadPlaneRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadPlaneRef { get; set; }
    
    [JsonProperty("loading", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorSix Loading { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }
  }
  [Serializable]

  public partial class Structural1DLoad : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural1DLoad"; }

    /// <summary>A list of Fx, Fy, Fz, Mx, My, and Mz loads.</summary>
    [JsonProperty("loading", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorSix Loading { get; set; }

    /// <summary>Application IDs of Structural1DElements to apply load.</summary>
    [JsonProperty("elementRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ElementRefs { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }
  }

  [Serializable]
  public partial class Structural1DLoadLine : SpeckleLine, IStructural
  {
    public override string Type { get { var speckleType = "/" + this.GetType().Name; return base.Type.Replace(speckleType, "") + speckleType; } } //The replacement is to avoid a peculiarity with merging using Automapper

    [JsonIgnore]
    private Dictionary<string, object> StructuralProperties
    {
      get
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        if (!base.Properties.ContainsKey("structural"))
          base.Properties["structural"] = new Dictionary<string, object>();

        return base.Properties["structural"] as Dictionary<string, object>;

      }
      set
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        base.Properties["structural"] = value;
      }
    }

    /// <summary>Base SpeckleLine.</summary>
    [JsonIgnore]
    public SpeckleLine baseLine
    {
      get => this as SpeckleLine;
      set
      {
        this.Value = value.Value;
        this.Domain = value.Domain;
      }
    }

    /// <summary>Plane of load.</summary>
    [JsonProperty("LoadPlaneRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadPlaneRef { get; set; }

    /// <summary>A list of Fx, Fy, Fz, loads.</summary>
    [JsonIgnore]
    public StructuralVectorSix Loading
    {
      get => StructuralProperties.ContainsKey("loading") ? (StructuralProperties["loading"] as StructuralVectorSix) : null;
      set { if (value != null) StructuralProperties["loading"] = value; }
    }

    /// <summary>End loading if load varies at end point. A list of Fx, Fy, Fz, Mx, My, and Mz loads.</summary>
    [JsonIgnore]
    public StructuralVectorSix LoadingEnd
    {
      get => StructuralProperties.ContainsKey("loadingEnd") ? (StructuralProperties["loadingEnd"] as StructuralVectorSix) : null;
      set { if (value != null) StructuralProperties["loadingEnd"] = value; }
    }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonIgnore]
    public string LoadCaseRef
    {
      get => StructuralProperties.ContainsKey("loadCaseRef") ? (StructuralProperties["loadCaseRef"] as string) : null;
      set { if (value != null) StructuralProperties["loadCaseRef"] = value; }
    }
  }

  [Serializable]
  public partial class Structural2DLoad : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural2DLoad"; }

    /// <summary>A list of Fx, Fy, and Fz loads.</summary>
    [JsonProperty("loading", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorThree Loading { get; set; }

    /// <summary>Application IDs of Structural2DElementMeshes to apply load.</summary>
    [JsonProperty("elementRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ElementRefs { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("axisType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralLoadAxisType AxisType { get; set; }
  }

  [Serializable]
  public partial class Structural2DLoadPanel : SpecklePolyline, IStructural
  {
    public override string Type { get { var speckleType = "/" + this.GetType().Name; return base.Type.Replace(speckleType, "") + speckleType; } } //The replacement is to avoid a peculiarity with merging using Automapper

    [JsonIgnore]
    private Dictionary<string, object> StructuralProperties
    {
      get
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        if (!base.Properties.ContainsKey("structural"))
          base.Properties["structural"] = new Dictionary<string, object>();

        return base.Properties["structural"] as Dictionary<string, object>;

      }
      set
      {
        if (base.Properties == null)
          base.Properties = new Dictionary<string, object>();

        base.Properties["structural"] = value;
      }
    }

    /// <summary>Base SpecklePolyline.</summary>
    [JsonIgnore]
    public SpecklePolyline basePolyline
    {
      get => this as SpecklePolyline;
      set
      {
        this.Value = value.Value;
        this.Closed = value.Closed;
        this.Domain = value.Domain;
      }
    }

    /// <summary>A list of Fx, Fy, and Fz loads.</summary>
    [JsonIgnore]
    public StructuralVectorThree Loading
    {
      get => StructuralProperties.ContainsKey("loading") ? (StructuralProperties["loading"] as StructuralVectorThree) : null;
      set { if (value != null) StructuralProperties["loading"] = value; }
    }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonIgnore]
    public string LoadCaseRef
    {
      get => StructuralProperties.ContainsKey("loadCaseRef") ? (StructuralProperties["loadCaseRef"] as string) : null;
      set { if (value != null) StructuralProperties["loadCaseRef"] = value; }
    }

    [JsonIgnore]
    public string LoadPlaneRef
    {
      get => StructuralProperties.ContainsKey("loadPlaneRef") ? (StructuralProperties["loadPlaneRef"] as string) : null;
      set { if (value != null) StructuralProperties["loadPlaneRef"] = value; }
    }
  }

  [Serializable]
  public partial class Structural2DThermalLoad : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural2DThermalLoad"; }

    /// <summary>Temperature at the top surface of the element.</summary>
    [JsonProperty("topTemperature", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? TopTemperature { get; set; }

    /// <summary>Temperature at the bottom surface of the element.</summary>
    [JsonProperty("bottomTemperature", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? BottomTemperature { get; set; }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonProperty("loadCaseRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string LoadCaseRef { get; set; }

    /// <summary>Application IDs of Structural2DElements to apply load.</summary>
    [JsonProperty("elementRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ElementRefs { get; set; }
  }

}
