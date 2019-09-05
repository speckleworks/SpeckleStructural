using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PATH.1", new string[] { "ALIGN.1" }, "misc", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgePath
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralBridgePath();

    public void ParseGWACommand(GSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgePath();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Type destType = typeof(GSABridgePath);

      var path = this.Value as StructuralBridgePath;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(destType, path);
      int alignmentIndex = GSA.Indexer.LookupIndex(typeof(GSABridgeAlignment), path.AlignmentRef) ?? 1;

      var left = path.Offsets.First();
      var right = (path.PathType == StructuralBridgePathType.Track || path.PathType == StructuralBridgePathType.Vehicle) ? path.Gauge : path.Offsets.Last();

      var ls = new List<string>
        {
          "SET",
          keyword + ":" + GSA.GenerateSID(path),
          index.ToString(),
          string.IsNullOrEmpty(path.Name) ? "" : path.Name,
          PathTypeToGWAString(path.PathType),
          "1", //Group
          alignmentIndex.ToString(),
          left.ToString(),
          right.ToString(),
          path.LeftRailFactor.ToString()
      };

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private string PathTypeToGWAString(StructuralBridgePathType pathType)
    {
      switch (pathType)
      {
        case StructuralBridgePathType.Carriage1Way: return "CWAY_1WAY";
        case StructuralBridgePathType.Carriage2Way: return "CWAY_2WAY";
        case StructuralBridgePathType.Footway: return "FOOTWAY";
        case StructuralBridgePathType.Lane: return "LANE";
        case StructuralBridgePathType.Vehicle: return "VEHICLE";
        default: return "TRACK";
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralBridgePath path)
    {
      new GSABridgePath() { Value = path }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSABridgePath dummyObject)
    {
      var objType = dummyObject.GetType();

      if (!GSASenderObjects.ContainsKey(objType))
        GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var paths = new List<GSABridgePath>();

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSABridgePath path = new GSABridgePath() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        path.ParseGWACommand(GSA);
        paths.Add(path);
      }

      GSASenderObjects[objType].AddRange(paths);

      if (paths.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
