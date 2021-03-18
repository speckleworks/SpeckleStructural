using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  //TO DO: check why everything except GSA1DElement is needed as read prerequisites 
  [GSAObject("EL.4", new string[] { }, "model", true, false, new Type[] { typeof(GSA1DProperty), typeof(GSA1DElement), typeof(GSA1DLoad), typeof(GSA1DElementResult), typeof(GSAAssembly), typeof(GSAConstructionStage), typeof(GSA1DInfluenceEffect) }, new Type[] { typeof(GSA1DProperty), typeof(GSA1DElement) })]
  public class GSA1DElementPolyline : GSABase<Structural1DElementPolyline>
  {
    public void ParseGWACommand(List<GSA1DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      var elementsListCopy = new List<GSA1DElement>(elements);

      var obj = new Structural1DElementPolyline
      {
        ApplicationId = Helper.GetApplicationId(typeof(GSA1DMember).GetGSAKeyword(), GSAId),

        Value = new List<double>(),
        ElementApplicationId = new List<string>(),

        ElementType = elementsListCopy.First().Value.ElementType,
        PropertyRef = elementsListCopy.First().Value.PropertyRef,
        ZAxis = new List<StructuralVectorThree>(),
        EndRelease = new List<StructuralVectorBoolSix>(),
        Offset = new List<StructuralVectorThree>(),
        ResultVertices = new List<double>()
      };

      if (obj.Properties == null)
      {
        obj.Properties = new Dictionary<string, object>();
      }
      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }

      Dictionary<string, object> results = null;
      if (Initialiser.AppResources.Settings.Element1DResults.Count > 0 && Initialiser.AppResources.Settings.EmbedResults)
        results = new Dictionary<string, object>();

      // Match up coordinates
      var coordinates = new List<Tuple<string, string>>();

      foreach (var e in elementsListCopy)
        coordinates.Add(new Tuple<string, string>(
          string.Join(",", (e.Value.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString())),
          string.Join(",", (e.Value.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()))
        ));

      // Find start coordinate
      var flatCoordinates = coordinates.SelectMany(x => new List<string>() { x.Item1, x.Item2 }).ToList();
      var uniqueCoordinates = flatCoordinates.Where(x => flatCoordinates.Count(y => y == x) == 1).ToList();

      var current = uniqueCoordinates[0];

      //Because these properties could, depending on how they've been added to the StructuralProperties dictionary,
      //return another instance of the lists instead of a pointer to the lists themselves, temporary variables are used
      //to build up new lists which are assigned as replacements to the property values further down
      if (obj.ElementApplicationId == null)
      {
        obj.ElementApplicationId = new List<string>();
      }
      var elementAppIds = obj.ElementApplicationId ?? new List<string>();
      var gsaIds = new List<int>();
      var zAxes = obj.ZAxis ?? new List<StructuralVectorThree>();
      var endReleases = obj.EndRelease ?? new List<StructuralVectorBoolSix>();
      var offsets = obj.Offset ?? new List<StructuralVectorThree>();
      var resultVertices = obj.ResultVertices ?? new List<double>();

      while (coordinates.Count > 0)
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

        var gsaElement = elementsListCopy[matchIndex];
        var element = (Structural1DElement)gsaElement.Value;

        elementAppIds.Add(element.ApplicationId);
        try
        {
          if (int.TryParse(((Dictionary<string, object>)element.Properties["structural"])["NativeId"].ToString(), out int gsaId))
          gsaIds.Add(gsaId);
        }
        catch { }

        zAxes.Add(element.ZAxis);

        if (obj.Value.Count == 0)
        {
          if (!reverseCoordinates)
          {
            obj.Value.AddRange((element.Value as List<double>).Take(3));
          }
          else
          {
            obj.Value.AddRange((element.Value as List<double>).Skip(3).Take(3));
          }
        }

        if (!reverseCoordinates)
        {
          current = string.Join(",", (element.Value as List<double>).Skip(3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value as List<double>).Skip(3).Take(3));
          endReleases.AddRange(element.EndRelease);
          offsets.AddRange(element.Offset);

          if (Initialiser.AppResources.Settings.Element1DResults.Count > 0 && Initialiser.AppResources.Settings.EmbedResults)
          {
            resultVertices.AddRange(element.ResultVertices);
          }
          else
          {
            resultVertices.AddRange((element.Value as List<double>));
          }
        }
        else
        {
          current = string.Join(",", (element.Value as List<double>).Take(3).Select(x => Math.Round(x, 4).ToString()));
          obj.Value.AddRange((element.Value as List<double>).Take(3));
          endReleases.Add((element.EndRelease as List<StructuralVectorBoolSix>).Last());
          endReleases.Add((element.EndRelease as List<StructuralVectorBoolSix>).First());
          offsets.Add((element.Offset as List<StructuralVectorThree>).Last());
          offsets.Add((element.Offset as List<StructuralVectorThree>).First());

          if (Initialiser.AppResources.Settings.Element1DResults.Count > 0 && Initialiser.AppResources.Settings.EmbedResults)
          {
            for (var i = (element.ResultVertices.Count - 3); i >= 0; i -= 3)
            {
              resultVertices.AddRange((element.ResultVertices as List<double>).Skip(i).Take(3));
            }
          }
          else
          {
            resultVertices.AddRange((element.Value as List<double>).Skip(3).Take(3));
            resultVertices.AddRange((element.Value as List<double>).Take(3));
          }
        }

        // Result merging
        if (results != null && ((Structural1DElement)gsaElement.Value).Result != null)
        {
          try
          {
            foreach (var loadCase in element.Result.Keys)
            {
              if (!results.ContainsKey(loadCase))
              {
                results[loadCase] = new Structural1DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                };
              }


              if (element.Result[loadCase] is Structural1DElementResult resultExport)
              {
                foreach (var key in resultExport.Value.Keys)
                {
                  if (!(results[loadCase] as Structural1DElementResult).Value.ContainsKey(key))
                  {
                    (results[loadCase] as Structural1DElementResult).Value[key]
                      = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  }
                  else
                  {
                    foreach (var resultKey in ((results[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>).Keys)
                    {
                      var res = (resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>;
                      if (reverseCoordinates)
                      {
                        res.Reverse();
                      }
                      (((results[loadCase] as Structural1DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange(res);
                    }
                  }
                }
              }
              else
              {
                // UNABLE TO MERGE RESULTS
                //obj.Result = null;
                results = null;
                break;
              }
            }
          }
          catch
          {
            // UNABLE TO MERGE RESULTS
            //obj.Result = null;
            results = null;
          }
        }

        coordinates.RemoveAt(matchIndex);
        elementsListCopy.RemoveAt(matchIndex);

        this.SubGWACommand.Add(gsaElement.GWACommand);
        this.SubGWACommand.AddRange(gsaElement.SubGWACommand);
      }

      obj.ElementApplicationId = elementAppIds;
      obj.ZAxis = zAxes;
      obj.EndRelease = endReleases;
      obj.Offset = offsets;
      obj.ResultVertices = resultVertices;
      obj.Result = results;

      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeIds", gsaIds.Select(gid => gid.ToString()).ToList());

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var obj = this.Value as Structural1DElementPolyline;
      if (obj.Value == null || obj.Value.Count() == 0)
        return "";

      var elements = obj.Explode();
      var gwaCommands = new List<string>();

      if (elements.Count() == 1)
      {
        gwaCommands.Add((Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
         ? new GSA1DElement() { Value = elements.First() }.SetGWACommand()
         : new GSA1DMember() { Value = elements.First() }.SetGWACommand());
      }
      else
      {
        var group = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA1DElementPolyline).GetGSAKeyword(), obj.ApplicationId);

        foreach (var element in elements)
        {
          gwaCommands.Add((Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
            ? new GSA1DElement() { Value = element }.SetGWACommand(group)
            : new GSA1DMember() { Value = element }.SetGWACommand(group));
        }
      }
      return string.Join("\n", gwaCommands);
    }
  }

  //This class is here to host the GSAObject attribute which is picked up in the reflection-based construction of the type hierarchy
  [GSAObject("MEMB.8", new string[] { }, "model", false, true, new Type[] { typeof(GSA1DProperty), typeof(GSA1DMember), typeof(GSA1DLoad), typeof(GSA1DElementResult), typeof(GSAAssembly), typeof(GSAConstructionStage), typeof(GSA1DInfluenceEffect) }, new Type[] { typeof(GSA1DProperty), typeof(GSA1DMember) })]
  public class GSA1DMemberFromPolyline : GSABase<Structural1DElementPolyline>
  {
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpecklePolyline inputObject)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(inputObject, () =>
      {
        var convertedObject = new Structural1DElementPolyline();

        foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
        {
          var inputProperty = inputObject.GetType().GetProperty(p.Name);
          if (inputProperty != null)
          {
            p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
          }
        }

        return convertedObject.ToNative();
      });
    }

    public static string ToNative(this Structural1DElementPolyline poly)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(poly, () => new GSA1DElementPolyline() { Value = poly }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSA1DElementPolyline dummyObject)
    {
      var keyword = dummyObject.GetGSAKeyword();
      var polylines = new List<GSA1DElementPolyline>();
      var typeName = dummyObject.GetType().Name;
      // Perform mesh merging
      var uniqueMembers = new List<int>(Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>().Select(x => x.Member).Where(m => m > 0).Distinct());
      uniqueMembers.Sort();  //Just for readability and testing

      //This loop has been left as serial for now, considering the fact that the sender objects are retrieved and removed-from with each iteration
      foreach (var member in uniqueMembers)
      {
        try
        {
          var all1dElements = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>();
          var matching1dElementList = all1dElements.Where(x => x.Member == member).OrderBy(m => m.GSAId).ToList();
          if (matching1dElementList.Count() > 1)
          {
            var poly = new GSA1DElementPolyline() { GSAId = Convert.ToInt32(member) };
            try
            {
              poly.ParseGWACommand(matching1dElementList);
              polylines.Add(poly);
            }
            catch (Exception ex)
            {
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
                "Keyword=" + keyword, "Index=" + member);
            }

            Initialiser.GsaKit.GSASenderObjects.RemoveAll(matching1dElementList);
          }
        }
        catch { }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(polylines);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA1DElement will handle this change
    }
  }
}
