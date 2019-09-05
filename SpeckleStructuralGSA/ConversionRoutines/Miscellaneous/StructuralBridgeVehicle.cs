using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("USER_VEHICLE.1", new string[] { }, "misc", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgeVehicle
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralBridgeVehicle();

    public void ParseGWACommand(GSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgeVehicle();

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

      Type destType = typeof(GSABridgeVehicle);

      StructuralBridgeVehicle vehicle = this.Value as StructuralBridgeVehicle;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(destType, vehicle);

      //The width parameter is intentionally not being used here as the meaning doesn't map to the y coordinate parameter of the ASSEMBLY keyword
      //It is therefore to be ignored here for GSA purposes.

      var ls = new List<string>
        {
          "SET",
          keyword + ":" + GSA.GenerateSID(vehicle),
          index.ToString(),
          string.IsNullOrEmpty(vehicle.Name) ? "" : vehicle.Name,
          vehicle.Width.ToString(),
          vehicle.Axles.Count().ToString()
      };

      foreach (var axle in vehicle.Axles)
      {
        ls.AddRange(new[] {
          axle.Position.ToString(),
          axle.WheelOffset.ToString(),
          axle.LeftWheelLoad.ToString(),
          axle.RightWheelLoad.ToString() });
      }

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralBridgeVehicle assembly)
    {
      new GSABridgeVehicle() { Value = assembly }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSABridgeVehicle dummyObject)
    {
      var objType = dummyObject.GetType();

      if (!GSASenderObjects.ContainsKey(objType))
        GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var alignments = new List<GSABridgeVehicle>();

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
        GSABridgeVehicle alignment = new GSABridgeVehicle() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        alignment.ParseGWACommand(GSA);
        alignments.Add(alignment);
      }

      GSASenderObjects[objType].AddRange(alignments);

      if (alignments.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
