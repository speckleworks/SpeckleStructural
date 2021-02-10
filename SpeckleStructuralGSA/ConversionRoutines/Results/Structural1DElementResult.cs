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
      if (Initialiser.AppResources.Settings.Element1DResults.Count() == 0
        || Initialiser.AppResources.Settings.EmbedResults && Initialiser.GsaKit.GSASenderObjects.Count<GSA1DElement>() == 0)
      {
        return new SpeckleNull();
      }

      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";
      var num1dPos = Initialiser.AppResources.Settings.Result1DNumPosition;
      var typeName = dummyObject.GetType().Name;

      if (Initialiser.AppResources.Settings.EmbedResults)
      {
        Embed1DResults(typeName, axisStr, num1dPos);
      }
      else
      {
        if (!Create1DElementResultObjects(typeName, axisStr, num1dPos))
        {
          return new SpeckleNull();
        }
      }

      return new SpeckleObject();
    }

    private static bool Create1DElementResultObjects(string typeName, string axisStr, int num1dPos)
    {
      var results = new List<GSA1DElementResult>();
      var memberKw = typeof(GSA1DMember).GetGSAKeyword();
      var keyword = typeof(GSA1DElement).GetGSAKeyword();

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each 1d element.  There is always though
      //some GWA loaded into the cache
      if (!Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return false;
      }

      foreach (var kvp in Initialiser.AppResources.Settings.Element1DResults)
      {
        foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
        {
          for (var i = 0; i < indices.Count(); i++)
          {
            try
            {
              var pPieces = gwa[i].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
              if (pPieces[4].ParseElementNumNodes() != 2 || indices[i] == 0)
              {
                continue;
              }

              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(indices[i], kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, axisStr, num1dPos);

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
                  targetRef = SpeckleStructuralClasses.Helper.CreateChildApplicationId(indices[i], Helper.GetApplicationId(memberKw, memberIndex));
                }
                else
                {
                  targetRef = Helper.GetApplicationId(keyword, indices[i]);
                }
              }

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == targetRef && x.Value.LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = targetRef,
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
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
            catch (Exception ex)
            {
              var contextDesc = string.Join(" ", typeName, kvp.Key, loadCase);
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, i.ToString());
            }
          }
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(results);

      return true;
    }

    private static void Embed1DResults(string typeName, string axisStr, int num1dPos)
    {
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>();

      var entities = elements.Cast<GSA1DElement>().ToList();

      foreach (var kvp in Initialiser.AppResources.Settings.Element1DResults)
      {
        foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
        {
          foreach (var entity in entities)
          {
            var id = entity.GSAId;
            var obj = entity.Value;

            try
            {
              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, axisStr, num1dPos);

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
                obj.Result = new Dictionary<string, object>() { { loadCase, newResult } };
              }
              else if (!obj.Result.ContainsKey(loadCase))
              {
                obj.Result[loadCase] = newResult;
              }

              (obj.Result[loadCase] as Structural1DElementResult).Value[kvp.Key] = resultExport;
            }
            catch (Exception ex)
            {
              var contextDesc = string.Join(" ", typeName, kvp.Key, loadCase);
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, id.ToString());
            }
          }
        }
      }

      // Linear interpolate the line values
      foreach (var entity in entities)
      {
        var obj = entity.Value;

        var dX = (obj.Value[3] - obj.Value[0]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);
        var dY = (obj.Value[4] - obj.Value[1]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);
        var dZ = (obj.Value[5] - obj.Value[2]) / (Initialiser.AppResources.Settings.Result1DNumPosition + 1);

        var interpolatedVertices = new List<double>();
        interpolatedVertices.AddRange(obj.Value.Take(3));

        for (var i = 1; i <= Initialiser.AppResources.Settings.Result1DNumPosition; i++)
        {
          interpolatedVertices.Add(interpolatedVertices[0] + dX * i);
          interpolatedVertices.Add(interpolatedVertices[1] + dY * i);
          interpolatedVertices.Add(interpolatedVertices[2] + dZ * i);
        }

        interpolatedVertices.AddRange(obj.Value.Skip(3).Take(3));

        obj.ResultVertices = interpolatedVertices;
      }
    }
  }
}
