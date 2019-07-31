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
  [GSAObject("MEMB.7", new string[] { }, "elements", true, false, new Type[] { typeof(GSA1DElement), typeof(GSA1DLoad), typeof(GSA1DElementResult), typeof(GSAAssembly), typeof(GSAConstructionStage), typeof(GSA1DInfluenceEffect) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA1DElementPolyline : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DElementPolyline();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSA1DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      var elementsListCopy = new List<GSA1DElement>(elements);

      Structural1DElementPolyline obj = new Structural1DElementPolyline();
      obj.ApplicationId = GSA.GetSID(typeof(GSA1DElementPolyline).GetGSAKeyword(), GSAId);

      obj.Value = new List<double>();
      obj.ElementApplicationId = new List<string>();

      obj.ElementType = elementsListCopy.First().Value.ElementType;
      obj.PropertyRef = elementsListCopy.First().Value.PropertyRef;
      obj.ZAxis = new List<StructuralVectorThree>();
      obj.EndRelease = new List<StructuralVectorBoolSix>();
      obj.Offset = new List<StructuralVectorThree>();
      obj.ResultVertices = new List<double>();

      if (Conversions.GSAElement1DResults.Count > 0 && Conversions.GSAEmbedResults)
        obj.Result = new Dictionary<string, object>();

      // Match up coordinates
      List<Tuple<string, string>> coordinates = new List<Tuple<string, string>>();

      foreach (GSA1DElement e in elementsListCopy)
        coordinates.Add( new Tuple<string, string>(
          string.Join(",", (e.Value.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString())),
          string.Join(",", (e.Value.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()))
        ));

      // Find start coordinate
      List<string> flatCoordinates = coordinates.SelectMany(x => new List<string>() { x.Item1, x.Item2 }).ToList();
      List<string> uniqueCoordinates = flatCoordinates.Where(x => flatCoordinates.Count(y => y == x) == 1).ToList();

      string current = uniqueCoordinates[0];
      while(coordinates.Count > 0)
      {
        var matchIndex = 0;
        var reverseCoordinates = false;
        
        matchIndex = coordinates.FindIndex(x => x.Item1 == current);
        reverseCoordinates = false;
        if (matchIndex == -1)
        { 
          matchIndex = coordinates.FindIndex(x => x.Item2 == current);
          reverseCoordinates = true;
        }

        var element = elementsListCopy[matchIndex];

        obj.ElementApplicationId.Add(element.Value.ApplicationId);
        obj.ZAxis.Add(element.Value.ZAxis);

        if (obj.Value.Count == 0)
        {
          if (!reverseCoordinates)
          { 
            obj.Value.AddRange((element.Value.Value as List<double>).Take(3));
          }
          else
          { 
            obj.Value.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
          }
        }

        if (!reverseCoordinates)
        {
          current = string.Join(",", (element.Value.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
          obj.EndRelease.AddRange(element.Value.EndRelease);
          obj.Offset.AddRange(element.Value.Offset);

          if (Conversions.GSAElement1DResults.Count > 0 && Conversions.GSAEmbedResults)
            obj.ResultVertices.AddRange(element.Value.ResultVertices);
          else
            obj.ResultVertices.AddRange((element.Value.Value as List<double>));
        }
        else
        {
          current = string.Join(",", (element.Value.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value.Value as List<double>).Take(3));
          obj.EndRelease.Add((element.Value.EndRelease as List<StructuralVectorBoolSix>).Last());
          obj.EndRelease.Add((element.Value.EndRelease as List<StructuralVectorBoolSix>).First());
          obj.Offset.Add((element.Value.Offset as List<StructuralVectorThree>).Last());
          obj.Offset.Add((element.Value.Offset as List<StructuralVectorThree>).First());

          if (Conversions.GSAElement1DResults.Count > 0 && Conversions.GSAEmbedResults)
            for (int i = element.Value.ResultVertices.Count - 3; i >= 0; i -= 3)
              obj.ResultVertices.AddRange((element.Value.ResultVertices as List<double>).Skip(i).Take(3));
          else
          {
            obj.ResultVertices.AddRange((element.Value.Value as List<double>).Skip(3).Take(3));
            obj.ResultVertices.AddRange((element.Value.Value as List<double>).Take(3));
          }
        }

        // Result merging
        if (obj.Result != null)
        {
          try
          { 
            foreach (string loadCase in element.Value.Result.Keys)
            {
              if (!obj.Result.ContainsKey(loadCase))
                obj.Result[loadCase] = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Conversions.GSAResultInLocalAxis,
                };

              var resultExport = element.Value.Result[loadCase] as Structural1DElementResult;

              if (resultExport != null)
              {
                foreach (string key in resultExport.Value.Keys)
                {
                  if (!(obj.Result[loadCase] as Structural1DElementResult).Value.ContainsKey(key))
                    (obj.Result[loadCase] as Structural1DElementResult).Value[key] = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  else
                    foreach (string resultKey in ((obj.Result[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>).Keys)
                    {
                      var res = (resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>;
                      if (reverseCoordinates)
                        res.Reverse();
                      (((obj.Result[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange(res);
                    }
                }
              }
              else
              {
                // UNABLE TO MERGE RESULTS
                obj.Result = null;
                break;
              }
            }
          }
          catch
          {
            // UNABLE TO MERGE RESULTS
            obj.Result = null;
          }
        }

        coordinates.RemoveAt(matchIndex);
        elementsListCopy.RemoveAt(matchIndex);

        this.SubGWACommand.Add(element.GWACommand);
        this.SubGWACommand.AddRange(element.SubGWACommand);
      }
      
      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural1DElementPolyline obj = this.Value as Structural1DElementPolyline;

      int group = GSA.Indexer.ResolveIndex(typeof(GSA1DElementPolyline), obj);

      Structural1DElement[] elements = obj.Explode();

      foreach (Structural1DElement element in elements)
      {
        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
          new GSA1DElement() { Value = element }.SetGWACommand(GSA, group);
        else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
          new GSA1DMember() { Value = element }.SetGWACommand(GSA, group);
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this SpecklePolyline inputObject)
    {
      Structural1DElementPolyline convertedObject = new Structural1DElementPolyline();

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static bool ToNative(this Structural1DElementPolyline poly)
    {
      new GSA1DElementPolyline() { Value = poly }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DElementPolyline dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DElementPolyline)))
        GSASenderObjects[typeof(GSA1DElementPolyline)] = new List<object>();

      List<GSA1DElementPolyline> polylines = new List<GSA1DElementPolyline>();

      // Perform mesh merging
      var uniqueMembers = new List<string>(GSASenderObjects[typeof(GSA1DElement)].Select(x => (x as GSA1DElement).Member).Where(m => Convert.ToInt32(m) > 0).Distinct());
      foreach (string member in uniqueMembers)
      {
        var elementList = GSASenderObjects[typeof(GSA1DElement)].Where(x => (x as GSA1DElement).Member == member).Cast<GSA1DElement>().ToList();
        GSA1DElementPolyline poly = new GSA1DElementPolyline() { GSAId = Convert.ToInt32(member) };
        poly.ParseGWACommand(GSA, elementList);
        polylines.Add(poly);

        GSASenderObjects[typeof(GSA1DElement)].RemoveAll(x => elementList.Contains(x));
      }

      GSASenderObjects[typeof(GSA1DElementPolyline)].AddRange(polylines);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA1DElement will handle this change
    }
  }
}
