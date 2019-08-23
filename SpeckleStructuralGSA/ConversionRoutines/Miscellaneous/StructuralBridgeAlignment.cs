using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ALIGN.1", new string[] { }, "misc", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgeAlignment : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralBridgeAlignment();

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgeAlignment();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Interface.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Type destType = typeof(GSABridgeAlignment);

      StructuralBridgeAlignment alignment = this.Value as StructuralBridgeAlignment;

      string keyword = destType.GetGSAKeyword();

      int gridSurfaceIndex = GSA.Indexer.ResolveIndex("GRID_SURFACE.1", alignment.ApplicationId);
      int gridPlaneIndex = GSA.Indexer.ResolveIndex("GRID_PLANE.4", alignment.ApplicationId);

      int index = GSA.Indexer.ResolveIndex(keyword, alignment.ApplicationId);

      var ls = new List<string>();

      ls.Clear();
      ls.AddRange(new[] {
        "SET",
        "GRID_PLANE.4" + ":" + HelperClass.GenerateSID(alignment),
        gridPlaneIndex.ToString(),
        alignment.Name == null || alignment.Name == "" ? " " : alignment.Name,
        "GENERAL", // Type
        HelperClass.SetAxis(alignment.Plane.Xdir, alignment.Plane.Ydir, alignment.Plane.Origin, alignment.Name).ToString(),
        "0", // Elevation assumed to be at local z=0 (i.e. dictated by the origin)
        "0", // Elevation above
        "0" }); // Elevation below

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
      ls.Clear();
      ls.Add("SET");
      ls.Add("GRID_SURFACE.1" + ":" + HelperClass.GenerateSID(alignment));
      ls.Add(gridSurfaceIndex.ToString());
      ls.Add(alignment.Name == null || alignment.Name == "" ? " " : alignment.Name);
      ls.Add(gridPlaneIndex.ToString());
      ls.Add("2"); // Dimension of elements to target
      ls.Add("all"); // List of elements to target
      ls.Add("0.01"); // Tolerance
      ls.Add("ONE"); // Span option
      ls.Add("0"); // Span angle
      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));


      ls.Clear();
      ls.AddRange(new []
        {
          "SET",
          keyword + ":" + HelperClass.GenerateSID(alignment),
          index.ToString(),
          string.IsNullOrEmpty(alignment.Name) ? "" : alignment.Name,
          "1", //Grid surface
          alignment.Nodes.Count().ToString(),
      });

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

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralBridgeAlignment alignment)
    {
      new GSABridgeAlignment() { Value = alignment }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSABridgeAlignment dummyObject)
    {
      var objType = dummyObject.GetType();

      if (!Initialiser.GSASenderObjects.ContainsKey(objType))
        Initialiser.GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var alignments = new List<GSABridgeAlignment>();

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSABridgeAlignment alignment = new GSABridgeAlignment() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        alignment.ParseGWACommand(Initialiser.Interface);
        alignments.Add(alignment);
      }

      Initialiser.GSASenderObjects[objType].AddRange(alignments);

      if (alignments.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
