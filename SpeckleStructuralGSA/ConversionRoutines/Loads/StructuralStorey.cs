using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_PLANE.4", new string[] { "AXIS.1" }, "loads", true, true, new Type[] { }, new Type[] { })]
  public class GSAStorey : GSAGridPlaneBase, IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoad();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralStorey();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var storey = this.Value as StructuralStorey;

      var keyword = typeof(GSAStorey).GetGSAKeyword();
      var index = Initialiser.Cache.ResolveIndex(keyword);

      int gridPlaneIndex;
      var axis = (storey.Axis == null) ? new StructuralAxis(new StructuralVectorThree(1, 0, 0), new StructuralVectorThree(0, 1, 0)) : storey.Axis;

      var gwaCommands = SetAxisPlaneGWACommands(axis, storey.Name, out gridPlaneIndex);

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralStorey storey)
    {
      return new GSAStorey() { Value = storey }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAStorey dummyObject)
    {
      var newLines = ToSpeckleBase<GSAStorey>();

      var storeys = new List<GSAStorey>();

      foreach (var k in newLines.Keys)
      {
        var storey = new GSAStorey() { GSAId = k, GWACommand = newLines[k] };
        storey.ParseGWACommand();
        storeys.Add(storey);
      }

      Initialiser.GSASenderObjects[typeof(GSA2DThermalLoading)].AddRange(storeys);

      return (storeys.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
