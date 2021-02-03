using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("MAT_CONCRETE.17", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSAMaterialConcrete : GSABase<StructuralMaterialConcrete>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralMaterialConcrete();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      counter++; // MAT.10
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.YoungsModulus = Convert.ToDouble(pieces[counter++]);
      counter++; // strength
      obj.PoissonsRatio = Convert.ToDouble(pieces[counter++]);
      obj.ShearModulus = Convert.ToDouble(pieces[counter++]);
      obj.Density = Convert.ToDouble(pieces[counter++]);
      obj.CoeffThermalExpansion = Convert.ToDouble(pieces[counter++]);

      obj.CompressiveStrength = Convert.ToDouble(pieces[54]);

      obj.MaxStrain = Convert.ToDouble(pieces[65]);

      obj.AggragateSize = Convert.ToDouble(pieces[70]);

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var mat = this.Value as StructuralMaterialConcrete;
      if (mat.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSAMaterialConcrete).GetGSAKeyword();
      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), mat.ApplicationId);

      // TODO: This function barely works.
      var ls = new List<string>
      {
        "SET",
        "MAT_CONCRETE.17" + ":" + Helper.GenerateSID(mat),
        index.ToString(),
        "MAT.10",
        mat.Name == null || mat.Name == "" ? " " : mat.Name,
        (mat.YoungsModulus*1000).ToString(), // E
        mat.CompressiveStrength.ToString(), // Design Strength (Pa) <-- need to check units here
        mat.PoissonsRatio.ToString(), // nu
        mat.ShearModulus.ToString(), // G
        mat.Density.ToString(), // rho
        mat.CoeffThermalExpansion.ToString(), // alpha
        "MAT_ANAL.1",
        "Concrete",
        "-268435456", // TODO: What is this?
        "MAT_ELAS_ISO",
        "6", // TODO: What is this?
        mat.YoungsModulus.ToString(), // E
        mat.PoissonsRatio.ToString(), // nu
        mat.Density.ToString(), // rho
        mat.CoeffThermalExpansion.ToString(), // alpha
        mat.ShearModulus.ToString(), // G
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // Ultimate strain
        "MAT_CURVE_PARAM.3", // ULS - this is GSA default for concrete
        "",
        "RECT_PARABOLA+NO_TENSION", // material model
        "0", // strain[6]
        "0",
        "0",
        "0",
        "0.0035",
        "1",
        "1.5", // Material factor on strength
        "1", // Material factor on elastic modulus
        "MAT_CURVE_PARAM.3", // SLS - this is the GSA default for concrete
        "",
        "FIB_SCHEMATIC+INTERPOLATED",
        "0", // strain[6]
        "0",
        "0",
        "0",
        "0",
        "0",
        "1", // Material factor on strength
        "1", // Material factor on elastic modulus
        "0", // Cost
        "Concrete",
        "CYLINDER", // Strength type
        "N", // Cement class
        mat.CompressiveStrength.ToString(), // Concrete strength
        "0", //ls.Add("27912500"); // Uncracked strength
        "0", //ls.Add("17500000"); // Cracked strength
        "0", //ls.Add("2366431"); // Tensile strength
        "0", //ls.Add("2366431"); // Peak strength for curves
        "0", // TODO: What is this?
        "1", // Ratio of initial elastic modulus to secant modulus
        "2", // Parabolic coefficient
        "0.00218389285990043", // SLS strain at peak stress
        "0.0035", // SLS max strain
        "0.00041125", // ULS strain at plateau stress
        mat.MaxStrain.ToString(), // ULS max compressive strain
        "0.0035", // TODO: What is this?
        "0.002", // Plateau strain
        "0.0035", // Max axial strain
        "NO", // Lightweight?
        mat.AggragateSize.ToString(), // Aggragate size
        "0", // TODO: What is this?
        "1", // TODO: What is this?
        "0.8825", // Constant stress depth
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0", // TODO: What is this?
        "0" // TODO: What is this?
      };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralMaterialConcrete mat)
    {
      return new GSAMaterialConcrete() { Value = mat }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAMaterialConcrete dummyObject)
    {
      var newLines = ToSpeckleBase<GSAMaterialConcrete>();
      var typeName = dummyObject.GetType().Name;
      var materialsLock = new object();
      var materials = new SortedDictionary<int, GSAMaterialConcrete>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        try
        {
          var mat = new GSAMaterialConcrete() { GWACommand = newLines[k] };
          mat.ParseGWACommand();
          lock (materialsLock)
          {
            materials.Add(k, mat);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, gsaId);
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, gsaId);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(materials.Values.ToList());

      return (materials.Keys.Count > 0 ) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
