using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_PLANE.4", new string[] { "AXIS.1" }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSAStorey : GSAGridPlaneBase, IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralStorey();

    public bool ParseGWACommand()
    {
      if (this.GWACommand == null)
        return false;

      var pieces = this.GWACommand.ListSplit("\t");

      if (pieces[3].ToLower() != "storey")
      {
        return false;
      }

      var obj = new StructuralStorey();

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO 

      this.Value = obj;

      return true;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var storey = this.Value as StructuralStorey;
      if (storey.ApplicationId == null)
      {
        return "";
      }

      var axis = (storey.Axis == null) ? new StructuralAxis(new StructuralVectorThree(1, 0, 0), new StructuralVectorThree(0, 1, 0)) : storey.Axis;

      var gwaCommands = SetAxisPlaneGWACommands(axis, storey.Name, out var gridPlaneIndex, storey.Elevation, storey.ToleranceAbove, storey.ToleranceBelow, 
        GridPlaneType.Storey, Helper.GenerateSID(storey));

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
      var typeName = dummyObject.GetType().Name;
      var storeys = new List<GSAStorey>();

      foreach (var k in newLines.Keys)
      {
        var storey = new GSAStorey() { GWACommand = newLines[k] };
        try
        {
          if (storey.ParseGWACommand())
          {
            storeys.Add(storey);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }
      }

      Initialiser.GSASenderObjects.AddRange(storeys);

      return (storeys.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
