using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleStructuralClasses;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{

  public abstract class GSAGridPlaneBase
  {
    protected enum GridPlaneType
    {
      General = 1,
      Storey = 2
    }

    protected List<string> SetAxisPlaneGWACommands(StructuralAxis axis, string planeName, out int gridPlaneIndex, double? elevation = null, 
      double? elevationAbove = null, double? elevationBelow = null, GridPlaneType gridPlaneType = GridPlaneType.General, string sid = null)
    {
      var gridPlaneKeyword = "GRID_PLANE.4";
      gridPlaneIndex = Initialiser.Cache.ResolveIndex(gridPlaneKeyword);

      var ls = new List<string>();

      var gwaCommands = new List<string>();

      Helper.SetAxis(axis, out var planeAxisIndex, out var planeAxisGwa, planeName);
      if (planeAxisGwa.Length > 0)
      {
        gwaCommands.Add(planeAxisGwa);
      }

      var planeType = gridPlaneType.ToString().ToUpper();

      ls.Clear();

      ls.AddRange(new[] {
        "SET",
        gridPlaneKeyword + ((sid == null) ? "" : ":" + sid),
        gridPlaneIndex.ToString(),
        (string.IsNullOrEmpty(planeName)) ? " " : planeName,
        planeType, // Type
        planeAxisIndex.ToString(),
        (elevation ?? 0).ToString(), // Elevation
        (elevationBelow ?? 0).ToString(), // tolerance below
        (elevationAbove ?? 0).ToString()
      }); 

      gwaCommands.Add(string.Join(Initialiser.Interface.GwaDelimiter.ToString(), ls));

      return gwaCommands;
    }
  }
}
