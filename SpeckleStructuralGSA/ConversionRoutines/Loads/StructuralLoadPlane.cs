using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_SURFACE.1", new string[] { "POLYLINE.1", "GRID_PLANE.4", "AXIS.1" }, "loads", true, true, new Type[] { }, new Type[] { typeof(GSAStorey) })]
  public class GSAGridSurface : GSAGridPlaneBase, IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadPlane();

    public bool ParseGWACommand()
    {
      //TO DO
      return false;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var plane = this.Value as StructuralLoadPlane;
      if (plane.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSAGridSurface).GetGSAKeyword();
      var index = Initialiser.Cache.ResolveIndex(keyword);

      int gridPlaneIndex;

      var gwaCommands = new List<string>();

      if (plane.Axis != null)
      {
        gwaCommands.AddRange(SetAxisPlaneGWACommands(plane.Axis, plane.Name, out gridPlaneIndex));
      }
      else if (plane.Axis == null && !string.IsNullOrEmpty(plane.StoreyRef))
      {
        gridPlaneIndex = Initialiser.Cache.ResolveIndex(typeof(GSAStorey).GetGSAKeyword(), plane.StoreyRef);
      }
      else
      {
        return "";
      }

      var ls = new List<string>();

      ls.AddRange(new[] {"SET",
        keyword + ":" + Helper.GenerateSID(plane),
        index.ToString(),
        plane.Name == null || plane.Name == "" ? " " : plane.Name,
        gridPlaneIndex.ToString(),
        (plane.ElementDimension ?? 1).ToString() , // Dimension of elements to target
        "all", // List of elements to target
        (plane.Tolerance ?? 0.01).ToString(), // Tolerance
        (plane.Span == null || plane.Span == 2) ? "TWO_SIMPLE" : "ONE", // Span option
        (plane.SpanAngle ?? 0).ToString()}); // Span angle

      gwaCommands.Add(string.Join("\t", ls));

      return string.Join("\n", gwaCommands);
    }

  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadPlane plane)
    {
      return new GSAGridSurface() { Value = plane }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGridSurface dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridSurface>();

      var planes = new List<GSAGridSurface>();

      foreach (var p in newLines.Values)
      {
        var plane = new GSAGridSurface() { GWACommand = p };
        if (plane.ParseGWACommand())
        {
          planes.Add(plane);
        }
      }

      Initialiser.GSASenderObjects.AddRange(planes);

      return (planes.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
