using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "NODE.3" }, "results", true, false, new Type[] { typeof(GSANode) }, new Type[] { })]
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
      if ((Initialiser.Settings.NodalResults.Count() == 0)
        || (Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSANode>() == 0))
      {
        return new SpeckleNull();
      }

      if (Initialiser.Settings.EmbedResults)
      {
        var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

        Parallel.ForEach(Initialiser.Settings.NodalResults, (kvp) =>
        //foreach (var kvp in Initialiser.Settings.NodalResults)
        {
          var resultType = kvp.Key;
          var columns = resultType.ToNodeResultCsvColumns();
          //Use the file-based result extraction if it's supported, otherwise revert back to the old COM-based method
          var useApiForResults = (columns == null);

          //foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          Parallel.ForEach(Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)), (loadCase) =>
          {
            //foreach (var node in nodes)
            Parallel.ForEach(nodes, (node) =>
            {
              var id = node.GSAId;

              if (node.Value.Result == null)
              {
                node.Value.Result = new Dictionary<string, object>();
              }

              Dictionary<string, object> resultExport = null;
              object[,] contextResults = null;
              if (useApiForResults)
              {
                resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase,
                  Initialiser.Settings.ResultInLocalAxis ? "local" : "global");
                if (resultExport == null || resultExport.Count() == 0)
                {
                  //continue;
                  return;
                }
              }
              else
              {
                if (!Initialiser.ResultsContext.Query("result_node", columns, new[] { loadCase }, out contextResults, new[] { id })
                  || contextResults == null || contextResults.GetLength(0) == 0 || contextResults.GetLength(1) == 0)
                {
                  //continue;
                  return;
                }
              }

              var newResult = new StructuralNodeResult()
              {
                LoadCaseRef = loadCase,
                TargetRef = Helper.GetApplicationId(typeof(GSANode).GetGSAKeyword(), id),
                Value = new Dictionary<string, object>(),
              };

              //The setter of node.Value.Result won't accept a value if there are no keys (to avoid issues during merging), so
              //setting a value here needs to be done with at least one key in it
              if (node.Value.Result == null)
              {
                node.Value.Result = new Dictionary<string, object>() { { loadCase, newResult } };
              }
              else if (!node.Value.Result.ContainsKey(loadCase))
              {
                node.Value.Result[loadCase] = newResult;
              }

              (node.Value.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = (useApiForResults)
                ? resultExport.ToDictionary(x => x.Key, x => (x.Value as List<double>)[0] as object)
                : (node.Value.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = contextResults.ToNodeResultValue(resultType, columns);

              node.ForceSend = true;
            }
            );
          }
          );
        }
        );
      }
      else
      {
        var resultsLock = new object();
        var results = new List<GSANodeResult>();

        var keyword = Helper.GetGSAKeyword(typeof(GSANode));

        var indices = Initialiser.Cache.LookupIndices(keyword).Where(i => i.HasValue).Select(i => i.Value).ToList();

        //foreach (var kvp in Initialiser.Settings.NodalResults)
        Parallel.ForEach(Initialiser.Settings.NodalResults, (kvp) =>
        {
          var resultType = kvp.Key;
          var columns = resultType.ToNodeResultCsvColumns();
          var useApiForResults = (columns == null);

          //foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          Parallel.ForEach(Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)), (loadCase) =>
          {
            Parallel.For(0, indices.Count(), (i) =>
            //for (var i = 0; i < indices.Count(); i++)
            {
              var id = indices[i];
              Dictionary<string, object> resultExport = null;
              object[,] contextResults = null;
              if (useApiForResults)
              {
                resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase,
                  Initialiser.Settings.ResultInLocalAxis ? "local" : "global");
                if (resultExport == null || resultExport.Count() == 0)
                {
                  return;
                }
              }
              else
              {
                if (!Initialiser.ResultsContext.Query("result_node", columns, new[] { loadCase }, out contextResults, new[] { id })
                  || contextResults == null || contextResults.GetLength(0) == 0 || contextResults.GetLength(1) == 0)
                {
                  return;
                }
              }

              var resultValues = (useApiForResults) ? resultExport : contextResults.ToNodeResultValue(resultType, columns);

              lock (resultsLock)
              {
                var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                if (existingRes == null)
                {
                  var newRes = new StructuralNodeResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = Helper.GetApplicationId(typeof(GSANode).GetGSAKeyword(), id),
                    IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                    LoadCaseRef = loadCase
                  };
                  newRes.Value[kvp.Key] = resultValues;

                  newRes.GenerateHash();

                  results.Add(new GSANodeResult() { Value = newRes });
                }
                else
                {
                  existingRes.Value.Value[kvp.Key] = resultValues;
                }
              }
            }
            );
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
