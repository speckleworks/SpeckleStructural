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
  [GSAObject("", new string[] { }, "results", true, false, new Type[] { typeof(GSA1DElement) }, new Type[] { })]
  public class GSA1DElementResult : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElementResult();
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSA1DElementResult dummyObject)
    {
      if (Conversions.GSAElement1DResults.Count() == 0)
        return new SpeckleNull();

      if (Conversions.GSAEmbedResults && !GSASenderObjects.ContainsKey(typeof(GSA1DElement)))
        return new SpeckleNull();

      if (Conversions.GSAEmbedResults)
      {
        List<GSA1DElement> elements = GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList();

        List<IGSASpeckleContainer> entities = elements.Cast<IGSASpeckleContainer>().ToList();

        foreach (KeyValuePair<string, Tuple<int, int, List<string>>> kvp in Conversions.GSAElement1DResults)
        {
          foreach (string loadCase in GSAResultCases)
          {
            if (!GSA.CaseExist(loadCase))
              continue;

            foreach (IGSASpeckleContainer entity in entities)
            {
              int id = entity.GSAId;

              if (entity.Value.Result == null)
                entity.Value.Result = new Dictionary<string, object>();

              var resultExport = GSA.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, GSAResultInLocalAxis ? "local" : "global", Conversions.GSAResult1DNumPosition);
            
              if (resultExport == null)
                continue;

              if (!entity.Value.Result.ContainsKey(loadCase))
                entity.Value.Result[loadCase] = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>()
                };

              (entity.Value.Result[loadCase] as Structural1DElementResult).Value[kvp.Key] = resultExport;
            }
          }
        }

        // Linear interpolate the line values
        foreach (IGSASpeckleContainer entity in entities)
        {
          var dX = (entity.Value.Value[3] - entity.Value.Value[0]) / (Conversions.GSAResult1DNumPosition + 1);
          var dY = (entity.Value.Value[4] - entity.Value.Value[1]) / (Conversions.GSAResult1DNumPosition + 1);
          var dZ = (entity.Value.Value[5] - entity.Value.Value[2]) / (Conversions.GSAResult1DNumPosition + 1);

          var interpolatedVertices = new List<double>();
          interpolatedVertices.AddRange((entity.Value.Value as List<double>).Take(3));
        
          for (int i = 1; i <= Conversions.GSAResult1DNumPosition; i++)
          {
            interpolatedVertices.Add(interpolatedVertices[0] + dX * i);
            interpolatedVertices.Add(interpolatedVertices[1] + dY * i);
            interpolatedVertices.Add(interpolatedVertices[2] + dZ * i);
          }

          interpolatedVertices.AddRange((entity.Value.Value as List<double>).Skip(3).Take(3));

          entity.Value.ResultVertices = interpolatedVertices;
        }
      }
      else
      {
        GSASenderObjects[typeof(GSA1DElementResult)] = new List<object>();

        List<GSA1DElementResult> results = new List<GSA1DElementResult>();
        
        string keyword = HelperClass.GetGSAKeyword(typeof(GSA1DElement));

        foreach (KeyValuePair<string, Tuple<int, int, List<string>>> kvp in Conversions.GSAElement1DResults)
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
                string record = GSA.GetGWARecords("GET\t" + keyword + "\t" + id.ToString())[0];

                string[] pPieces = record.ListSplit("\t");
                if (pPieces[4].ParseElementNumNodes() != 2)
                {
                  id++;
                  continue;
                }
                
                var resultExport = GSA.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, GSAResultInLocalAxis ? "local" : "global", Conversions.GSAResult1DNumPosition);

                if (resultExport == null)
                {
                  id++;
                  continue;
                }

                var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                if (existingRes == null)
                {
                  Structural1DElementResult newRes = new Structural1DElementResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = GSA.GetSID(typeof(GSA1DElement).GetGSAKeyword(), id),
                    IsGlobal = !GSAResultInLocalAxis,
                  };
                  newRes.Value[kvp.Key] = resultExport;

                  newRes.GenerateHash();

                  results.Add(new GSA1DElementResult() { Value = newRes });
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

        GSASenderObjects[typeof(GSA1DElementResult)].AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
