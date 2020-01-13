using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_POINT.2", new string[] { "GRID_SURFACE.1", "AXIS.1" }, "loads", true, true, new Type[] { }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGridPointLoad : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoadPoint();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DLoadPoint();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      obj.Value = new List<double>
      {
        Convert.ToDouble(pieces[counter++]),
        Convert.ToDouble(pieces[counter++]),
        0
      };

      HelperClass.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]), out int gridPlaneRefRet, out string gridSurfaceRec);
      HelperClass.GetGridPlaneData(gridPlaneRefRet, out int gridPlaneAxis, out double gridPlaneElevation, out string gridPlaneRec);
      this.SubGWACommand.Add(gridSurfaceRec);
      this.SubGWACommand.Add(gridPlaneRec);

      string gwaRec = null;
      var axis = HelperClass.Parse0DAxis(gridPlaneAxis, Initialiser.Interface, out gwaRec);
      if (gwaRec != null)
        this.SubGWACommand.Add(gwaRec);

      obj.LoadCaseRef = HelperClass.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var loadAxisId = 0;
      var loadAxisData = pieces[counter++];
      StructuralAxis loadAxis;
      if (loadAxisData == "LOCAL")
        loadAxis = axis;
      else
      {
        loadAxisId = loadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(loadAxisData);
        loadAxis = HelperClass.Parse0DAxis(loadAxisId, Initialiser.Interface, out gwaRec);
        if (gwaRec != null)
          this.SubGWACommand.Add(gwaRec);
      }
      var direction = pieces[counter++];
      var loadingValue = Convert.ToDouble(pieces[counter++]);

      obj.Loading = new StructuralVectorThree(new double[3]);
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = loadingValue;
          break;
        case "Y":
          obj.Loading.Value[1] = loadingValue;
          break;
        case "Z":
          obj.Loading.Value[2] = loadingValue;
          break;
        default:
          // TODO: Error case maybe?
          break;
      }
      obj.Loading.TransformOntoAxis(loadAxis);

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural0DLoadPoint;

      if (load.Loading == null)
        return "";

      var keyword = typeof(GSAGridPointLoad).GetGSAKeyword();

      //There are no GSA types for these yet, so use empty strings for the type names
      var gridSurfaceIndex = Initialiser.Cache.ResolveIndex("GRID_SURFACE.1");
      var gridPlaneIndex = Initialiser.Cache.ResolveIndex("GRID_PLANE.4");

      var loadCaseRef = 0;
      try
      {
        loadCaseRef = Initialiser.Cache.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef).Value;
      }
      catch
      {
        loadCaseRef = Initialiser.Cache.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
      }

      //var axis = GSA.Parse1DAxis(load.Value.ToArray());
      var axis = HelperClass.Parse1DAxis(load.Value.ToArray());

      // Calculate elevation
      var elevation = (load.Value[0] * axis.Normal.Value[0] +
          load.Value[1] * axis.Normal.Value[1] +
          load.Value[2] * axis.Normal.Value[2]) /
          Math.Sqrt(axis.Normal.Value[0] * axis.Normal.Value[0] +
              axis.Normal.Value[1] * axis.Normal.Value[1] +
              axis.Normal.Value[2] * axis.Normal.Value[2]);

      // Transform coordinate to new axis
      var transformed = HelperClass.MapPointsGlobal2Local(load.Value.ToArray(), axis);

      var ls = new List<string>();

      var direction = new string[3] { "X", "Y", "Z" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        if (load.Loading.Value[i] == 0) continue;

        var subLs = new List<string>();
        for (var j = 0; j < transformed.Count(); j += 3)
        {
          subLs.Add("(" + transformed[j].ToString() + "," + transformed[j + 1].ToString() + ")");
        }

        ls.Clear();

        var index = Initialiser.Cache.ResolveIndex(typeof(GSAGridPointLoad).GetGSAKeyword(), typeof(GSAGridPointLoad).Name);

        ls.AddRange(new[] {
          "SET_AT",
          index.ToString(),
          keyword + ":" + HelperClass.GenerateSID(load),
          load.Name == null || load.Name == "" ? " " : load.Name,
          gridSurfaceIndex.ToString(),
          "POLYGON",
          string.Join(" ", subLs),
          loadCaseRef.ToString(),
          "GLOBAL",
          "NO",
          direction[i],
          load.Loading.Value[i].ToString(),
          load.Loading.Value[i].ToString()});

        gwaCommands.Add(string.Join("\t", ls));
      }

      ls.Clear();
      ls.AddRange(new[] {"SET",
        "GRID_SURFACE.1",
        gridSurfaceIndex.ToString(),
        load.Name == null || load.Name == "" ? " " : load.Name,
        gridPlaneIndex.ToString(),
        "1", // Dimension of elements to target
        "all", // List of elements to target
        "0.01", // Tolerance
        "TWO_SIMPLE", // Span option
        "0"}); // Span angle
      gwaCommands.Add(string.Join("\t", ls));

      ls.Clear();

      HelperClass.SetAxis(axis, out int planeAxisIndex, out string planeAxisGwa, load.Name);
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

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DLoadPoint load)
    {
      return new GSAGridPointLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGridPointLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridPointLoad>();

      var loads = new List<GSAGridPointLoad>();

      foreach (var p in newLines.Values)
      {
        var load = new GSAGridPointLoad() { GWACommand = p };
        load.ParseGWACommand();
        loads.Add(load);
      }

      Initialiser.GSASenderObjects[typeof(GSAGridPointLoad)].AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
