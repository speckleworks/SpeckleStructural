using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleCoreGeometryClasses;

namespace SpeckleStructuralClasses
{
  public enum StructuralInfluenceEffectType
  {
    Displacement,
    Force,
  }

  public enum StructuralBridgeCurvature
  {
    NotSet,
    Straight,
    LeftCurve,
    RightCurve
  }

  public enum StructuralBridgePathType
  {
    NotSet,
    Lane,
    Footway,
    Track,
    Vehicle,
    Carriage1Way,
    Carriage2Way
  }

  [Serializable]
  public partial class StructuralAssembly : SpeckleLine, IStructural
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

    [JsonIgnore]
    public int? NumPoints
    {
      get
      {
        if (StructuralProperties.ContainsKey("numPoints") && int.TryParse(StructuralProperties["numPoints"].ToString(), out var numPoints))
        {
          return numPoints;
        }
        return null;
      }
      set { if (value != null) StructuralProperties["numPoints"] = value; }
    }

    [JsonIgnore]
    public List<double> PointDistances
    {
      get
      {
        return StructuralProperties.ValueAsDoubleList("pointdistances");
      }
      set { if (value != null && value.Count() > 0) StructuralProperties["pointdistances"] = value; }
    }

    [JsonIgnore]
    public List<string> StoreyRefs
    {
      get
      {
        return StructuralProperties.ValueAsTypedList<string>("storeyRefs");
      }
      set { if (value != null && value.Count() > 0) StructuralProperties["storeyRefs"] = value; }
    }

    /// <summary>Base SpeckleLine.</summary>
    [JsonIgnore]
    public SpeckleLine BaseLine
    {
      get => this as SpeckleLine;
      set
      {
        this.Value = value.Value;
        this.Domain = value.Domain;
      }
    }

    [JsonIgnore]
    public SpecklePoint OrientationPoint
    {
      get => StructuralProperties.ContainsKey("orientationPoint") ? (StructuralProperties["orientationPoint"] as SpecklePoint) : null;
      set { if (value != null) StructuralProperties["orientationPoint"] = value; }
    }

    [JsonIgnore]
    public double? Width
    {
      get => (StructuralProperties.ContainsKey("width") && double.TryParse(StructuralProperties["width"].ToString(), out var width)) ? (double?) width : null;
      set { if (value != null) StructuralProperties["width"] = value; }
    }

