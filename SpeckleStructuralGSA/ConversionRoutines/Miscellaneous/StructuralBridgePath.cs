using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PATH.1", new string[] { "ALIGN.1" }, "misc", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgePath : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralBridgePath();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgePath();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = HelperClass.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO: change these defaults for the real thing
      obj.PathType = StructuralBridgePathType.Lane;
      obj.Gauge = 0;
      obj.LeftRailFactor = 0;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSABridgePath);

      var path = this.Value as StructuralBridgePath;

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.Indexer.ResolveIndex(keyword, destType.Name, path.ApplicationId);
      var alignmentIndex = Initialiser.Indexer.LookupIndex(typeof(GSABridgeAlignment).GetGSAKeyword(), typeof(GSABridgeAlignment).ToSpeckleTypeName(), path.AlignmentRef) ?? 1;

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

      return (string.Join("\t", ls));
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
    public static string ToNative(this StructuralBridgePath path)
    {
      return new GSABridgePath() { Value = path }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSABridgePath dummyObject)
    {
      var newLines = ToSpeckleBase<GSABridgePath>();
      var paths = new List<GSABridgePath>();

      foreach (var p in newLines.Values)
      {
        var path = new GSABridgePath() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        path.ParseGWACommand();
        paths.Add(path);
      }

      Initialiser.GSASenderObjects[typeof(GSABridgePath)].AddRange(paths);

      return (paths.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
