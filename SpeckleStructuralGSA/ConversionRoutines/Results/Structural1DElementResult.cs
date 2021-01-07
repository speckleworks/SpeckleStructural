using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  //Because the application ID could come from the member (if the element is derived from a parent member)"
  // - GSA1DMember is also listed as a read prerequisite
  // - MEMB.8 is listed as a subkeyword
  [GSAObject("", new string[] { "EL.4", "MEMB.8" }, "results", true, false, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember) }, new Type[] { })]
  public class GSA1DElementResult : GSABase<Structural1DElementResult>
  {
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSA1DElementResult dummyObject)
    {
      if (Initialiser.Settings.Element1DResults.Count() == 0 
        || Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSA1DElement>() == 0)
      {
        return new SpeckleNull();
      }

      var keyword = typeof(GSA1DElement).GetGSAKeyword();

      if (Initialiser.Settings.EmbedResults)
      {
        var elements = Initialiser.GSASenderObjects.Get<GSA1DElement>();

        var entities = elements.Cast<GSA1DElement>().ToList();

        foreach (var kvp in Initialiser.Settings.Element1DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          {
            foreach (var entity in entities)
            {
              var obj = (Structural1DElement)entity.Value;
              var id = entity.GSAId;

              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis
                ? "local" : "global", Initialiser.Settings.Result1DNumPosition);

              if (resultExport == null)
              {
                continue;
              }

              var newResult = new Structural1DElementResult()
              {
                TargetRef = obj.ApplicationId,
                LoadCaseRef = loadCase,
                Value = new Dictionary<string, object>()
              };

              //The setter of entity.Value.Result won't accept a value if there are no keys (to avoid issues during merging), so
              //setting a value here needs to be done with at least one key in it
              if (obj.Result == null)
              {
                obj.Result = new Dictionary<string, object>() { { loadCase, newResult }};
              }
              else if (!obj.Result.ContainsKey(loadCase))
              {
                obj.Result[loadCase] = newResult;
              }

              (obj.Result[loadCase] as Structural1DElementResult).Value[kvp.Key] = resultExport;
            }
          }
        }

        // Linear interpolate the line values
        foreach (var entity in entities)
        {
          var obj = (Structural1DElement)entity.Value;

          var dX = (obj.Value[3] - obj.Value[0]) / (Initialiser.Settings.Result1DNumPosition + 1);
          var dY = (obj.Value[4] - obj.Value[1]) / (Initialiser.Settings.Result1DNumPosition + 1);
          var dZ = (obj.Value[5] - obj.Value[2]) / (Initialiser.Settings.Result1DNumPosition + 1);

          var interpolatedVertices = new List<double>();
          interpolatedVertices.AddRange((obj.Value as List<double>).Take(3));

          for (var i = 1; i <= Initialiser.Settings.Result1DNumPosition; i++)
          {
            interpolatedVertices.Add(interpolatedVertices[0] + dX * i);
            interpolatedVertices.Add(interpolatedVertices[1] + dY * i);
            interpolatedVertices.Add(interpolatedVertices[2] + dZ * i);
          }

          interpolatedVertices.AddRange((obj.Value as List<double>).Skip(3).Take(3));

          obj.ResultVertices = interpolatedVertices;
        }
      }
      else
      {
        var results = new List<GSA1DElementResult>();

        //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 1d element.  There is always though
        //some GWA loaded into the cache
        if (!Initialiser.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
        {
          return new SpeckleNull();
        }

        foreach (var kvp in Initialiser.Settings.Element1DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          {
            for (var i = 0; i < indices.Count(); i++)
            {
              var pPieces = gwa[i].ListSplit(Initialiser.Interface.GwaDelimiter);
              if (pPieces[4].ParseElementNumNodes() != 2 || indices[i] == 0)
              {
                continue;
              }

              var resultExport = Initialiser.Interface.GetGSAResult(indices[i], kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, 
                Initialiser.Settings.ResultInLocalAxis ? "local" : "global", Initialiser.Settings.Result1DNumPosition);

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var targetRef = applicationIds[i];
              if (string.IsNullOrEmpty(applicationIds[i]))
              {
                //The call to ToSpeckle() for 1D element would create application Ids in the cache, but when this isn't called (like for results-only sending)
                //then the cache would be filled with elements' and members' GWA commands but not their non-Speckle-originated (i.e. stored in SIDs) application IDs, 
                //and so in that case the application ID would need to be calculated in the same way as what would happen as a result of the ToSpeckle() call
                if (Helper.GetElementParentIdFromGwa(gwa[i], out var memberIndex) && memberIndex > 0)
                {
                  targetRef = SpeckleStructuralClasses.Helper.CreateChildApplicationId(indices[i], Helper.GetApplicationId(typeof(GSA1DMember).GetGSAKeyword(), memberIndex));
                }
                else
                {
                  targetRef = Helper.GetApplicationId(keyword, indices[i]);
                }
              }

              var existingRes = results.FirstOrDefault(x => ((StructuralResultBase)x.Value).TargetRef == targetRef
                && ((StructuralResultBase)x.Value).LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = targetRef,
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                  LoadCaseRef = loadCase
                };
                newRes.Value[kvp.Key] = resultExport;

                newRes.GenerateHash();

                results.Add(new GSA1DElementResult() { Value = newRes, GSAId = indices[i] });
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
