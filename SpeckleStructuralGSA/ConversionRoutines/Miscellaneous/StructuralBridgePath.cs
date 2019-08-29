using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAConversion("PATH.1", new string[] { "ALIGN.1" }, "misc", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgePath
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralBridgePath();

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgePath();

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

      Type destType = typeof(GSABridgePath);

      var path = this.Value as StructuralBridgePath;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(keyword, path.ApplicationId);
      int alignmentIndex = GSA.Indexer.LookupIndex(typeof(GSABridgeAlignment).GetGSAKeyword(), path.AlignmentRef) ?? 1;

      var left = path.Offsets.First();
      var right = (path.PathType == StructuralBridgePathType.Track || path.PathType == StructuralBridgePathType.Vehicle) ? path.Gauge : path.Offsets.Last();

      var ls = new List<string>
        {
          "SET",
          keyword + ":" + HelperClass.GenerateSID(path),
          index.ToString(),
          string.IsNullOrEmpty(path.Name) ? "" : path.Name,
          PathTypeToGWAString(path.PathType),
          "1", //Group
          alignmentIndex.ToString(),
          left.ToString(),
          right.ToString(),
          path.LeftRailFactor.ToString()
      };

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
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
      new GSABridgePath() { Value = path }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSABridgePath dummyObject)
    {
      var objType = dummyObject.GetType();

      if (!Initialiser.GSASenderObjects.ContainsKey(objType))
        Initialiser.GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var paths = new List<GSABridgePath>();

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSABridgePath path = new GSABridgePath() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        path.ParseGWACommand(Initialiser.Interface);
        paths.Add(path);
      }

      Initialiser.GSASenderObjects[objType].AddRange(paths);

      if (paths.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
