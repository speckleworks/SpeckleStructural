using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_AREA.2", new string[] { "POLYLINE.1", "GRID_SURFACE.1", "GRID_PLANE.4", "AXIS.1" }, "elements", true, true, new Type[] { }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGridAreaLoad : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DLoadPanel();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DLoadPanel();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);

      Helper.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]), out int gridPlaneRefRet, out string gridSurfaceRec);
      Helper.GetGridPlaneData(gridPlaneRefRet, out int gridPlaneAxis, out double gridPlaneElevation, out string gridPlaneRec);

      this.SubGWACommand.Add(gridSurfaceRec);
      this.SubGWACommand.Add(gridPlaneRec);

      string gwaRec = null;
      var axis = Helper.Parse0DAxis(gridPlaneAxis, Initialiser.Interface, out gwaRec);
      if (gwaRec != null)
        this.SubGWACommand.Add(gwaRec);
      double elevation = gridPlaneElevation;

      var polylineDescription = "";

      switch (pieces[counter++])
      {
        case "PLANE":
          // TODO: Do not handle for now
          return;
        case "POLYREF":
          var polylineRef = pieces[counter++];
          string newRec = null;
         Helper.GetPolylineDesc(Convert.ToInt32(polylineRef), out polylineDescription, out newRec);
          this.SubGWACommand.Add(newRec);
          break;
        case "POLYGON":
          polylineDescription = pieces[counter++];
          break;
      }
      var polyVals = Helper.ParsePolylineDesc(polylineDescription);

      for (var i = 2; i < polyVals.Length; i += 3)
        polyVals[i] = elevation;

      obj.Value = Helper.MapPointsLocal2Global(polyVals, axis).ToList();
      obj.Closed = true;

      var loadCaseIndex = Convert.ToInt32(pieces[counter++]);
      if (loadCaseIndex > 0)
      {
        obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), loadCaseIndex);
      }

      var loadAxisId = 0;
      var loadAxisData = pieces[counter++];
      StructuralAxis loadAxis;
      if (loadAxisData == "LOCAL")
        loadAxis = axis;
      else
      {
        loadAxisId = loadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(loadAxisData);
        loadAxis = Helper.Parse0DAxis(loadAxisId, Initialiser.Interface, out gwaRec);
        if (gwaRec != null)
          this.SubGWACommand.Add(gwaRec);
      }
      var projected = pieces[counter++] == "YES";
      var direction = pieces[counter++];
      var value = Convert.ToDouble(pieces[counter++]);

      obj.Loading = new StructuralVectorThree(new double[3]);
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = value;
          break;
        case "Y":
          obj.Loading.Value[1] = value;
          break;
        case "Z":
          obj.Loading.Value[2] = value;
          break;
        default:
          // TODO: Error case maybe?
          break;
      }
      obj.Loading.TransformOntoAxis(loadAxis);

      if (projected)
      {
        var scale = (obj.Loading.Value[0] * axis.Normal.Value[0] +
            obj.Loading.Value[1] * axis.Normal.Value[1] +
            obj.Loading.Value[2] * axis.Normal.Value[2]) /
            (axis.Normal.Value[0] * axis.Normal.Value[0] +
            axis.Normal.Value[1] * axis.Normal.Value[1] +
            axis.Normal.Value[2] * axis.Normal.Value[2]);

        obj.Loading = new StructuralVectorThree(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);
        obj.Loading.Scale(scale);
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural2DLoadPanel;

      if (load.ApplicationId == null)
        return "";

      var keyword = typeof(GSAGridAreaLoad).GetGSAKeyword();

      //There are no GSA types for these yet so use empty strings for the type names for the index
      var gridSurfaceIndex = Initialiser.Cache.ResolveIndex("GRID_SURFACE.1");
      var gridPlaneIndex = Initialiser.Cache.ResolveIndex("GRID_PLANE.4");

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

      if (indexResult == null && load.ApplicationId != null)
      {
        if (load.LoadCaseRef == null)
        {
          Helper.SafeDisplay("Blank load case references found for these Application IDs:", load.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Load case references not found:", load.ApplicationId + " referencing " + load.LoadCaseRef);
        }
      }

      var axis = Helper.Parse2DAxis(load.Value.ToArray());

      // Calculate elevation
      var elevation = (load.Value[0] * axis.Normal.Value[0] +
          load.Value[1] * axis.Normal.Value[1] +
          load.Value[2] * axis.Normal.Value[2]) /
          Math.Sqrt(axis.Normal.Value[0] * axis.Normal.Value[0] +
              axis.Normal.Value[1] * axis.Normal.Value[1] +
              axis.Normal.Value[2] * axis.Normal.Value[2]);

      // Transform coordinate to new axis
      var transformed = Helper.MapPointsGlobal2Local(load.Value.ToArray(), axis);

      var ls = new List<string>();

      var direction = new string[3] { "X", "Y", "Z" };

      var gwaCommands = new List<string>();

      
      Helper.SetAxis(axis, out int planeAxisIndex, out string planeAxisGwa, load.Name);
      if (planeAxisGwa.Length > 0)
      {
        gwaCommands.Add(planeAxisGwa);
      }

      ls.AddRange(new[] {
        "SET",
        "GRID_PLANE.4",
        gridPlaneIndex.ToString(),
        load.Name == null || load.Name == "" ? " " : load.Name,
        "GENERAL", // Type
        planeAxisIndex.ToString(),
        elevation.ToString(),
        "0", // Elevation above
        "0"}); // Elevation below
      gwaCommands.Add(string.Join("\t", ls));

      ls.Clear();
      ls.AddRange(new[] {
        "SET",
        "GRID_SURFACE.1",
        gridSurfaceIndex.ToString(),
        load.Name == null || load.Name == "" ? " " : load.Name,
        gridPlaneIndex.ToString(),
        "2", // Dimension of elements to target
        "all", // List of elements to target
        "0.01", // Tolerance
        "ONE", // Span option
        "0"}); // Span angle
      gwaCommands.Add(string.Join("\t", ls));

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Cache.ResolveIndex(typeof(GSAGridAreaLoad).GetGSAKeyword());

        ls.Clear();
        var subLs = new List<string>();
        for (var j = 0; j < transformed.Count(); j += 3)
        {
          //The GWA that GSA generates seems to return a rounded number, so do so here
          subLs.Add("(" + Math.Round(transformed[j],4).ToString() + "," + Math.Round(transformed[j + 1],4).ToString() + ")");
        }

        ls.AddRange(new string[] { 
          "SET_AT",
          index.ToString(),
          keyword + ":" + Helper.GenerateSID(load),
          load.Name == null || load.Name == "" ? " " : load.Name,
          gridSurfaceIndex.ToString(),
          "POLYGON",
          "\"" + string.Join(" ", subLs) + "(m)\"",
          loadCaseRef.ToString(),
          "GLOBAL",
          "NO",
          direction[i],
          load.Loading.Value[i].ToString() });

        gwaCommands.Add(string.Join("\t", ls));
      }

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DLoadPanel load)
    {
      return new GSAGridAreaLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGridAreaLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridAreaLoad>();
      var loads = new List<GSAGridAreaLoad>();

      foreach (var k in newLines.Keys)
      {
        var load = new GSAGridAreaLoad() { GSAId = k, GWACommand = newLines[k] };
        load.ParseGWACommand();
        loads.Add(load);
      }

      Initialiser.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
