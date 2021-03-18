using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { }, "model", true, false, new Type[] { typeof(GSA2DElement), typeof(GSA2DElementResult) }, new Type[] { typeof(GSANode), typeof(GSA2DProperty) })]
  public class GSA2DElementMesh : GSABase<Structural2DElementMesh>
  {
    public void ParseGWACommand(List<GSA2DElement> elements)
    {
      if (elements.Count() < 1)
        return;

      var obj = new Structural2DElementMesh
      {
        ApplicationId = Helper.GetApplicationId(GsaRecord.GetKeyword<GsaMemb>(), GSAId),

        Vertices = new List<double>(),
        Faces = new List<int>(),
        ElementApplicationId = new List<string>(),

        ElementType = elements.First().Value.ElementType,
        PropertyRef = elements.First().Value.PropertyRef,
        Axis = new List<StructuralAxis>(),
        Offset = new List<double>()
      };

      if (Initialiser.AppResources.Settings.Element2DResults.Count > 0 && Initialiser.AppResources.Settings.EmbedResults)
      {
        obj.Result = new Dictionary<string, object>();
      }

      var axes = obj.Axis ?? new List<StructuralAxis>();
      var offsets = obj.Offset ?? new List<double>();
      var elementAppIds = obj.ElementApplicationId ?? new List<string>();

      foreach (var e in elements)
      {
        var verticesOffset = obj.Vertices.Count() / 3;
        obj.Vertices.AddRange(e.Value.Vertices);
        obj.Faces.Add((e.Value.Faces as List<int>).First());
        obj.Faces.AddRange((e.Value.Faces as List<int>).Skip(1).Select(x => x + verticesOffset));

        
        axes.Add(e.Value.Axis);
        offsets.Add(e.Value.Offset ?? 0);
        elementAppIds.Add(e.Value.ApplicationId);

        // Result merging
        if (obj.Result != null && ((Structural2DElement)e.Value).Result != null)
        {
          try
          {
            foreach (string loadCase in e.Value.Result.Keys)
            {
              if (!obj.Result.ContainsKey(loadCase))
              {
                obj.Result[loadCase] = new Structural2DElementResult()
                {
                  Value = new Dictionary<string, object>(),
                  IsGlobal = !Initialiser.AppResources.Settings.ResultInLocalAxis,
                };
              }

              if (e.Value.Result[loadCase] is Structural2DElementResult resultExport)
              {
                foreach (var key in resultExport.Value.Keys)
                {
                  if (!(obj.Result[loadCase] as Structural2DElementResult).Value.ContainsKey(key))
                  {
                    (obj.Result[loadCase] as Structural2DElementResult).Value[key] = new Dictionary<string, object>(resultExport.Value[key] as Dictionary<string, object>);
                  }
                  else
                    foreach (var resultKey in ((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>).Keys)
                    {
                      (((obj.Result[loadCase] as Structural2DElementResult).Value[key] as Dictionary<string, object>)[resultKey] as List<double>)
                        .AddRange((resultExport.Value[key] as Dictionary<string, object>)[resultKey] as List<double>);
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
      if (obj.ApplicationId == null || Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
      {
        return "";
      }  

      var group = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA2DElementMesh).GetGSAKeyword(), obj.ApplicationId);

      var elements = obj.Explode();

      var gwaCommands = elements.Select(e => new GSA2DElement() { Value = e }.SetGWACommand(group));

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpeckleMesh inputObject)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(inputObject, () =>
      {
        var convertedObject = new Structural2DElementMesh();

        foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
        {
          var inputProperty = inputObject.GetType().GetProperty(p.Name);
          if (inputProperty != null)
            p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
        }

        return convertedObject.ToNative();
      });
    }

    public static string ToNative(this Structural2DElementMesh mesh)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(mesh, () => (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis) 
        ? new GSA2DElementMesh() { Value = mesh }.SetGWACommand() 
        : new GSA2DMember() { Value = mesh }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSA2DElementMesh dummyObject)
    {
      var meshes = new List<GSA2DElementMesh>();
      var typeName = dummyObject.GetType().Name;
      var keyword = dummyObject.GetGSAKeyword();

      // Perform mesh merging
      var uniqueMembers = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>().Select(x => x.Member).Where(m => m > 0).Distinct().ToList();

      //This loop has been left as serial for now, considering the fact that the sender objects are retrieved and removed-from with each iteration
      foreach (var member in uniqueMembers)
      {
        try
        {
          var all2dElements = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>();
          var matching2dElementList = all2dElements.Where(x => x.Member == member).Cast<GSA2DElement>().ToList();
          var mesh = new GSA2DElementMesh() { GSAId = Convert.ToInt32(member) };
          mesh.ParseGWACommand(matching2dElementList);
          meshes.Add(mesh);

          Initialiser.GsaKit.GSASenderObjects.RemoveAll(matching2dElementList);
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + member);
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(meshes);

      return new SpeckleNull(); // Return null because ToSpeckle method for GSA2DElement will handle this change
    }
  }
}
