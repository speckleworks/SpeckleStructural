using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using Newtonsoft.Json;

namespace SpeckleStructuralClasses
{
  public interface IStructural : ISpeckleInitializer { }
  
  [Serializable]
  public partial class StructuralVectorThree : SpeckleVector, IStructural
  {
    public override string Type { get => base.Type + "/StructuralVectorThree"; }

    /// <summary>Base SpeckleVector.</summary>
    [JsonIgnore]
    public SpeckleVector baseVector
    {
      get => this as SpeckleVector;
      set => this.Value = value.Value;
    }
  }

  [Serializable]
  public partial class StructuralVectorBoolThree : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralVectorBoolThree"; }

    /// <summary>An array containing the X, Y, and Z values of the vector.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<bool> Value { get; set; }
  }

  [Serializable]
  public partial class StructuralVectorSix : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralVectorSix"; }

    /// <summary>An array containing the X, Y, Z, XX, YY, and ZZ values of the vector.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<double> Value { get; set; }
  }

  [Serializable]
  public partial class StructuralVectorBoolSix : SpeckleObject, IStructural
  {
    public override string Type { get => "StructuralVectorBoolSix"; }

    /// <summary>An array containing the X, Y, Z, XX, YY, and ZZ values of the vector.</summary>
    [JsonProperty("value", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
    public List<bool> Value { get; set; }
  }

  [Serializable]
  public partial class StructuralAxis : SpecklePlane, IStructural
  {
    public override string Type { get => base.Type + "/StructuralAxis"; }

    /// <summary>Base SpecklePlane.</summary>
    [JsonIgnore]
    public SpecklePlane basePlane
    {
      get => this as SpecklePlane;
      set
      {
        this.Origin = value.Origin;
        this.Normal = value.Normal;
        this.Xdir = value.Xdir;
        this.Ydir = value.Ydir;
      }
    }
  }
}
