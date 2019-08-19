using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_2D.5", new string[] { "MAT_STEEL.3", "MAT_CONCRETE.16" }, "properties", true, true, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) }, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) })]
  public class GSA2DProperty : IGSASpeckleContainer
  {
    public bool IsAxisLocal;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DProperty();

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSAMaterialSteel> steels, List<GSAMaterialConcrete> concretes)
    {
      if (this.GWACommand == null)
        return;

      Structural2DProperty obj = new Structural2DProperty();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
      counter++; // Type
      this.IsAxisLocal = pieces[counter++] == "LOCAL"; // Axis
      counter++; // Analysis material
      string materialType = pieces[counter++];
      string materialGrade = pieces[counter++];
      if (materialType == "STEEL")
      {
        if (steels != null)
        {
          GSAMaterialSteel matchingMaterial = steels.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }
      else if (materialType == "CONCRETE")
      {
        if (concretes != null)
        {
          GSAMaterialConcrete matchingMaterial = concretes.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }

      counter++; // Analysis material
      obj.Thickness = Convert.ToDouble(pieces[counter++]);

      switch (pieces[counter++])
      {
        case "CENTROID":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Middle;
          break;
        case "TOP_CENTRE":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Top;
          break;
        case "BOT_CENTRE":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Bottom;
          break;
        default:
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Middle;
          break;
      }
      // Ignore the rest

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural2DProperty prop = this.Value as Structural2DProperty;

      string keyword = typeof(GSA2DProperty).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA2DProperty).GetGSAKeyword(), prop.ApplicationId);
      int materialRef = 0;
      string materialType = "UNDEF";

      var res = GSA.Indexer.LookupIndex(typeof(GSAMaterialSteel).GetGSAKeyword(), prop.MaterialRef);
      if (res.HasValue)
      {
        materialRef = res.Value;
        materialType = "STEEL";
      }
      else
      {
        res = GSA.Indexer.LookupIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), prop.MaterialRef);
        if (res.HasValue)
        {
          materialRef = res.Value;
          materialType = "CONCRETE";
        }
      }

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + HelperClass.GenerateSID(prop));
      ls.Add(index.ToString());
      ls.Add(prop.Name == null || prop.Name == "" ? " " : prop.Name);
      ls.Add("NO_RGB");
      ls.Add("SHELL");
      ls.Add("GLOBAL");
      ls.Add("0"); // Analysis material
      ls.Add(materialType);
      ls.Add(materialRef.ToString());
      ls.Add("0"); // Design
      ls.Add(prop.Thickness.ToString());
      switch (prop.ReferenceSurface)
      {
        case Structural2DPropertyReferenceSurface.Middle:
          ls.Add("CENTROID");
          break;
        case Structural2DPropertyReferenceSurface.Top:
          ls.Add("TOP_CENTRE");
          break;
        case Structural2DPropertyReferenceSurface.Bottom:
          ls.Add("BOT_CENTRE");
          break;
        default:
          ls.Add("CENTROID");
          break;
      }
      ls.Add("0"); // Ref_z
      ls.Add("0"); // Mass
      ls.Add("100%"); // Flex modifier
      ls.Add("100%"); // Shear modifier
      ls.Add("100%"); // Inplane modifier
      ls.Add("100%"); // Weight modifier
      ls.Add("NO_ENV"); // Environmental data

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural2DProperty prop)
    {
      new GSA2DProperty() { Value = prop }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA2DProperty dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA2DProperty)))
        GSASenderObjects[typeof(GSA2DProperty)] = new List<object>();

      List<GSA2DProperty> props = new List<GSA2DProperty>();
      List<GSAMaterialSteel> steels = GSASenderObjects[typeof(GSAMaterialSteel)].Cast<GSAMaterialSteel>().ToList();
      List<GSAMaterialConcrete> concretes = GSASenderObjects[typeof(GSAMaterialConcrete)].Cast<GSAMaterialConcrete>().ToList();

      string keyword = typeof(GSA2DProperty).GetGSAKeyword();
      string[] subKeywords = typeof(GSA2DProperty).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA2DProperty)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA2DProperty)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          GSA2DProperty prop = new GSA2DProperty() { GWACommand = p };
          prop.ParseGWACommand(GSA, steels, concretes);
          props.Add(prop);
        }
        catch { }
      }

      GSASenderObjects[typeof(GSA2DProperty)].AddRange(props);

      if (props.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
