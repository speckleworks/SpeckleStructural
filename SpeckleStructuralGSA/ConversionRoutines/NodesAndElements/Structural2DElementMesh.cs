using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  // Keyword set as MEMB to not clash with grouping of members
  [GSAObject("MEMB.7", new string[] { }, "elements", true, false, new Type[] { typeof(GSA2DElement), typeof(GSA2DLoadAnalysisLayer), typeof(GSA2DElementResult), typeof(GSAAssembly), typeof(GSAConstructionStage) }, new Type[] { typeof(GSA2DProperty) })]
  public class GSA2DElementMesh : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DElementMesh();

    public void ParseGWACommand(List<GSA2DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      var obj = new Structural2DElementMesh
      {
        ApplicationId = Helper.GetApplicationId(typeof(GSA2DElementMesh).GetGSAKeyword(), GSAId),

        Vertices = new List<double>(),
        Faces = new List<int>(),
        ElementApplicationId = new List<string>(),

        ElementType = elements.First().Value.ElementType,
        PropertyRef = elements.First().Value.PropertyRef,
        Axis = new List<StructuralAxis>(),
        Offset = new List<double>()
      };

      if (Initialiser.Settings.Element2DResults.Count > 0 && Initialiser.Settings.EmbedResults)
        obj.Result = new Dictionary<string, object>();

      var axes = obj.Axis;
      var offsets = obj.Offset;
      var elementAppIds = obj.ElementApplicationId;

      foreach (var e in elements)
      {
        var verticesOffset = obj.Vertices.Count() / 3;
        obj.Vertices.AddRange(e.Value.Vertices);
        obj.Faces.Add((e.Value.Faces as List<int>).First());
        obj.Faces.AddRange((e.Value.Faces as List<int>).Skip(1).Select(x => x + verticesOffset));

        
        axes.Add(e.Value.Axis);
        offsets.Add(e.Value.Offset);
        elementAppIds.Add(e.Value.ApplicationId);

        // Result merging
        if (obj.Result != null)
        {
          try
          {
            foreach (string loadCase in e.Value.Result.Keys)
            {
              if (!obj.Result.ContainsKey(loadCase))
                obj.Result[loadCase] = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Initialiser.Settings.ResultInLocalAxis,
                };

              var resultExport = e.Value.Result[loadCase] as Structural2DElementResult;

              if (resultExport != null)
              {
                foreach (var key in resultExport.Value.Keys)
                {
                  if (!(obj.Result[loadCase] as Structural2DElementResult).Value.ContainsKey(key))
                    (obj.Result[loadCase] as Structural2DElementResult).Value[key] = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  else
                    foreach (var resultKey in ((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>).Keys)
                      (((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange((resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>);
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

        this.SubGWACommand.Add(e.GWACommand);
        this.SubGWACommand.AddRange(e.SubGWACommand);
      }

      obj.Axis = axes;
      obj.Offset = offsets;
      obj.ElementApplicationId = elementAppIds;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var obj = this.Value as Structural2DElementMesh;

      var group = Initialiser.Cache.ResolveIndex(typeof(GSA2DElementMesh).GetGSAKeyword(), obj.ApplicationId);

      var elements = obj.Explode();

      var gwaCommands = new List<string>();

      foreach (var element in elements)
      {
        if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          gwaCommands.Add(new GSA2DElement() { Value = element }.SetGWACommand(group));
        }
      }
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpeckleMesh inputObject)
    {
      var convertedObject = new Structural2DElementMesh();

      foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        var inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static string ToNative(this Structural2DElementMesh mesh)
    {
      return (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) 
        ? new GSA2DElementMesh() { Value = mesh }.SetGWACommand() 
        : new GSA2DMember() { Value = mesh }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DElementMesh dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA2DElementMesh)))
      {
        Initialiser.GSASenderObjects[typeof(GSA2DElementMesh)] = new List<object>();
      }

      var meshes = new List<GSA2DElementMesh>();

      // Perform mesh merging
      var uniqueMembers = new List<string>(Initialiser.GSASenderObjects[typeof(GSA2DElement)].Select(x => (x as GSA2DElement).Member).Where(m => Convert.ToInt32(m) > 0).Distinct());
      foreach (var member in uniqueMembers)
      {
        try
        {
          var elementList = Initialiser.GSASenderObjects[typeof(GSA2DElement)].Where(x => (x as GSA2DElement).Member == member).Cast<GSA2DElement>().ToList();
          var mesh = new GSA2DElementMesh() { GSAId = Convert.ToInt32(member) };
          mesh.ParseGWACommand(elementList);
          meshes.Add(mesh);

          Initialiser.GSASenderObjects[typeof(GSA2DElement)].RemoveAll(x => elementList.Contains(x));
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSA2DElementMesh)].AddRange(meshes);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA2DElement will handle this change
    }
  }
}
