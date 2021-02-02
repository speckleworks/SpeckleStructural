using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "NODE.3" }, "results", true, false, new Type[] { typeof(GSANode) }, new Type[] { })]
  public class GSANodeResult : GSABase<StructuralNodeResult>
  {
  }


  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSANodeResult dummyObject)
    {
      if (Initialiser.AppResources.Settings.NodalResults.Count() == 0 || Initialiser.AppResources.Settings.EmbedResults && Initialiser.GsaKit.GSASenderObjects.Count<GSANode>() == 0)
      {
        return new SpeckleNull();
      }

      if (Initialiser.AppResources.Settings.EmbedResults)
      {
        var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

        foreach (var kvp in Initialiser.AppResources.Settings.NodalResults)
        {
          foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
          {
            foreach (var node in nodes)
            {
              var id = node.GSAId;
              var obj = node.Value;

              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>();
              }

              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var newResult = new StructuralNodeResult()
              {
                LoadCaseRef = loadCase,
                TargetRef = obj.ApplicationId,
                Value = new Dictionary<string, object>()
              };

              //The setter of obj.Result won't accept a value if there are no keys (to avoid issues during merging), so
              //setting a value here needs to be done with at least one key in it
              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>() { { loadCase, newResult } };
              }
              else if (!obj.Result.ContainsKey(loadCase))
              {
                obj.Result[loadCase] = newResult;
              }

              (obj.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = resultExport.ToDictionary(x => x.Key, x => (x.Value as List<double>)[0] as object);

              node.ForceSend = true;
            }
          }
        }
      }
      else
      {
        var results = new List<GSANodeResult>();

        var keyword = typeof(GSANode).GetGSAKeyword();

        //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each node.  There is always though
        //some GWA loaded into the cache
        if (!Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
        {
          return new SpeckleNull();
        }

        foreach (var kvp in Initialiser.AppResources.Settings.NodalResults)
        {
          foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
          {
            for (var i = 0; i < indices.Count(); i++)
            {
              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(indices[i], kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, 
                Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }
              var targetRef = string.IsNullOrEmpty(applicationIds[i]) ? Helper.GetApplicationId(keyword, indices[i]) : applicationIds[i];

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == targetRef && x.Value.LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new StructuralNodeResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = targetRef,
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                  LoadCaseRef = loadCase
                };
                newRes.Value[kvp.Key] = resultExport;

                newRes.GenerateHash();

                results.Add(new GSANodeResult() { Value = newRes, GSAId = indices[i] });
              }
              else
              {
                existingRes.Value.Value[kvp.Key] = resultExport;
              }
            }
          }
        }

        Initialiser.GsaKit.GSASenderObjects.AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
