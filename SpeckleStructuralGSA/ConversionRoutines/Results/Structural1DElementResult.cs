using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "EL.4" }, "results", true, false, new Type[] { typeof(GSA1DElement) }, new Type[] { })]
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
      if (Initialiser.Settings.Element1DResults.Count() == 0)
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSA1DElement>() == 0)
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults)
      {
        var elements = Initialiser.GSASenderObjects.Get<GSA1DElement>();

        var entities = elements.Cast<IGSASpeckleContainer>().ToList();

        foreach (var kvp in Initialiser.Settings.Element1DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase))
              continue;

            foreach (var entity in entities)
            {
              var id = entity.GSAId;

              if (entity.Value.Result == null)
                entity.Value.Result = new Dictionary<string, object>();

              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis
                ? "local" : "global", Initialiser.Settings.Result1DNumPosition);

              if (resultExport == null)
              {
                continue;
              }

              var newResult = new Structural1DElementResult()
              {
                TargetRef = Helper.GetApplicationId(typeof(GSA1DElement).GetGSAKeyword(), id),
                LoadCaseRef = loadCase,
                Value = new Dictionary<string, object>()
              };

              //The setter of entity.Value.Result won't accept a value if there are no keys (to avoid issues during merging), so
              //setting a value here needs to be done with at least one key in it
              if (entity.Value.Result == null)
              {
                entity.Value.Result = new Dictionary<string, object>() { { loadCase, newResult }};
              }
              else if (!entity.Value.Result.ContainsKey(loadCase))
              {
                entity.Value.Result[loadCase] = newResult;
              }

              (entity.Value.Result[loadCase] as Structural1DElementResult).Value[kvp.Key] = resultExport;
            }
          }
        }

        // Linear interpolate the line values
        foreach (var entity in entities)
        {
          var dX = (entity.Value.Value[3] - entity.Value.Value[0]) / (Initialiser.Settings.Result1DNumPosition + 1);
          var dY = (entity.Value.Value[4] - entity.Value.Value[1]) / (Initialiser.Settings.Result1DNumPosition + 1);
          var dZ = (entity.Value.Value[5] - entity.Value.Value[2]) / (Initialiser.Settings.Result1DNumPosition + 1);

          var interpolatedVertices = new List<double>();
          interpolatedVertices.AddRange((entity.Value.Value as List<double>).Take(3));

          for (var i = 1; i <= Initialiser.Settings.Result1DNumPosition; i++)
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
        var results = new List<GSA1DElementResult>();

        var keyword = Helper.GetGSAKeyword(typeof(GSA1DElement));
        var gwa = Initialiser.Cache.GetGwa(keyword);

        foreach (var kvp in Initialiser.Settings.Element1DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase))
              continue;

            for (var i = 0; i < gwa.Count(); i++)
            {
              var record = gwa[i];

              var pPieces = record.ListSplit("\t");
              if (pPieces[4].ParseElementNumNodes() != 2)
              {
                continue;
              }

              if (!int.TryParse(pPieces[1], out var id))
              {
                //Could not extract index
                continue;
              }

              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global", Initialiser.Settings.Result1DNumPosition);

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
              if (existingRes == null)
              {
                var newRes = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = Helper.GetApplicationId(typeof(GSA1DElement).GetGSAKeyword(), id),
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                  LoadCaseRef = loadCase
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
          }
        }

        Initialiser.GSASenderObjects.AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
