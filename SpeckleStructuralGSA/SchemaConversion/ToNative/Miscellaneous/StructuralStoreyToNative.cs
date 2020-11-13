using System.Collections.Generic;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Corresponds to GRID_PLANE
  public static class StructuralStoreyToNative
  {
    public static string ToNative(this StructuralStorey storey)
    {
      var keyword = GsaRecord.Keyword<GsaGridPlane>();
      var index = Initialiser.Cache.ResolveIndex(keyword, storey.ApplicationId);
      var gwaCommands = new List<string>();
      var gsaPlane = new GsaGridPlane()
      {
        Index = index,
        ApplicationId = storey.ApplicationId,
        Name = storey.Name,
        Elevation = storey.Elevation,
        Type = GridPlaneType.Storey,
      };

      gsaPlane.StoreyToleranceAboveAuto = (!storey.ToleranceAbove.HasValue || storey.ToleranceAbove.Value == 0);
      if (storey.ToleranceBelow.HasValue && storey.ToleranceBelow.Value != 0)
      {
        gsaPlane.StoreyToleranceBelow = storey.ToleranceBelow;
      }
      gsaPlane.StoreyToleranceBelowAuto = (!storey.ToleranceBelow.HasValue || storey.ToleranceBelow.Value == 0);
      if (!gsaPlane.StoreyToleranceAboveAuto)
      {
        gsaPlane.StoreyToleranceAbove = storey.ToleranceAbove;
      }

      if (storey.ValidNonZero())
      {
        gsaPlane.AxisRefType = GridPlaneAxisRefType.Reference;
        //Create new axis on the fly here
        var gsaAxisGwa = StructuralAxisToNative.ToNative(storey.Axis);
        gwaCommands.Add(gsaAxisGwa);

        //TO DO: review ways around having to parse here to get the newly-created axis index
        Initialiser.Interface.ParseGeneralGwa(gsaAxisGwa, out string _, out int? axisIndex, out string _, out string _, out string _, out GwaSetCommandType? _);
        gsaPlane.AxisIndex = axisIndex;
      }
      else
      {
        gsaPlane.AxisRefType = GridPlaneAxisRefType.Global;
      }

      if (gsaPlane.Gwa(out var gsaPlaneGwaLines, true))
      {
        gwaCommands.AddRange(gsaPlaneGwaLines);
        return string.Join("\n", gwaCommands);
      }
      else
      {
        return "";
      }
    }
  }
}
