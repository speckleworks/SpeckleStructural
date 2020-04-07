using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_SURFACE.1", new string[] { "POLYLINE.1", "GRID_PLANE.4", "AXIS.1" }, "loads", true, true, new Type[] { }, new Type[] { })]
  public class GSALoadPlane : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadPlane();

    public void ParseGWACommand()
    {
      //TO DO
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var plane = this.Value as StructuralLoadPlane;

      var keyword = typeof(GSALoadPlane).GetGSAKeyword();
      var gridPlaneKeyword = "GRID_PLANE.4";
      var gridPlaneIndex = Initialiser.Cache.ResolveIndex(gridPlaneKeyword);

      var index = Initialiser.Cache.ResolveIndex(typeof(GSALoadPlane).GetGSAKeyword(), typeof(GSALoadPlane).Name);

      var ls = new List<string>();

      var gwaCommands = new List<string>();

      Helper.SetAxis(this.Value, out int planeAxisIndex, out string planeAxisGwa, plane.Name);
      if (planeAxisGwa.Length > 0)
      {
        gwaCommands.Add(planeAxisGwa);
      }

      ls.Clear();

      ls.AddRange(new[] {
        "SET",
        gridPlaneKeyword,
        gridPlaneIndex.ToString(),
        (string.IsNullOrEmpty(plane.Name)) ? " " : plane.Name,
        "GENERAL", // Type
        planeAxisIndex.ToString(),
        "0", // Elevation
        "0", // Elevation above
        (plane.SpanAngle ?? 0).ToString()
      }); // Elevation below

      gwaCommands.Add(string.Join("\t", ls));

      ls.Clear();

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
      return new GSALoadPlane() { Value = plane }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSALoadPlane dummyObject)
    {
      var newLines = ToSpeckleBase<GSALoadPlane>();

      var planes = new List<GSALoadPlane>();

      foreach (var p in newLines.Values)
      {
        var plane = new GSALoadPlane() { GWACommand = p };
        plane.ParseGWACommand();
        planes.Add(plane);
      }

      Initialiser.GSASenderObjects[typeof(GSALoadPlane)].AddRange(planes);

      return (planes.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
