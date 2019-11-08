using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { }, "results", true, false, new Type[] { typeof(GSANode) }, new Type[] { })]
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
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults && !Initialiser.GSASenderObjects.ContainsKey(typeof(GSANode)))
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults)
      {
        var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

        foreach (var kvp in Initialiser.Settings.NodalResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase)) continue;

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
                  Value = new Dictionary<string, object>()
                };
              }
              (node.Value.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = resultExport.ToDictionary( x => x.Key, x => (x.Value as List<double>)[0] as object );

              node.ForceSend = true;
            }
          }
        }
      }
      else
      {
        Initialiser.GSASenderObjects[typeof(GSANodeResult)] = new List<object>();

        var results = new List<GSANodeResult>();

        var keyword = HelperClass.GetGSAKeyword(typeof(GSANode));

        var indices = Initialiser.Indexer.LookupIndices(keyword).Where(i => i.HasValue).Select(i => i.Value).ToList();

        foreach (var kvp in Initialiser.Settings.NodalResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase)) continue;

            for (var i = 0; i < indices.Count(); i++)
            {
              var id = indices[i];

            //var id = 1;
            //var highestIndex = (int)Initialiser.Interface.RunGWACommand("HIGHEST\t" + keyword);

            //while (id <= highestIndex)
            //{
              //if ((int)Initialiser.Interface.RunGWACommand("EXIST\t" + keyword + "\t" + id.ToString()) == 1)
              //{
                var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

                if (resultExport == null || resultExport.Count() == 0)
                {
                  id++;
                  continue;
                }
                
                var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                if (existingRes == null)
                {
                  var newRes = new StructuralNodeResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = HelperClass.GetApplicationId(typeof(GSANode).GetGSAKeyword(), id),
                    IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                  };
                  newRes.Value[kvp.Key] = resultExport;

                  newRes.GenerateHash();

                  results.Add(new GSANodeResult() { Value = newRes });
                }
                else
                {
                  existingRes.Value.Value[kvp.Key] = resultExport;
                }
              //}
              //id++;
            }
          }
        }

        Initialiser.GSASenderObjects[typeof(GSANodeResult)].AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
