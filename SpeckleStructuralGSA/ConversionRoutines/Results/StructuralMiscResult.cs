using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
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
      GSASenderObjects[typeof(GSAMiscResult)] = new List<object>();

      if (Conversions.GSAMiscResults.Count() == 0)
        return new SpeckleNull();

      List<GSAMiscResult> results = new List<GSAMiscResult>();

      foreach (KeyValuePair<string, Tuple<string, int, int, List<string>>> kvp in Conversions.GSAMiscResults)
      {
        foreach (string loadCase in GSAResultCases)
        {
          if (!GSA.CaseExist(loadCase))
            continue;

          int id = 0;
          int highestIndex = 0;

          if (!string.IsNullOrEmpty(kvp.Value.Item1))
          {
            highestIndex = (int)GSA.RunGWACommand("HIGHEST\t" + kvp.Value.Item1);
            id = 1;
          }

          while (id <= highestIndex)
          {
            if (id == 0 || (int)GSA.RunGWACommand("EXIST\t" + kvp.Value.Item1 + "\t" + id.ToString()) == 1)
            {
              var resultExport = GSA.GetGSAResult(id, kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4, loadCase, GSAResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
              {
                id++;
                continue;
              }

              StructuralMiscResult newRes = new StructuralMiscResult();
              newRes.Description = kvp.Key;
              if (id != 0)
                newRes.TargetRef = GSA.GetSID(kvp.Value.Item1, id);
              newRes.IsGlobal = !GSAResultInLocalAxis;
              newRes.Value = resultExport;
              newRes.ResultSource = loadCase;
              newRes.GenerateHash();
              results.Add(new GSAMiscResult() { Value = newRes });
            }
            id++;
          }
        }
      }

      GSASenderObjects[typeof(GSAMiscResult)].AddRange(results);

      return new SpeckleObject();
    }
  }
}
