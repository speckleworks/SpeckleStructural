using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
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
      if (Conversions.GSANodalResults.Count() == 0)
        return new SpeckleNull();

      if (Conversions.GSAEmbedResults && !GSASenderObjects.ContainsKey(typeof(GSANode)))
        return new SpeckleNull();

      if (GSAEmbedResults)
      {
        List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

        foreach (KeyValuePair<string, Tuple<int, int, List<string>>> kvp in Conversions.GSANodalResults)
        {
          foreach (string loadCase in GSAResultCases)
          {
            if (!GSA.CaseExist(loadCase))
              continue;

            foreach (GSANode node in nodes)
            {
              int id = node.GSAId;

              if (node.Value.Result == null)
                node.Value.Result = new Dictionary<string, object>();

              var resultExport = GSA.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, GSAResultInLocalAxis ? "local" : "global");

              if (resultExport == null)
                continue;

              if (!node.Value.Result.ContainsKey(loadCase))
                node.Value.Result[loadCase] = new StructuralNodeResult()
                {
                  Value = new Dictionary<string, object>()
                };
              (node.Value.Result[loadCase] as StructuralNodeResult).Value[kvp.Key] = resultExport.ToDictionary( x => x.Key, x => (x.Value as List<double>)[0] as object );

              node.ForceSend = true;
            }
          }
        }
      }
      else
      {
        GSASenderObjects[typeof(GSANodeResult)] = new List<object>();

        List<GSANodeResult> results = new List<GSANodeResult>();

        string keyword = HelperClass.GetGSAKeyword(typeof(GSANode));

        foreach (KeyValuePair<string, Tuple<int, int, List<string>>> kvp in Conversions.GSANodalResults)
        {
          foreach (string loadCase in GSAResultCases)
          {
            if (!GSA.CaseExist(loadCase))
              continue;

            int id = 1;
            int highestIndex = (int)GSA.RunGWACommand("HIGHEST\t" + keyword);

            while (id <= highestIndex)
            {
              if ((int)GSA.RunGWACommand("EXIST\t" + keyword + "\t" + id.ToString()) == 1)
              {
                var resultExport = GSA.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, GSAResultInLocalAxis ? "local" : "global");

                if (resultExport == null)
                {
                  id++;
                  continue;
                }
                
                var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                if (existingRes == null)
                {
                  StructuralNodeResult newRes = new StructuralNodeResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = GSA.GetSID(typeof(GSANode).GetGSAKeyword(), id),
                    IsGlobal = !GSAResultInLocalAxis,
                  };
                  newRes.Value[kvp.Key] = resultExport;

                  newRes.GenerateHash();

                  results.Add(new GSANodeResult() { Value = newRes });
                }
                else
                {
                  existingRes.Value.Value[kvp.Key] = resultExport;
                }
              }
              id++;
            }
          }
        }

        GSASenderObjects[typeof(GSANodeResult)].AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
