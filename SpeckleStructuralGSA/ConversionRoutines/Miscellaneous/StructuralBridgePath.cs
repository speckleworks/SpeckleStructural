using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PATH.1", new string[] { "ALIGN.1" }, "model", true, true, new Type[] { }, new Type[] { typeof(GSABridgeAlignment) })]
  public class GSABridgePath : GSABase<StructuralBridgePath>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgePath();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //PATH.1 \t2    \tPath One\tCWAY_1WAY\t1    \t1             \t-6.8\t6.8  \t0.5
      //keyword\tIndex\tName    \tPathType \tGroup\tAlignmentIndex\tLeft\tRight\tLeftRailFactor

      obj.PathType = GWAStringToPathType(pieces[counter++]);
      //obj.Gauge = 0;
      counter++; //Group
      counter++; //AlignmentIndex 
      counter++; //Left
      counter++; //Right
      obj.LeftRailFactor = pieces[counter++].ToDouble();

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSABridgePath);

      var path = this.Value as StructuralBridgePath;
      if (string.IsNullOrEmpty(path.ApplicationId))
      {
        return "";
      }

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, path.ApplicationId);
      var alignmentIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSABridgeAlignment).GetGSAKeyword(), path.AlignmentRef) ?? 1;

      var left = (path.Offsets == null || path.Offsets.Count() == 0) ? 0 : path.Offsets.First();
      var right = (path.PathType == StructuralBridgePathType.Track || path.PathType == StructuralBridgePathType.Vehicle) 
        ? path.Gauge 
        : (path.Offsets == null || path.Offsets.Count() == 0) ? 0 : path.Offsets.Last();

      var sid = Helper.GenerateSID(path);
      var ls = new List<string>
        {
          "SET",
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          index.ToString(),
          string.IsNullOrEmpty(path.Name) ? "" : path.Name,
          PathTypeToGWAString(path.PathType),
          "1", //Group
          alignmentIndex.ToString(),
          left.ToString(),
          right.ToString(),
          path.LeftRailFactor.ToString()
      };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
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

    private StructuralBridgePathType GWAStringToPathType(string pathType)
    {
      switch (pathType)
      {
        case "CWAY_1WAY": return StructuralBridgePathType.Carriage1Way;
        case "CWAY_2WAY": return StructuralBridgePathType.Carriage2Way;
        case "FOOTWAY":  return StructuralBridgePathType.Footway ;
        case "LANE": return StructuralBridgePathType.Lane;
        case "VEHICLE": return StructuralBridgePathType.Vehicle;
        default: return StructuralBridgePathType.Track;
      }
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralBridgePath path)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(path, () => new GSABridgePath() { Value = path }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSABridgePath dummyObject)
    {
      var newLines = ToSpeckleBase<GSABridgePath>();
      var paths = new SortedDictionary<int, GSABridgePath>();
      var typeName = dummyObject.GetType().Name;
      var pathsLock = new object();
      var keyword = dummyObject.GetGSAKeyword();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var path = new GSABridgePath() { GWACommand = newLines[k] };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        try
        {
          path.ParseGWACommand();
          lock (pathsLock)
          {
            paths.Add(k, path);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(paths.Values.ToList());

      return (paths.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
