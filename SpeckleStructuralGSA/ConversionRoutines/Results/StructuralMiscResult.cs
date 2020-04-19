using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { }, "results", true, false, new Type[] { }, new Type[] { })]
  public class GSAMiscResult : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralMiscResult();
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSAMiscResult dummyObject)
    {
      //Initialiser.GSASenderObjects.Get<GSAMiscResult)] = new List<object>();

      if (Initialiser.Settings.MiscResults.Count() == 0)
        return new SpeckleNull();

      var results = new List<GSAMiscResult>();

      var indices = Initialiser.Cache.LookupIndices(typeof(GSAAssembly).GetGSAKeyword()).Where(i => i.HasValue).Select(i => i.Value).ToList();

      foreach (var kvp in Initialiser.Settings.MiscResults)
      {
        foreach (var loadCase in Initialiser.Settings.ResultCases)
        {
          if (!Initialiser.Interface.CaseExist(loadCase)) continue;

          var gwa = Initialiser.Cache.GetGwa("");

          var id = 0;

          for (var i = 0; i < indices.Count(); i++)
          {
            id = indices[i];

            var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

            if (resultExport == null || resultExport.Count() == 0)
            {
              id++;
              continue;
            }

            var newRes = new StructuralMiscResult
            {
              Description = kvp.Key,
              IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
              Value = resultExport,
              ResultSource = loadCase
            };

            if (id != 0)
            {
              newRes.TargetRef = Helper.GetApplicationId(kvp.Value.Item1, id);
            }
            newRes.GenerateHash();
            results.Add(new GSAMiscResult() { Value = newRes });
          }
        }
      }

      Initialiser.GSASenderObjects.AddRange(results);

      return new SpeckleObject();
    }
  }
}
