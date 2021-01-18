﻿using System;
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
      if (Initialiser.Instance.Settings.MiscResults.Count() == 0 || !Initialiser.Instance.Cache.GetKeywordRecordsSummary(keyword, out var gwa, out var indices, out var applicationIds))
      {
        return new SpeckleNull();
      }

      var results = new List<GSAMiscResult>();

      //Unlike embedding, separate results doesn't necessarily mean that there is a Speckle object created for each assembly.  There is always though
      //some GWA loaded into the cache
      foreach (var kvp in Initialiser.Instance.Settings.MiscResults)
      {
        foreach (var loadCase in Initialiser.Instance.Settings.ResultCases.Where(rc => Initialiser.Instance.Interface.CaseExist(rc)))
        {
          for (var i = 0; i < indices.Count(); i++)
          {
            var resultExport = Initialiser.Instance.Interface.GetGSAResult(indices[i], kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4, loadCase, 
              Initialiser.Instance.Settings.ResultInLocalAxis ? "local" : "global");

            if (resultExport == null || resultExport.Count() == 0)
            {
              continue;
            }

            var targetRef = (string.IsNullOrEmpty(applicationIds[i])) ? Helper.GetApplicationId(keyword, indices[i]) : applicationIds[i];

            var existingRes = results.FirstOrDefault(x => ((StructuralResultBase)x.Value).TargetRef == targetRef
                && ((StructuralResultBase)x.Value).LoadCaseRef == loadCase);

            if (existingRes == null)
            {
              var newRes = new StructuralMiscResult
              {
                Description = kvp.Key,
                IsGlobal = !Initialiser.Instance.Settings.ResultInLocalAxis,
                Value = resultExport,
                LoadCaseRef = loadCase,
                TargetRef = string.IsNullOrEmpty(applicationIds[i]) ? Helper.GetApplicationId(keyword, indices[i]) : applicationIds[i]
              };
              newRes.GenerateHash();
              results.Add(new GSAMiscResult() { Value = newRes, GSAId = indices[i] });
            }
            else
            {
              ((StructuralMiscResult)existingRes.Value).Value[kvp.Key] = resultExport;
            }
          }
        }
      }

      Initialiser.Instance.GSASenderObjects.AddRange(results);

      return new SpeckleObject();
    }
  }
}
