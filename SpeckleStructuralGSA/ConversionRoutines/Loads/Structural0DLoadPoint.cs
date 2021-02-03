using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRID_POINT.2", new string[] { "NODE.3", "AXIS.1" }, "model", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSAGridSurface), typeof(GSAStorey), typeof(GSALoadCase), typeof(GSANode) })]
  public class GSA0DLoadPoint : GSABase<Structural0DLoadPoint>
  {
    public int Axis; // Store this temporarily to generate other loads

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DLoadPoint();
      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : Convert.ToInt32(axis);

      //Helper.GetGridPlaneRef(Convert.ToInt32(pieces[counter++]), out int gridPlaneRefRet, out string gridSurfaceRec);
      //Helper.GetGridPlaneData(gridPlaneRefRet, out int gridPlaneAxis, out double gridPlaneElevation, out string gridPlaneRec);

      //this.SubGWACommand.Add(gridSurfaceRec);
      //this.SubGWACommand.Add(gridPlaneRec);

      //string gwaRec = null;
      //var planeAxis = Helper.Parse0DAxis(gridPlaneAxis, Initialiser.Instance.Interface, out gwaRec);
      //if (gwaRec != null)
      //  this.SubGWACommand.Add(gwaRec);
      //double elevation = gridPlaneElevation;

      //var planeLoadAxisId = 0;
      //var planeLoadAxisData = pieces[counter++];
      //StructuralAxis planeLoadAxis;
      //if (planeLoadAxisData == "LOCAL")
      //  planeLoadAxis = planeAxis;
      //else
      //{
      //  planeLoadAxisId = planeLoadAxisData == "GLOBAL" ? 0 : Convert.ToInt32(planeLoadAxisData);
      //  planeLoadAxis = Helper.Parse0DAxis(planeLoadAxisId, Initialiser.Instance.Interface, out gwaRec);
      //  if (gwaRec != null)
      //    this.SubGWACommand.Add(gwaRec);
      //}
      //var planeProjected = pieces[counter++] == "YES";
      //var planeDirection = pieces[counter++];
      //var value = Convert.ToDouble(pieces[counter++]);


      obj.Loading = new StructuralVectorSix(new double[3]);

      var direction = pieces[counter++].ToLower();
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Y":
          obj.Loading.Value[1] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Z":
          obj.Loading.Value[2] = Convert.ToDouble(pieces[counter++]);
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {

      if (this.Value == null)
        return "";

      var load = this.Value as Structural0DLoadPoint;
      if (load.ApplicationId == null)
        return "";

      var keyword = typeof(GSA0DLoadPoint).GetGSAKeyword();
      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var loadPlaneKeyword = typeof(GSAGridSurface).GetGSAKeyword();

      var axisRef = "GLOBAL";
      int loadCaseRef;

      var gwaCommands = new List<string>();

      var ls = new List<string>();

      try
      {
        loadCaseRef = Initialiser.AppResources.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef).Value;
      }
      catch
      {
        loadCaseRef = Initialiser.AppResources.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);
      }

      int gridSurfaceIndex;
      try
      {
        gridSurfaceIndex = Initialiser.AppResources.Cache.LookupIndex(loadPlaneKeyword, load.LoadPlaneRef).Value;
      }
      catch
      {
        gridSurfaceIndex = Initialiser.AppResources.Cache.ResolveIndex(loadPlaneKeyword, load.LoadPlaneRef);
      }

      double x = 0;
      double y = 0;
      if (gridSurfaceIndex > 0)
      {
        var loadPlanes = Initialiser.AppResources.Cache.GetIndicesSpeckleObjects(typeof(StructuralLoadPlane).Name);
        var loadPlane = ((StructuralLoadPlane)loadPlanes[gridSurfaceIndex]).Axis;

        //Now that load planes are shared, and a new surface and axis aren't created for each point where the point
        //is arranged to be at the origin of the new surface and axis, there might need to be an X and Y coordinate calculated 
        //that is relative to the plane
        var axisCoords = Helper.MapPointsGlobal2Local(load.LoadPoint.Value.ToArray(), loadPlane);
        x = axisCoords[0];
        y = axisCoords[1];
      }

      ls.Clear();

      var direction = new string[3] { "X", "Y", "Z" };

      for (var i = 0; i < Math.Min(direction.Count(), load.Loading.Value.Count()) ; i++)
      {
        ls.Clear();
        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA0DLoadPoint).GetGSAKeyword());

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        var sid = Helper.GenerateSID(load);
        ls.Add(keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name + (load.Name.All(char.IsDigit) ? " " : ""));
        ls.Add(gridSurfaceIndex.ToString()); // Grid Surface
        ls.Add(x.ToString()); // X coordinate
        ls.Add(y.ToString()); // Y coordinate
        ls.Add(loadCaseRef.ToString());
        ls.Add(axisRef); // Axis
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      }
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DLoadPoint load)
    {
      return new GSA0DLoadPoint() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA0DLoadPoint dummyObject)
    {
      var newPoints = ToSpeckleBase<GSA0DLoadPoint>();

      var loads = new List<GSA0DLoadPoint>();

      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      var typeName = dummyObject.GetType().Name;
      foreach (var k in newPoints.Keys)
      {
        var p = newPoints[k];
        var loadSubList = new List<GSA0DLoadPoint>();

        // Placeholder load object to get list of nodes and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA0DLoadPoint() { GWACommand = p, GSAId = k };
        try
        {
          initLoad.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString());
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }

        loads.AddRange(loadSubList);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
