using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "ASSEMBLY.3" }, "results", true, false, new Type[] { typeof(GSAAssembly) }, new Type[] { })]
  public class GSAMiscResult : GSABase<StructuralMiscResult>
  {
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSAMiscResult dummyObject)
    {
      var keyword = typeof(GSAAssembly).GetGSAKeyword();
      var typeName = dummyObject.GetType().Name;
      var axisStr = Initialiser.AppResources.Settings.ResultInLocalAxis ? "local" : "global";

      if (Initialiser.AppResources.Settings.MiscResults.Count() == 0 
        || !Initialiser.AppResources.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return new SpeckleNull();
      }

      var results = new List<GSAMiscResult>();

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each assembly.  There is always though
      //some GWA loaded into the cache
      foreach (var kvp in Initialiser.AppResources.Settings.MiscResults)
      {
        foreach (var loadCase in Initialiser.AppResources.Settings.ResultCases.Where(rc => Initialiser.AppResources.Proxy.CaseExist(rc)))
        {
          for (var i = 0; i < indices.Count(); i++)
          {
            try
            {
              var resultExport = Initialiser.AppResources.Proxy.GetGSAResult(indices[i], kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4, loadCase, axisStr);

              if (resultExport == null || resultExport.Count() == 0)
              {
                continue;
              }

              var targetRef = (string.IsNullOrEmpty(applicationIds[i])) ? Helper.GetApplicationId(keyword, indices[i]) : applicationIds[i];

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == targetRef && x.Value.LoadCaseRef == loadCase);

              if (existingRes == null)
              {
                var newRes = new StructuralMiscResult
                {
                  Description = kvp.Key,
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                  Value = resultExport,
                  LoadCaseRef = loadCase,
                  TargetRef = string.IsNullOrEmpty(applicationIds[i]) ? Helper.GetApplicationId(keyword, indices[i]) : applicationIds[i]
                };
                newRes.GenerateHash();
                results.Add(new GSAMiscResult() { Value = newRes, GSAId = indices[i] });
              }
              else
              {
                existingRes.Value.Value[kvp.Key] = resultExport;
              }
            }
            catch (Exception ex)
            {
              var contextDesc = string.Join(" ", typeName, kvp.Key, loadCase);
              Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, contextDesc, i.ToString());
              Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, contextDesc, i.ToString());
            }
          }
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(results);

      return new SpeckleObject();
    }
  }
}
