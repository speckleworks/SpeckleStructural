using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_LINE.2", new string[] { "POLYLINE.1", "GRID_SURFACE.1", "GRID_PLANE.4", "AXIS.1" }, "model", true, true, new Type[] { }, new Type[] { typeof(GSAGridSurface), typeof(GSAStorey), typeof(GSALoadCase) })]
  public class GSAGridLineLoad : GSABase<Structural1DLoadLine>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DLoadLine();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      Helper.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]), out int gridPlaneRefRet, out string gridSurfaceRec);
      Helper.GetGridPlaneData(gridPlaneRefRet, out int gridPlaneAxis, out double gridPlaneElevation, out string gridPlaneRec);
      this.SubGWACommand.Add(gridSurfaceRec);
      this.SubGWACommand.Add(gridPlaneRec);

      string gwaRec = null;
      var axis = Helper.Parse0DAxis(gridPlaneAxis, out gwaRec);
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

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var loadAxisId = 0;
      var loadAxisData = pieces[counter++];
      StructuralAxis loadAxis;
      if (loadAxisData == "LOCAL")
        loadAxis = axis;
      else
      {
        loadAxisId = loadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(loadAxisData);
        loadAxis = Helper.Parse0DAxis(loadAxisId, out gwaRec);
        if (gwaRec != null)
          this.SubGWACommand.Add(gwaRec);
      }
      var projected = pieces[counter++] == "YES";
      var direction = pieces[counter++];
      var firstValue = Convert.ToDouble(pieces[counter++]);
      var secondValue = Convert.ToDouble(pieces[counter++]);
      var averageValue = (firstValue + secondValue) / 2;

      obj.Loading = new StructuralVectorSix(new double[6]);
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = averageValue;
          break;
        case "Y":
          obj.Loading.Value[1] = averageValue;
          break;
        case "Z":
          obj.Loading.Value[2] = averageValue;
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

        obj.Loading = new StructuralVectorSix(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2], 0, 0, 0);
        obj.Loading.Scale(scale);
      }

      obj.LoadingEnd = new StructuralVectorSix(new double[6]);
      switch (direction.ToUpper())
      {
        case "X":
          obj.LoadingEnd.Value[0] = averageValue;
          break;
        case "Y":
          obj.LoadingEnd.Value[1] = averageValue;
          break;
        case "Z":
          obj.LoadingEnd.Value[2] = averageValue;
          break;
      }
      obj.LoadingEnd.TransformOntoAxis(loadAxis);

      if (projected)
      {
        var scale = (obj.LoadingEnd.Value[0] * axis.Normal.Value[0] +
            obj.LoadingEnd.Value[1] * axis.Normal.Value[1] +
            obj.LoadingEnd.Value[2] * axis.Normal.Value[2]) /
            (axis.Normal.Value[0] * axis.Normal.Value[0] +
            axis.Normal.Value[1] * axis.Normal.Value[1] +
            axis.Normal.Value[2] * axis.Normal.Value[2]);

        obj.LoadingEnd = new StructuralVectorSix(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2], 0, 0, 0);
        obj.LoadingEnd.Scale(scale);
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural1DLoadLine;

      if (load.ApplicationId == null)
        return "";

      var keyword = typeof(GSAGridLineLoad).GetGSAKeyword();

      //There are no GSA types for these yet, so use empty strings for the type names

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();

      var indexResult = Initialiser.AppResources.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);
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

      StructuralAxis axis = null;
      var ls = new List<string>();
      var gwaCommands = new List<string>();
      int gridSurfaceIndex;

      double elevation = 0;

      if (string.IsNullOrEmpty(load.LoadPlaneRef))
      {
        axis = (load.Value == null)
          ? new StructuralAxis(new StructuralVectorThree(1, 0, 0), new StructuralVectorThree(0, 1, 0))
          : Helper.Parse1DAxis(load.Value.ToArray());

        Helper.SetAxis(axis, out int planeAxisIndex, out string planeAxisGwa, load.Name);
        if (planeAxisGwa.Length > 0)
        {
          gwaCommands.Add(planeAxisGwa);
        }

        if (load.Value != null)
        {
          // Calculate elevation
          elevation = (load.Value[0] * axis.Normal.Value[0] +
              load.Value[1] * axis.Normal.Value[1] +
              load.Value[2] * axis.Normal.Value[2]) /
              Math.Sqrt(axis.Normal.Value[0] * axis.Normal.Value[0] +
                  axis.Normal.Value[1] * axis.Normal.Value[1] +
                  axis.Normal.Value[2] * axis.Normal.Value[2]);
        }


        gridSurfaceIndex = Initialiser.AppResources.Cache.ResolveIndex("GRID_SURFACE.1");
        var gridPlaneIndex = Initialiser.AppResources.Cache.ResolveIndex("GRID_PLANE.4");

        ls.Clear();
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
        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

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
        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
      }
      else //LoadPlaneRef is not empty/null
      {
        try
        {
          gridSurfaceIndex = Initialiser.AppResources.Cache.LookupIndex("GRID_SURFACE.1", load.LoadPlaneRef).Value;
        }
        catch
        {
          gridSurfaceIndex = Initialiser.AppResources.Cache.ResolveIndex("GRID_SURFACE.1", load.LoadPlaneRef);
        }

        var loadPlanesDict = Initialiser.AppResources.Cache.GetIndicesSpeckleObjects(typeof(StructuralLoadPlane).Name);
        if (loadPlanesDict.ContainsKey(gridSurfaceIndex) && loadPlanesDict[gridSurfaceIndex] != null)
        {
          var loadPlane = ((StructuralLoadPlane)loadPlanesDict[gridSurfaceIndex]);
          if (loadPlane.Axis != null)
          {
            axis = loadPlane.Axis;
          }
          else
          {
            try
            {
              var storeyIndex = Initialiser.AppResources.Cache.LookupIndex("GRID_PLANE.4", loadPlane.StoreyRef).Value;
              var storeysDict = Initialiser.AppResources.Cache.GetIndicesSpeckleObjects(typeof(StructuralStorey).Name);
              if (storeysDict.ContainsKey(storeyIndex) && storeysDict[storeyIndex] != null)
              {
                var storey = ((StructuralStorey)storeysDict[storeyIndex]);
                if (storey.Axis != null)
                {
                  axis = storey.Axis;
                }
              }
            }
            catch { }

            if (axis == null)
            {
              axis = new StructuralAxis(new StructuralVectorThree(1, 0, 0), new StructuralVectorThree(0, 1, 0));
            }
          }
        }
      }

      // Transform coordinate to new axis
      var transformed = Helper.MapPointsGlobal2Local(load.Value.ToArray(), axis);

      var direction = new string[3] { "X", "Y", "Z" };

      for (var i = 0; i < Math.Min(direction.Count(), load.Loading.Value.Count()); i++)
      {
        if (load.Loading.Value[i] == 0) continue;

        var subLs = new List<string>();
        for (var j = 0; j < transformed.Count(); j += 3)
        {
          subLs.Add("(" + transformed[j].ToString() + "," + transformed[j + 1].ToString() + ")");
        }

        ls.Clear();

        var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSAGridLineLoad).GetGSAKeyword());

        var sid = Helper.GenerateSID(load);
        ls.AddRange(new[] {
          "SET_AT",
          index.ToString(),
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          load.Name == null || load.Name == "" ? " " : load.Name + (load.Name.All(char.IsDigit) ? " " : ""),
          gridSurfaceIndex.ToString(),
          "POLYGON",
          string.Join(" ", subLs),
          loadCaseRef.ToString(),
          "GLOBAL",
          "NO",
          direction[i],
          load.Loading.Value[i].ToString(),
          load.LoadingEnd == null ? load.Loading.Value[i].ToString() : load.LoadingEnd.Value[i].ToString()});

        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
      }

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural1DLoadLine load)
    {
      return new GSAGridLineLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGridLineLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridLineLoad>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSAGridLineLoad>();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var load = new GSAGridLineLoad() { GWACommand = p };
        try
        {
          load.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }

        // Break them apart
        for (var i = 0; i < load.Value.Value.Count - 3; i += 3)
        {
          var actualLoad = new GSAGridLineLoad() {
            GWACommand = load.GWACommand,
            SubGWACommand = new List<string>(load.SubGWACommand.ToArray()),
            Value = new Structural1DLoadLine()
            {
              Name = load.Value.Name,
              Value = (load.Value.Value as List<double>).Skip(i).Take(6).ToList(),
              Loading = load.Value.Loading,
              LoadCaseRef = load.Value.LoadCaseRef
            }
          };

          loads.Add(actualLoad);
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
