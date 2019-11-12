using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_TITLE.2", new string[] { }, "loads", true, true, new Type[] { }, new Type[] { })]
  public class GSALoadCase : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadCase();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralLoadCase();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = HelperClass.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      var type = pieces[counter++];
      switch (type)
      {
        case "DEAD":
          obj.CaseType = StructuralLoadCaseType.Dead;
          break;
        case "LC_VAR_IMP":
          obj.CaseType = StructuralLoadCaseType.Live;
          break;
        case "WIND":
          obj.CaseType = StructuralLoadCaseType.Wind;
          break;
        case "SNOW":
          obj.CaseType = StructuralLoadCaseType.Snow;
          break;
        case "SEISMIC":
          obj.CaseType = StructuralLoadCaseType.Earthquake;
          break;
        case "LC_PERM_SOIL":
          obj.CaseType = StructuralLoadCaseType.Soil;
          break;
        case "LC_VAR_TEMP":
          obj.CaseType = StructuralLoadCaseType.Thermal;
          break;
        default:
          obj.CaseType = StructuralLoadCaseType.Generic;
          break;
      }

      // Rest is unimportant

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var loadCase = this.Value as StructuralLoadCase;

      var keyword = typeof(GSALoadCase).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).ToSpeckleTypeName(), loadCase.ApplicationId);

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + HelperClass.GenerateSID(loadCase),
        index.ToString(),
        loadCase.Name == null || loadCase.Name == "" ? " " : loadCase.Name
      };
      switch (loadCase.CaseType)
      {
        case StructuralLoadCaseType.Dead:
          ls.Add("DEAD");
          break;
        case StructuralLoadCaseType.Live:
          ls.Add("LC_VAR_IMP");
          break;
        case StructuralLoadCaseType.Wind:
          ls.Add("WIND");
          break;
        case StructuralLoadCaseType.Snow:
          ls.Add("SNOW");
          break;
        case StructuralLoadCaseType.Earthquake:
          ls.Add("SEISMIC");
          break;
        case StructuralLoadCaseType.Soil:
          ls.Add("LC_PERM_SOIL");
          break;
        case StructuralLoadCaseType.Thermal:
          ls.Add("LC_VAR_TEMP");
          break;
        default:
          ls.Add("LC_UNDEF");
          break;
      }
      ls.Add("1"); // Source
      ls.Add("~"); // Category
      ls.Add("NONE"); // Direction
      ls.Add("INC_BOTH"); // Include

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadCase load)
    {
      return new GSALoadCase() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSALoadCase dummyObject)
    {
      var newLines = ToSpeckleBase<GSALoadCase>();

      var loadCases = new List<GSALoadCase>();

      foreach (var p in newLines.Values)
      {
        var loadCase = new GSALoadCase() { GWACommand = p };
        loadCase.ParseGWACommand();
        loadCases.Add(loadCase);
      }

      Initialiser.GSASenderObjects[typeof(GSALoadCase)].AddRange(loadCases);

      return (loadCases.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
