using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_SURFACE.1", new string[] { "POLYLINE.1", "GRID_PLANE.4", "AXIS.1" }, "model", true, true, new Type[] { }, new Type[] { typeof(GSAStorey) })]
  public class GSAGridSurface : GSABase<StructuralLoadPlane>
  {
    public bool ParseGWACommand()
    {
      //TO DO
      return false;
    }
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSAGridSurface dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridSurface>();
      var planes = new List<GSAGridSurface>();
      var keyword = dummyObject.GetGSAKeyword();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var plane = new GSAGridSurface() { GWACommand = p, GSAId = k };
        try
        {
          if (plane.ParseGWACommand())
          {
            planes.Add(plane);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(planes);

      return (planes.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
