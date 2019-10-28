using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("MAT_CONCRETE.16", new string[] { }, "properties", true, true, new Type[] { }, new Type[] { })]
  public class GSAMaterialConcrete : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralMaterialConcrete();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      StructuralMaterialConcrete obj = new StructuralMaterialConcrete();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Indexer.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      counter++; // MAT.8
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Unlocked
      obj.YoungsModulus = Convert.ToDouble(pieces[counter++]);
      obj.PoissonsRatio = Convert.ToDouble(pieces[counter++]);
      obj.ShearModulus = Convert.ToDouble(pieces[counter++]);
      obj.Density = Convert.ToDouble(pieces[counter++]);
      obj.CoeffThermalExpansion = Convert.ToDouble(pieces[counter++]);

      // Skip to last 27th to last
      counter = pieces.Count() - 27;
      obj.CompressiveStrength = Convert.ToDouble(pieces[counter++]);

      // Skip to last 15th to last
      counter = pieces.Count() - 15;
      obj.MaxStrain = Convert.ToDouble(pieces[counter++]);

      // Skip to last 10th to last
      counter = pieces.Count() - 10;
      obj.AggragateSize = Convert.ToDouble(pieces[counter++]);

      this.Value = obj;
    }

    public void SetGWACommand()
    {
      if (this.Value == null)
        return;

      StructuralMaterialConcrete mat = this.Value as StructuralMaterialConcrete;

      string keyword = typeof(GSAMaterialConcrete).GetGSAKeyword();

      int index = Initialiser.Indexer.ResolveIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), typeof(GSAMaterialConcrete).Name, mat.ApplicationId);

      // TODO: This function barely works.
      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add("MAT_CONCRETE.16" + ":" + HelperClass.GenerateSID(mat));
      ls.Add(index.ToString());
      ls.Add("MAT.8");
      ls.Add(mat.Name == null || mat.Name == "" ? " " : mat.Name);
      ls.Add("YES"); // Unlocked
      ls.Add(mat.YoungsModulus.ToString()); // E
      ls.Add(mat.PoissonsRatio.ToString()); // nu
      ls.Add(mat.ShearModulus.ToString()); // G
      ls.Add(mat.Density.ToString()); // rho
      ls.Add(mat.CoeffThermalExpansion.ToString()); // alpha
      ls.Add("MAT_ANAL.1");
      ls.Add("0"); // TODO: What is this?
      ls.Add("Concrete");
      ls.Add("-268435456"); // TODO: What is this?
      ls.Add("MAT_ELAS_ISO");
      ls.Add("6"); // TODO: What is this?
      ls.Add(mat.YoungsModulus.ToString()); // E
      ls.Add(mat.PoissonsRatio.ToString()); // nu
      ls.Add(mat.Density.ToString()); // rho
      ls.Add(mat.CoeffThermalExpansion.ToString()); // alpha
      ls.Add(mat.ShearModulus.ToString()); // G
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // Ultimate strain
      ls.Add("MAT_CURVE_PARAM.2");
      ls.Add("");
      ls.Add("UNDEF");
      ls.Add("1"); // Material factor on strength
      ls.Add("1"); // Material factor on elastic modulus
      ls.Add("MAT_CURVE_PARAM.2");
      ls.Add("");
      ls.Add("UNDEF");
      ls.Add("1"); // Material factor on strength
      ls.Add("1"); // Material factor on elastic modulus
      ls.Add("0"); // Cost
      ls.Add("CYLINDER"); // Strength type
      ls.Add("N"); // Cement class
      ls.Add(mat.CompressiveStrength.ToString()); // Concrete strength
      ls.Add("0"); //ls.Add("27912500"); // Uncracked strength
      ls.Add("0"); //ls.Add("17500000"); // Cracked strength
      ls.Add("0"); //ls.Add("2366431"); // Tensile strength
      ls.Add("0"); //ls.Add("2366431"); // Peak strength for curves
      ls.Add("0"); // TODO: What is this?
      ls.Add("1"); // Ratio of initial elastic modulus to secant modulus
      ls.Add("2"); // Parabolic coefficient
      ls.Add("1"); // Modifier on elastic stiffness
      ls.Add("0.00218389285990043"); // SLS strain at peak stress
      ls.Add("0.0035"); // SLS max strain
      ls.Add("0.00041125"); // ULS strain at plateau stress
      ls.Add(mat.MaxStrain.ToString()); // ULS max compressive strain
      ls.Add("0.0035"); // TODO: What is this?
      ls.Add("0.002"); // Plateau strain
      ls.Add("0.0035"); // Max axial strain
      ls.Add("NO"); // Lightweight?
      ls.Add(mat.AggragateSize.ToString()); // Aggragate size
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("1"); // TODO: What is this?
      ls.Add("0.8825"); // Constant stress depth
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?
      ls.Add("0"); // TODO: What is this?

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralMaterialConcrete mat)
    {
      new GSAMaterialConcrete() { Value = mat }.SetGWACommand();

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSAMaterialConcrete dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSAMaterialConcrete)))
        Initialiser.GSASenderObjects[typeof(GSAMaterialConcrete)] = new List<object>();

      List<GSAMaterialConcrete> materials = new List<GSAMaterialConcrete>();

      string keyword = typeof(GSAMaterialConcrete).GetGSAKeyword();
      string[] subKeywords = typeof(GSAMaterialConcrete).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSAMaterialConcrete)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSAMaterialConcrete)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          GSAMaterialConcrete mat = new GSAMaterialConcrete() { GWACommand = p };
          mat.ParseGWACommand(Initialiser.Interface);
          materials.Add(mat);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSAMaterialConcrete)].AddRange(materials);

      if (materials.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
