using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "NODE.2" }, "results", true, false, new Type[] { typeof(GSANode) }, new Type[] { })]
  public class GSANodeResult : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNodeResult();
  }


  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSANodeResult dummyObject)
    {
      if (Initialiser.Settings.NodalResults.Count() == 0)
      {
        return new SpeckleNull();
      }

      if (Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSANode>() == 0)
      {
        return new SpeckleNull();
      }

      if (Initialiser.Settings.EmbedResults)
      {
        var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

        Parallel.ForEach(Initialiser.Settings.NodalResults, kvp =>
        {
          Parallel.ForEach(Initialiser.Settings.ResultCases, loadCase =>
          {
            if (Initialiser.Interface.CaseExist(loadCase))
            {
              foreach (var node in nodes)
              {
                var id = node.GSAId;

                if (node.Value.Result == null)
                {
                  node.Value.Result = new Dictionary<string, object>();
                }

                var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

                if (resultExport == null || resultExport.Count() == 0)
                {
                  continue;
                }

                if (!node.Value.Result.ContainsKey(loadCase))
                {
                  node.Value.Result[loadCase] = new StructuralNodeResult()
                  {
                  LoadCaseRef = loadCase,
                  TargetRef = Helper.GetApplicationId(typeof(GSANode).GetGSAKeyword(), id),
                    Value = new Dictionary<string, object>()
                  };
                }
                (node.Value.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = resultExport.ToDictionary(x => x.Key, x => (x.Value as List<double>)[0] as object);

                node.ForceSend = true;
              }
            }
          }
          );
        }
        );
      }
      else
      {
        var results = new List<GSANodeResult>();
        var resultsLock = new object();

        var keyword = Helper.GetGSAKeyword(typeof(GSANode));

        var indices = Initialiser.Cache.LookupIndices(keyword).Where(i => i.HasValue).Select(i => i.Value).ToList();

        Parallel.ForEach(Initialiser.Settings.NodalResults, kvp =>
        {
          Parallel.ForEach(Initialiser.Settings.ResultCases, loadCase =>
          {
            if (Initialiser.Interface.CaseExist(loadCase))
            {
              for (var i = 0; i < indices.Count(); i++)
              {
                var id = indices[i];

                var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

                if (resultExport == null || resultExport.Count() == 0)
                {
                  id++;
                  continue;
                }
                GSANodeResult existingRes;
                lock (resultsLock)
                {
                  existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                }
                if (existingRes == null)
                {
                  var newRes = new StructuralNodeResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = Helper.GetApplicationId(typeof(GSANode).GetGSAKeyword(), id),
                    IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                  LoadCaseRef = loadCase
                  };
                  newRes.Value[kvp.Key] = resultExport;

                  newRes.GenerateHash();

                  lock (resultsLock)
                  {
                    results.Add(new GSANodeResult() { Value = newRes });
                  }
                }
                else
                {
                  existingRes.Value.Value[kvp.Key] = resultExport;
                }
              }
            }
          }
          );
        }
        );

        Initialiser.GSASenderObjects.AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
