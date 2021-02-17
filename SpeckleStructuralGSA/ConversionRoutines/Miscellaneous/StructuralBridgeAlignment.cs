using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ALIGN.1", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgeAlignment : GSABase<StructuralBridgeAlignment>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgeAlignment();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSABridgeAlignment);

      var alignment = this.Value as StructuralBridgeAlignment;
      if (alignment.ApplicationId == null)
      {
        return "";
      }

      var keyword = destType.GetGSAKeyword();

      var gridSurfaceIndex = Initialiser.AppResources.Cache.ResolveIndex("GRID_SURFACE.1");
      var gridPlaneIndex = Initialiser.AppResources.Cache.ResolveIndex("GRID_PLANE.4");

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, alignment.ApplicationId);

      var sid = Helper.GenerateSID(alignment);

      var gwaCommands = new List<string>();

      var ls = new List<string>();

      int axisIndex = 1;
      if (alignment.Plane != null)
      {
        var axis = new StructuralAxis() { Xdir = alignment.Plane.Xdir, Ydir = alignment.Plane.Ydir, Origin = alignment.Plane.Origin };
        axis.Normal = alignment.Plane.Normal ?? CrossProduct(alignment.Plane.Xdir, alignment.Plane.Ydir);

        Helper.SetAxis(axis, out axisIndex, out var axisGwa, alignment.Name);
        if (axisGwa.Length > 0)
        {
          gwaCommands.Add(axisGwa);
        }
      }

      ls.Clear();
      ls.AddRange(new[] {
        "SET",
        "GRID_PLANE.4",
        gridPlaneIndex.ToString(),
        alignment.Name == null || alignment.Name == "" ? " " : alignment.Name,
        "GENERAL", // Type
        axisIndex.ToString(),
        "0", // Elevation assumed to be at local z=0 (i.e. dictated by the origin)
        "0", // Elevation above
        "0" }); // Elevation below

      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      ls.Clear();
      ls.AddRange(new[] {
        "SET",
        "GRID_SURFACE.1",
        gridSurfaceIndex.ToString(),
        alignment.Name == null || alignment.Name == "" ? " " : alignment.Name,
        gridPlaneIndex.ToString(),
        "2", // Dimension of elements to target
        "all", // List of elements to target
        "0.01", // Tolerance
        "ONE", // Span option
        "0"}); // Span angle
      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));


      ls.Clear();
      ls.AddRange(new []
        {
          "SET",
          keyword + ":" + sid,
          index.ToString(),
          string.IsNullOrEmpty(alignment.Name) ? "" : alignment.Name,
          "1", //Grid surface
          (alignment.Nodes == null ? 0 : alignment.Nodes.Count()).ToString(),
      });

      if (alignment.Nodes != null)
      {
        foreach (var node in alignment.Nodes)
        {
          ls.Add(node.Chainage.ToString());
          if (node.Curvature == StructuralBridgeCurvature.Straight)
          {
            ls.Add("0");
          }
          else
          {
            ls.Add(((1d / node.Radius) * ((node.Curvature == StructuralBridgeCurvature.RightCurve) ? 1 : -1)).ToString());
          }
        }
        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
      }

      return string.Join("\n", gwaCommands);
    }

    private SpeckleVector CrossProduct(SpeckleVector v1, SpeckleVector v2)
    {
      double x, y, z;
      x = v1.Value[1] * v2.Value[2] - v2.Value[1] * v1.Value[2];
      y = (v1.Value[0] * v2.Value[2] - v2.Value[0] * v1.Value[2]) * -1;
      z = v1.Value[0] * v2.Value[1] - v2.Value[0] * v1.Value[1];

      return new SpeckleVector(x, y, z);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralBridgeAlignment alignment)
    {
      return new GSABridgeAlignment() { Value = alignment }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSABridgeAlignment dummyObject)
    {
      var newLines = ToSpeckleBase<GSABridgeAlignment>();
      var typeName = dummyObject.GetType().Name;
      var alignmentsLock = new object();

      //Get all relevant GSA entities in this entire model
      var alignments = new SortedDictionary<int, GSABridgeAlignment>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var alignment = new GSABridgeAlignment() { GWACommand = newLines[k] };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        try
        {
          alignment.ParseGWACommand();
          lock (alignmentsLock)
          {
            alignments.Add(k, alignment);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(alignments.Values.ToList());

      return (alignments.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
