using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { }, "results", true, false, new Type[] { typeof(GSA2DElement) }, new Type[] { })]
  public class GSA2DElementResult : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DElementResult();
  }

  public static partial class Conversions
  {
    public static SpeckleObject ToSpeckle(this GSA2DElementResult dummyObject)
    {
      if (Initialiser.Settings.Element2DResults.Count() == 0)
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults && !Initialiser.GSASenderObjects.ContainsKey(typeof(GSA2DElement)))
        return new SpeckleNull();

      if (Initialiser.Settings.EmbedResults)
      {
        var elements = Initialiser.GSASenderObjects[typeof(GSA2DElement)].Cast<GSA2DElement>().ToList();

        foreach (var kvp in Initialiser.Settings.Element2DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase))
              continue;

            foreach (var element in elements)
            {
              var id = element.GSAId;

              if (element.Value.Result == null)
                element.Value.Result = new Dictionary<string, object>();

              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null || resultExport.Count() == 0)
                continue;

              if (!element.Value.Result.ContainsKey(loadCase))
                element.Value.Result[loadCase] = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>()
                };

              // Let's split the dictionary into xxx_face and xxx_vertex
              var faceDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => new List<double>() { (x.Value as List<double>).Last() } as object);
              var vertexDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);

              (element.Value.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_face"] = faceDictionary;
              (element.Value.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_vertex"] = vertexDictionary;
            }
          }
        }
      }
      else
      {
        Initialiser.GSASenderObjects[typeof(GSA2DElementResult)] = new List<object>();

        var results = new List<GSA2DElementResult>();

        var keyword = HelperClass.GetGSAKeyword(typeof(GSA2DElement));

        foreach (var kvp in Initialiser.Settings.Element2DResults)
        {
          foreach (var loadCase in Initialiser.Settings.ResultCases)
          {
            if (!Initialiser.Interface.CaseExist(loadCase))
              continue;

            var gwa = Initialiser.Indexer.GetGwa(keyword);

            for (var i = 0; i < gwa.Count(); i++)
            {
              var record = gwa[i];

              var pPieces = record.ListSplit("\t");
              if (pPieces[4].ParseElementNumNodes() != 3 && pPieces[4].ParseElementNumNodes() != 4)
              {
                continue;
              }

              if (!int.TryParse(pPieces[1], out var id))
              {
                //Could not extract index
                continue;
              }

              var resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase, Initialiser.Settings.ResultInLocalAxis ? "local" : "global");

              if (resultExport == null)
              {
                continue;
              }

              // Let's split the dictionary into xxx_face and xxx_vertex
              var faceDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => new List<double>() { (x.Value as List<double>).Last() } as object);
              var vertexDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);

              var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
              if (existingRes == null)
              {
                var newRes = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  TargetRef = Initialiser.Indexer.GetApplicationId(typeof(GSA2DElement).GetGSAKeyword(), id),
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                };
                newRes.Value[kvp.Key + "_face"] = faceDictionary;
                newRes.Value[kvp.Key + "_vertex"] = vertexDictionary;

                newRes.GenerateHash();

                results.Add(new GSA2DElementResult() { Value = newRes });
              }
              else
              {
                existingRes.Value.Value[kvp.Key + "_face"] = faceDictionary;
                existingRes.Value.Value[kvp.Key + "_vertex"] = vertexDictionary;
              }
            }

          }
        }

        Initialiser.GSASenderObjects[typeof(GSA2DElementResult)].AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
