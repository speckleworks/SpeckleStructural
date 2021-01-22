using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SpeckleCore;

namespace SpeckleStructuralClasses
{
  public enum StructuralMaterialType
  {
    NotSet,
    Generic,
    Steel,
    Concrete
  }

  public enum Structural1DPropertyShape
  {
    NotSet,
    Generic,
    Circular,
    Rectangular,
    I,
    T
  }

  public enum Structural2DPropertyReferenceSurface
  {
    NotSet,
    Top,
    Middle,
    Bottom,
  }

  public enum StructuralSpringPropertyType
  {
    NotSet,
    General,
    Axial,
    Torsional,
    //Matrix, not supported yet
    Compression,
    Tension,
    //Connector, GWA commands using this doesn't seem to work yet
    Lockup,
    Gap,
    Friction
  }

  [Serializable]
  public partial class StructuralMaterialConcrete : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralMaterialConcrete"; }

    /// <summary>Young's modulus (E) of material.</summary>
    [JsonProperty("youngsModulus", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? YoungsModulus { get; set; }

    /// <summary>Shear modulus (G) of material.</summary>
    [JsonProperty("shearModulus", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? ShearModulus { get; set; }

    /// <summary>Poission's ratio (ν) of material.</summary>
    [JsonProperty("poissonsRatio", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? PoissonsRatio { get; set; }

    /// <summary>Density (ρ) of material.</summary>
    [JsonProperty("density", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Density { get; set; }

    /// <summary>Coefficient of thermal expansion (α) of material.</summary>
    [JsonProperty("coeffThermalExpansion", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? CoeffThermalExpansion { get; set; }

    /// <summary>Compressive strength.</summary>
    [JsonProperty("compressiveStrength", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? CompressiveStrength { get; set; }

    /// <summary>Max strain at failure.</summary>
    [JsonProperty("maxStrain", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? MaxStrain { get; set; }

    /// <summary>Aggragate size.</summary>
    [JsonProperty("aggragateSize", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? AggragateSize { get; set; }
  }

  [Serializable]
  public partial class StructuralMaterialSteel : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralMaterialSteel"; }

    /// <summary>Young's modulus (E) of material.</summary>
    [JsonProperty("youngsModulus", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? YoungsModulus { get; set; }

    /// <summary>Shear modulus (G) of material.</summary>
    [JsonProperty("shearModulus", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? ShearModulus { get; set; }

    /// <summary>Poission's ratio (ν) of material.</summary>
    [JsonProperty("poissonsRatio", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? PoissonsRatio { get; set; }

    /// <summary>Density (ρ) of material.</summary>
    [JsonProperty("density", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Density { get; set; }

    /// <summary>Coefficient of thermal expansion (α) of material.</summary>
    [JsonProperty("coeffThermalExpansion", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? CoeffThermalExpansion { get; set; }

    /// <summary>Yield strength.</summary>
    [JsonProperty("yieldStrength", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? YieldStrength { get; set; }

    /// <summary>Ultimate strength.</summary>
    [JsonProperty("ultimateStrength", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? UltimateStrength { get; set; }

    /// <summary>Max strain at failure.</summary>
    [JsonProperty("maxStrain", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? MaxStrain { get; set; }
  }

  [Serializable]
  public partial class Structural1DProperty : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural1DProperty"; }

    /// <summary>SpecklePolyline or SpeckleCircle of the cross-section.</summary>
    [JsonProperty("profile", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public SpeckleObject Profile { get; set; }

    /// <summary>SpecklePolyline or SpeckleCircle of the cross-section.</summary>
    [JsonProperty("voids", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<SpeckleObject> Voids { get; set; }

    /// <summary>Cross-section shape.</summary>
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("shape", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public Structural1DPropertyShape Shape { get; set; }

    /// <summary>Is the section filled or hollow?</summary>
    [JsonProperty("hollow", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public bool? Hollow { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("thickness", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Thickness { get; set; }

    /// <summary>Catalogue section name which will take precedence over the profile.</summary>
    [JsonProperty("catalogueName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string CatalogueName { get; set; }

    /// <summary>Application ID of StructuralMaterial.</summary>
    [JsonProperty("materialRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string MaterialRef { get; set; }
  }

  [Serializable]
  public partial class Structural1DPropertyExplicit : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural1DPropertyExplicit"; }

    /// <summary>Application ID of StructuralMaterial.</summary>
    [JsonProperty("materialRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string MaterialRef { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("area", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Area { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("iyy", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Iyy { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("izz", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Izz { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("j", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? J { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("ky", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Ky { get; set; }

    /// <summary>Thickness of the section if hollow.</summary>
    [JsonProperty("kz", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Kz { get; set; }

  }

  [Serializable]
  public partial class Structural2DProperty : SpeckleObject, IStructural
  {
    public override string Type { get => "Structural2DProperty"; }

    /// <summary>Thickness of the 2D element.</summary>
    [JsonProperty("thickness", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public double? Thickness { get; set; }

    /// <summary>Application ID of StructuralMaterial.</summary>
    [JsonProperty("materialRef", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public string MaterialRef { get; set; }

    /// <summary>Reference surface for property.</summary>
    /// [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [JsonProperty("referenceSurface", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public Structural2DPropertyReferenceSurface ReferenceSurface { get; set; }
  }

  [Serializable]
  public partial class StructuralSpringProperty : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralSpringProperty"; } 

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

    /// <summary>Application ID of StructuralSpringProperty.</summary>
    [JsonIgnore]
    public double? DampingRatio
    {
      get => StructuralProperties.ContainsKey("dampingRatio") ? (double?)StructuralProperties["dampingRatio"] : null;
      set { if (value != null) StructuralProperties["dampingRatio"] = value; }
    }

    /// <summary>Application ID of StructuralSpringProperty.</summary>
    [JsonIgnore]
    public StructuralSpringPropertyType SpringType
    {
      get => StructuralProperties.ContainsKey("springType")
        ? (StructuralSpringPropertyType)Enum.Parse(typeof(StructuralSpringPropertyType), (StructuralProperties["springType"] as string), true)
        : StructuralSpringPropertyType.NotSet;
      set { if (value != StructuralSpringPropertyType.NotSet) StructuralProperties["springType"] = value.ToString(); }
    }

    /// <summary>Local axis of the spring.</summary>
    [JsonIgnore]
    public StructuralAxis Axis
    {
      get => StructuralProperties.ContainsKey("axis") ? (StructuralProperties["axis"] as StructuralAxis) : null;
      set { if (value != null) StructuralProperties["axis"] = value; }
    }

    /// <summary>X, Y, Z, XX, YY, ZZ stiffnesses.</summary>
    [JsonIgnore]
    public StructuralVectorSix Stiffness
    {
      get => StructuralProperties.ContainsKey("stiffness") ? (StructuralProperties["stiffness"] as StructuralVectorSix) : null;
      set { if (value != null) StructuralProperties["stiffness"] = value; }
    }
  }
}
