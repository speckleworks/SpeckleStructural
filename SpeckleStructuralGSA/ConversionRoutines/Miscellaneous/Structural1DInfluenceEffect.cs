using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("INF_BEAM.1", new string[] { }, "misc", true, false, new Type[] { typeof(GSA1DElement) }, new Type[] { typeof(GSA1DElement) })]
  public class GSA1DInfluenceEffect : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DInfluenceEffect();

    public void ParseGWACommand(List<GSA1DElement> e1Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DInfluenceEffect();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
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
      switch (effectType)
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

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var infl = this.Value as Structural1DInfluenceEffect;

      var keyword = typeof(GSA1DInfluenceEffect).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA1DInfluenceEffect).GetGSAKeyword(), infl.ApplicationId);

      var elementRef = Initialiser.Cache.LookupIndex(typeof(GSA1DElement).GetGSAKeyword(), infl.ElementRef);

      if (!elementRef.HasValue)
        return "";

      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < infl.Directions.Value.Count(); i++)
      {
        var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + ":" + Helper.GenerateSID(infl),
          infl.Name == null || infl.Name == "" ? " " : infl.Name,
          infl.GSAEffectGroup.ToString(),
          elementRef.Value.ToString(),
          (infl.Position * 100).ToString() + "%",
          infl.Factor.ToString()
        };
        switch (infl.EffectType)
        {
          case StructuralInfluenceEffectType.Force:
            ls.Add("FORCE");
            break;
          case StructuralInfluenceEffectType.Displacement:
            ls.Add("DISP");
            break;
          default:
            return "";
        }
        ls.Add("GLOBAL"); // TODO: GSA TEAM TO LOOK INTO THIS. GLOBAL IS DEFAULT IN GSA
        ls.Add(direction[i]);
        gwaCommands.Add(string.Join("\t", ls));
      }
      return string.Join("\n", gwaCommands);
    }
  }
  
  public static partial class Conversions
  {
    public static string ToNative(this Structural1DInfluenceEffect infl)
    {
      return new GSA1DInfluenceEffect() { Value = infl }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA1DInfluenceEffect dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DInfluenceEffect>();
      var typeName = dummyObject.GetType().Name;
      var e1Ds = Initialiser.GSASenderObjects.Get<GSA1DElement>();

      var inflsLock = new object();
      var infls = new List<GSA1DInfluenceEffect>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var p = newLines[k];
          var infl = new GSA1DInfluenceEffect() { GWACommand = p };
          infl.ParseGWACommand(e1Ds);
          lock (inflsLock)
          {
            infls.Add(infl);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }
      });

      Initialiser.GSASenderObjects.AddRange(infls);

      return (infls.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
