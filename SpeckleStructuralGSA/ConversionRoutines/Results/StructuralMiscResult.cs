﻿using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { }, "results", true, false, new Type[] { }, new Type[] { })]
  public class GSAMiscResult : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralMiscResult();
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSAMiscResult dummyObject)
    {
      Initialiser.GSASenderObjects[typeof(GSAMiscResult)] = new List<object>();

      if (Initialiser.Settings.MiscResults.Count() == 0)
        return new SpeckleNull();

      List<GSAMiscResult> results = new List<GSAMiscResult>();

      foreach (var kvp in Initialiser.Settings.MiscResults)
      {
        foreach (string loadCase in Initialiser.Settings.ResultCases)
        {
          if (!Initialiser.Interface.CaseExist(loadCase))
            continue;

          int id = 0;
          int highestIndex = 0;

          if (!string.IsNullOrEmpty(kvp.Value.Item1))
          {
            highestIndex = (int)Initialiser.Interface.RunGWACommand("HIGHEST\t" + kvp.Value.Item1);
            id = 1;
          }

          while (id <= highestIndex)
          {
            if (id == 0 || (int)Initialiser.Interface.RunGWACommand("EXIST\t" + kvp.Value.Item1 + "\t" + id.ToString()) == 1)
            {
              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
              {
                id++;
                continue;
              }

              var newRes = new StructuralMiscResult
              {
                Description = kvp.Key,
                IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                Value = resultExport,
                ResultSource = loadCase
              };
              if (id != 0)
                newRes.TargetRef = Initialiser.Indexer.GetApplicationId(kvp.Value.Item1, id);
              newRes.GenerateHash();
              results.Add(new GSAMiscResult() { Value = newRes });
            }
            id++;
          }
        }
      }

      Initialiser.GSASenderObjects[typeof(GSAMiscResult)].AddRange(results);

      return new SpeckleObject();
    }
  }
}