    /// <summary>Application ID of StructuralLoadCase.</summary>
    [JsonIgnore]
    public List<string> ElementRefs
    {
      get
      {
        return StructuralProperties.ValueAsTypedList<string>("elementRefs");
      }
      set { if (value != null && value.Count() > 0) StructuralProperties["elementRefs"] = value; }
    }
  }

  [Serializable]
  public partial class StructuralConstructionStage : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralConstructionStage"; }

    /// <summary>Application ID of members to include in the stage of the construction sequence.</summary>
    [JsonProperty("elementRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ElementRefs { get; set; }

    /// <summary>Number of days in the stage</summary>
    [JsonProperty("stageDays", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? StageDays { get; set; }
  }

  [Serializable]
  public partial class StructuralStagedNodalRestraints : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralStagedNodalRestraints"; }

    /// <summary>A list of the X, Y, Z, Rx, Ry, and Rz restraints.</summary>
    [JsonProperty("restraint", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorBoolSix Restraint { get; set; }

    /// <summary>Application IDs of StructuralNodes to apply restrain.</summary>
    [JsonProperty("nodeRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NodeRefs { get; set; }

    /// <summary>Application IDs of StructuralConstructionStages to apply restraints on</summary>
    [JsonProperty("constructionStageRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ConstructionStageRefs { get; set; }
  }

  [Serializable]
  public partial class StructuralBridgeAlignment : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralBridgeAlignment"; }

    [JsonProperty("plane", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public SpecklePlane Plane { get; set; }

    /// <summary>List nodes on the alignment.</summary>
    [JsonIgnore]
    public List<StructuralBridgeAlignmentNode> Nodes
    {
      get
      {
        return StructuralProperties.ValueAsTypedList<StructuralBridgeAlignmentNode>("nodes");
      }
      set { if (value != null && value.Count() > 0) StructuralProperties["nodes"] = value; }
    }

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
  }

  [Serializable]
  public partial class StructuralBridgeAlignmentNode : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralBridgeAlignmentNode"; }

    [JsonProperty("chainage", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Chainage { get; set; }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("curvature", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralBridgeCurvature Curvature { get; set; }

    [JsonProperty("radius", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Radius { get; set; }
  }

  [Serializable]
  public partial class StructuralBridgePath : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralBridgePath"; }

    [JsonProperty("alignmentRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string AlignmentRef { get; set; }

    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("pathType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralBridgePathType PathType { get; set; }

    [JsonProperty("offsets", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<double> Offsets { get; set; }

    [JsonProperty("gauge", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Gauge { get; set; }

    [JsonProperty("leftRailFactor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? LeftRailFactor { get; set; }
  }

  [Serializable]
  public partial class StructuralBridgeVehicle : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralBridgeVehicle"; }

    [JsonProperty("width", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Width { get; set; }

    [JsonIgnore]
    public List<StructuralBridgeVehicleAxle> Axles
    {
      get
      {
        return StructuralProperties.ValueAsTypedList<StructuralBridgeVehicleAxle>("axles");
      }
      set { if (value != null && value.Count() > 0) StructuralProperties["axles"] = value; }
    }

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
  }

  [Serializable]
  public partial class StructuralBridgeVehicleAxle : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralBridgeVehicleAxle"; }

    [JsonProperty("position", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Position { get; set; }

    [JsonProperty("wheelOffset", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? WheelOffset { get; set; }

    [JsonProperty("leftWheelLoad", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? LeftWheelLoad { get; set; }

    [JsonProperty("rightWheelLoad", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? RightWheelLoad { get; set; }
  }

  [Serializable]
  public partial class StructuralRigidConstraints : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralRigidConstraints"; }

    /// <summary>A list of the X, Y, Z, Rx, Ry, and Rz constraints.</summary>
    [JsonProperty("constraints", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorBoolSix Constraint { get; set; }

    /// <summary>Application IDs of StructuralNodes to apply constraints.</summary>
    [JsonProperty("nodeRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> NodeRefs { get; set; }

    /// <summary>Application ID of master StructuralNodes which all other nodes are tied to.</summary>
    [JsonProperty("masterNodeRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string MasterNodeRef { get; set; }

    /// <summary>Application IDs of StructuralConstructionStages to apply restraints on</summary>
    [JsonProperty("constructionStageRefs", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ConstructionStageRefs { get; set; }
  }

  [Serializable]
  public partial class StructuralNodalInfluenceEffect : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralNodalInfluenceEffect"; }

    /// <summary>Value to calculate influence effect for.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("effectType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralInfluenceEffectType EffectType { get; set; }

    /// <summary>Application ID of StructuralNodes to calculate influence effect at.</summary>
    [JsonProperty("nodeRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string NodeRef { get; set; }

    /// <summary>Influence effect factor to apply.</summary>
    [JsonProperty("factor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Factor { get; set; }

    /// <summary>Axis of effect to be considered.</summary>
    [JsonProperty("axis", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralAxis Axis { get; set; }

    /// <summary>Directions of effect to be considered.</summary>
    [JsonProperty("directions", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorBoolSix Directions { get; set; }

    /// <summary>GSA grouping of influence effects to combine effects.</summary>
    [JsonProperty("gsaEffectGroup", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? GSAEffectGroup { get; set; }
  }

  [Serializable]
  public partial class Structural1DInfluenceEffect : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural1DInfluenceEffect"; }

    /// <summary>Value to calculate influence effect for.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("effectType", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralInfluenceEffectType EffectType { get; set; }

    /// <summary>Application ID of Structural1DElement to calculate influence effect at.</summary>
    [JsonProperty("elementRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string ElementRef { get; set; }

    /// <summary>Position on the element in percentage (0 to 1) to calculate influence effect at.</summary>
    [JsonProperty("position", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Position { get; set; }

    /// <summary>Influence effect factor to apply.</summary>
    [JsonProperty("factor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Factor { get; set; }

    /// <summary>Directions of effect to be considered.</summary>
    [JsonProperty("directions", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public StructuralVectorBoolSix Directions { get; set; }
    
    /// <summary>GSA grouping of influence effects to combine effects.</summary>
    [JsonProperty("gsaEffectGroup", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public int? GSAEffectGroup { get; set; }
  }

  [Serializable]
  public partial class StructuralReferenceLine : SpeckleLine, IStructural
  {
    public override string Type { get => "StructuralReferenceLine"; }

    [JsonIgnore]
    public string Group
    {
      get => StructuralProperties.ContainsKey("group") ? (StructuralProperties["group"] as string) : null;
      set { if (value != null) StructuralProperties["group"] = value; }
    }

    [JsonIgnore]
    private Dictionary<string, object> StructuralProperties
    {
      get
      {
        if (base.Properties == null)
        {
          base.Properties = new Dictionary<string, object>();
        }

        if (!base.Properties.ContainsKey("structural"))
        {
          base.Properties["structural"] = new Dictionary<string, object>();
        }

        return base.Properties["structural"] as Dictionary<string, object>;
      }
      set
      {
        if (base.Properties == null)
        {
          base.Properties = new Dictionary<string, object>();
        }

        base.Properties["structural"] = value;
      }
    }
  }
}
