using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_AREA.2", new string[] { "POLYLINE.1", "GRID_SURFACE.1", "GRID_PLANE.4", "AXIS.1" }, "model", true, true, new Type[] { }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGridAreaLoad : GSABase<Structural2DLoadPanel>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DLoadPanel();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);

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
        loadAxis = Helper.Parse0DAxis(loadAxisId, out gwaRec);
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
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSAGridAreaLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridAreaLoad>();
      var loads = new List<GSAGridAreaLoad>();
      var typeName = dummyObject.GetType().Name;

      foreach (var k in newLines.Keys)
      {
        var load = new GSAGridAreaLoad() { GSAId = k, GWACommand = newLines[k] };
        try
        {
          load.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }

        loads.Add(load);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
