using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("", new string[] { "EL.4" }, "results", true, false, new Type[] { typeof(GSA2DElement) }, new Type[] { })]
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
      if ((Initialiser.Settings.Element2DResults.Count() == 0)
        || (Initialiser.Settings.EmbedResults && Initialiser.GSASenderObjects.Count<GSA2DElement>() == 0))
      {
        return new SpeckleNull();
      }

      if (Initialiser.Settings.EmbedResults)
      {
        var elements = Initialiser.GSASenderObjects.Get<GSA2DElement>();

        //foreach (var kvp in Initialiser.Settings.Element2DResults)
        Parallel.ForEach(Initialiser.Settings.Element2DResults, (kvp) =>
        {
          var resultType = kvp.Key;
          var columns = resultType.To2dResultCsvColumns();
          //Use the file-based result extraction if it's supported, otherwise revert back to the old COM-based method
          var useApiForResults = (columns == null);

          //foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          Parallel.ForEach(Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)), loadCase =>
          {
            //foreach (var element in )
            Parallel.ForEach(elements, element =>
            {
              var id = element.GSAId;

              if (element.Value.Result == null)
              {
                element.Value.Result = new Dictionary<string, object>();
              }

              Dictionary<string, object> resultExport = null;
              object[,] contextResults = null;
              if (useApiForResults)
              {
                resultExport = Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase,
                  Initialiser.Settings.ResultInLocalAxis ? "local" : "global");
                if (resultExport == null || resultExport.Count() == 0)
                {
                  //continue;
                  return;
                }
              }
              else
              {
                if (!Initialiser.ResultsContext.Query("result_element_2d", columns, new[] { loadCase }, out contextResults, new[] { id })
                  || contextResults == null || contextResults.GetLength(0) == 0 || contextResults.GetLength(1) == 0)
                {
                  //continue;
                  return;
                }
              }

              if (!element.Value.Result.ContainsKey(loadCase))
                element.Value.Result[loadCase] = new Structural2DElementResult()
                {
                  TargetRef = Helper.GetApplicationId(typeof(GSA2DElement).GetGSAKeyword(), id),
                  LoadCaseRef = loadCase,
                  Value = new Dictionary<string, object>()
                };

              Dictionary<string, object> faceDictionary;
              Dictionary<string, object> vertexDictionary;
              if (useApiForResults)
              {
                // Let's split the dictionary into xxx_face and xxx_vertex
                faceDictionary = resultExport.ToDictionary(
                  x => x.Key,
                  x => new List<double>() { (x.Value as List<double>).Last() } as object);
                vertexDictionary = resultExport.ToDictionary(
                  x => x.Key,
                  x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);
              }
              else
              {
                var lastRowIndex = contextResults.GetUpperBound(0);
                faceDictionary = contextResults.To2dResultValue(resultType, columns, 0, lastRowIndex - 1);
                vertexDictionary = contextResults.To2dResultValue(resultType, columns, lastRowIndex, lastRowIndex);
              }

              (element.Value.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_face"] = faceDictionary;
              (element.Value.Result[loadCase] as Structural2DElementResult).Value[kvp.Key + "_vertex"] = vertexDictionary;
            }
            );
          }
          );
        }
        );
      }
      else
      {
        var resultsLock = new object();
        var results = new List<GSA2DElementResult>();

        var keyword = Helper.GetGSAKeyword(typeof(GSA2DElement));
        var gwa = Initialiser.Cache.GetGwa(keyword);

        //foreach (var kvp in Initialiser.Settings.Element2DResults)
        Parallel.ForEach(Initialiser.Settings.Element2DResults, kvp =>
        {
          var resultType = kvp.Key;
          var columns = resultType.To2dResultCsvColumns();
          //Use the file-based result extraction if it's supported, otherwise revert back to the old COM-based method
          var useApiForResults = (columns == null);

          //foreach (var loadCase in Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)))
          Parallel.ForEach(Initialiser.Settings.ResultCases.Where(rc => Initialiser.Interface.CaseExist(rc)), loadCase =>
          {
            //for (var i = 0; i < gwa.Count(); i++)
            Parallel.For(0, gwa.Count(), i =>
            {
              var record = gwa[i];

              var pPieces = record.ListSplit("\t");
              if (pPieces[4].ParseElementNumNodes() != 3 && pPieces[4].ParseElementNumNodes() != 4)
              {
                return;
              }

              if (!int.TryParse(pPieces[1], out var id))
              {
                //Could not extract index
                return;
              }

              Dictionary<string, object> resultExport = null;
              object[,] contextResults = null;
              if (useApiForResults)
              {
                Initialiser.Interface.GetGSAResult(id, kvp.Value.Item1, kvp.Value.Item2, kvp.Value.Item3, loadCase,
                  Initialiser.Settings.ResultInLocalAxis ? "local" : "global");
                if (resultExport == null)
                {
                  return;
                }
              }
              else
              {
                if (!Initialiser.ResultsContext.Query("result_element_2d", columns, new[] { loadCase }, out contextResults, new[] { id })
                  || contextResults == null || contextResults.GetLength(0) == 0 || contextResults.GetLength(1) == 0)
                {
                  return;
                }
              }

              Dictionary<string, object> faceDictionary;
              Dictionary<string, object> vertexDictionary;
              if (useApiForResults)
              {
                // Let's split the dictionary into xxx_face and xxx_vertex
                faceDictionary = resultExport.ToDictionary(
                x => x.Key,
                x => new List<double>() { (x.Value as List<double>).Last() } as object);
                vertexDictionary = resultExport.ToDictionary(
                  x => x.Key,
                  x => (x.Value as List<double>).Take((x.Value as List<double>).Count - 1).ToList() as object);
              }
              else
              {
                var lastRowIndex = contextResults.GetUpperBound(0);
                faceDictionary = contextResults.To2dResultValue(resultType, columns, 0, lastRowIndex - 1);
                vertexDictionary = contextResults.To2dResultValue(resultType, columns, lastRowIndex, lastRowIndex);
              }

              lock (resultsLock)
              {
                var existingRes = results.FirstOrDefault(x => x.Value.TargetRef == id.ToString());
                if (existingRes == null)
                {
                  var newRes = new Structural2DElementResult()
                  {
                    Value = new Dictionary<string, object>(),
                    TargetRef = Helper.GetApplicationId(typeof(GSA2DElement).GetGSAKeyword(), id),
                    IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                    LoadCaseRef = loadCase
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
            );
          }
          );
        }
        );

        Initialiser.GSASenderObjects.AddRange(results);
      }

      return new SpeckleObject();
    }
  }
}
