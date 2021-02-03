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
  [GSAObject("MAT_STEEL.3", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSAMaterialSteel : GSABase<StructuralMaterialSteel>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralMaterialSteel();

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

      // Failure strain is found before MAT_CURVE_PARAM.2
      var strainIndex = Array.FindIndex(pieces, x => x.StartsWith("MAT_CURVE_PARAM"));
      if (strainIndex > 0)
        obj.MaxStrain = Convert.ToDouble(pieces[strainIndex - 1]);

      // Skip to last fourth to last
      counter = pieces.Count() - 4;
      obj.YieldStrength = Convert.ToDouble(pieces[counter++]);
      obj.UltimateStrength = Convert.ToDouble(pieces[counter++]);

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

      var mat = this.Value as StructuralMaterialSteel;
      if (mat.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSAMaterialSteel).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSAMaterialSteel).GetGSAKeyword(), mat.ApplicationId);

      // TODO: This function barely works.
      var ls = new List<string>
      {
        "SET",
        "MAT_STEEL.3" + ":" + Helper.GenerateSID(mat),
        index.ToString(),
        "MAT.10",
        mat.Name == null || mat.Name == "" ? " " : mat.Name,
        mat.YoungsModulus.ToString(), // E
        mat.YieldStrength.ToString(), // f (fy for steel)
        mat.PoissonsRatio.ToString(), // nu
        mat.ShearModulus.ToString(), // G
        mat.Density.ToString(), // rho
        mat.CoeffThermalExpansion.ToString(), // alpha
        "MAT_ANAL.1",
        "Steel",
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
        mat.MaxStrain.ToString(), // Ultimate strain
        "MAT_CURVE_PARAM.3",
        "",
        "ELAS_PLAS", // GSA default model for steel - ULS
        "0", // strain[6]
        "0",
        "0",
        "0",
        "0.05",
        "0.05",
        "1", // Material factor on strength
        "1", // Material factor on elastic modulus
        "MAT_CURVE_PARAM.3",
        "",
        "ELAS_PLAS",
        "0",
        "0",
        "0",
        "0",
        "0.05",
        "0.05",
        "1", // Material factor on strength
        "1", // Material factor on elastic modulus
        "0", // Cost
        "Steel",
        mat.YieldStrength.ToString(), // Yield strength
        mat.UltimateStrength.ToString(), // Ultimate strength
        "0", // Perfectly plastic strain limit
        "0" // Hardening modulus
      };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralMaterialSteel mat)
    {
      return new GSAMaterialSteel() { Value = mat }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAMaterialSteel dummyObject)
    {
      var newLines = ToSpeckleBase<GSAMaterialSteel>();
      var typeName = dummyObject.GetType().Name;
      var materialsLock = new object();
      var materials = new SortedDictionary<int, GSAMaterialSteel>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        try
        {
          var mat = new GSAMaterialSteel() { GWACommand = newLines[k] };
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

      return (materials.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
