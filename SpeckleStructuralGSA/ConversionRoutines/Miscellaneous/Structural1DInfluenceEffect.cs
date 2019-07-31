using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;
using SQLite;

namespace SpeckleStructuralGSA
{
  [GSAObject("INF_BEAM.1", new string[] { }, "misc", true, false, new Type[] { typeof(GSA1DElement) }, new Type[] { typeof(GSA1DElement) })]
  public class GSA1DInfluenceEffect : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DInfluenceEffect();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSA1DElement> e1Ds)
    {
      if (this.GWACommand == null)
        return;

      Structural1DInfluenceEffect obj = new Structural1DInfluenceEffect();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.GSAEffectGroup = Convert.ToInt32(pieces[counter++]);

      var targetElementRef = pieces[counter++];

      GSA1DElement targetElement;

      if (e1Ds != null)
      {
        targetElement = e1Ds.Where(e => targetElementRef == e.GSAId.ToString()).FirstOrDefault();

        obj.ElementRef = targetElement.Value.ApplicationId;

        this.SubGWACommand.Add(targetElement.GWACommand);
      }
      else
        return;

      var pos = pieces[counter++];
      if (pos.Contains("%"))
      {
        obj.Position = Convert.ToDouble(pos.Replace("%", "")) / 100;
      }
      else
      {
        // Get element length
        var eCoor = targetElement.Value.Value;
        var eLength = Math.Sqrt(
          Math.Pow(eCoor[0] - eCoor[3], 2) +
          Math.Pow(eCoor[1] - eCoor[4], 2) +
          Math.Pow(eCoor[2] - eCoor[5], 2)
        );
        obj.Position = Convert.ToDouble(pos) / eLength;
      }

      obj.Factor = Convert.ToDouble(pieces[counter++]);
      var effectType = pieces[counter++];
      switch(effectType)
      {
        case "DISP":
          obj.EffectType = StructuralInfluenceEffectType.Displacement;
          break;
        case "FORCE":
          obj.EffectType = StructuralInfluenceEffectType.Force;
          break;
        default:
          return;
      }

      counter++; //Axis which doesn't make sense

      var dir = pieces[counter++];
      obj.Directions = new StructuralVectorBoolSix(new bool[6]);
      switch (dir.ToLower())
      {
        case "x":
          obj.Directions.Value[0] = true;
          break;
        case "y":
          obj.Directions.Value[1] = true;
          break;
        case "z":
          obj.Directions.Value[2] = true;
          break;
        case "xx":
          obj.Directions.Value[3] = true;
          break;
        case "yy":
          obj.Directions.Value[4] = true;
          break;
        case "zz":
          obj.Directions.Value[5] = true;
          break;
      }

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural1DInfluenceEffect infl = this.Value as Structural1DInfluenceEffect;
      
      string keyword = typeof(GSA1DInfluenceEffect).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA1DInfluenceEffect), infl);

      int? elementRef = GSA.Indexer.LookupIndex(typeof(GSA1DElement), infl.ElementRef);

      if (!elementRef.HasValue)
        return;

      string[] direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (int i = 0; i < infl.Directions.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + GSA.GenerateSID(infl));
        ls.Add(infl.Name == null || infl.Name == "" ? " " : infl.Name);
        ls.Add(infl.GSAEffectGroup.ToString());
        ls.Add(elementRef.Value.ToString());
        ls.Add((infl.Position * 100).ToString() + "%");
        ls.Add(infl.Factor.ToString());
        switch (infl.EffectType)
        {
          case StructuralInfluenceEffectType.Force:
            ls.Add("FORCE");
            break;
          case StructuralInfluenceEffectType.Displacement:
            ls.Add("DISP");
            break;
          default:
            return;
        }
        ls.Add("GLOBAL"); // TODO: GSA TEAM TO LOOK INTO THIS. GLOBAL IS DEFAULT IN GSA
        ls.Add(direction[i]);
        GSA.RunGWACommand(string.Join("\t", ls));
      }
    }
  }
  
  public static partial class Conversions
  {
    public static bool ToNative(this Structural1DInfluenceEffect infl)
    {
      new GSA1DInfluenceEffect() { Value = infl }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DInfluenceEffect dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DInfluenceEffect)))
        GSASenderObjects[typeof(GSA1DInfluenceEffect)] = new List<object>();

      List<GSA1DInfluenceEffect> infls = new List<GSA1DInfluenceEffect>();
      List<GSA1DElement> e1Ds = GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList();

      string keyword = typeof(GSA1DInfluenceEffect).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DInfluenceEffect).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA1DInfluenceEffect)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA1DInfluenceEffect)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSA1DInfluenceEffect infl = new GSA1DInfluenceEffect() { GWACommand = p };
        infl.ParseGWACommand(GSA, e1Ds);
        infls.Add(infl);
      }

      GSASenderObjects[typeof(GSA1DInfluenceEffect)].AddRange(infls);

      if (infls.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
