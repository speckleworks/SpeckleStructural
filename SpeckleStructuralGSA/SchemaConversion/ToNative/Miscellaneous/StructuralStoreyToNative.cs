using System.Collections.Generic;
using System.Linq;
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
      if (string.IsNullOrEmpty(storey.ApplicationId) && Helper.IsZeroAxis(storey.Axis))
      {
        return "";
      }

      var keyword = GsaRecord.GetKeyword<GsaGridPlane>();
      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, storey.ApplicationId);
      var streamId = Initialiser.AppResources.Cache.LookupStream(storey.ApplicationId);

      var gsaPlane = new GsaGridPlane()
      {
        Index = index,
        ApplicationId = storey.ApplicationId,
        StreamId = streamId,
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
        var gsaAxis = StructuralAxisToNative.ToNativeSchema(storey.Axis);
        StructuralAxisToNative.ToNative(gsaAxis);

        gsaPlane.AxisIndex = gsaAxis.Index;
      }
      else
      {
        gsaPlane.AxisRefType = GridPlaneAxisRefType.Global;
      }

      if (gsaPlane.Gwa(out var gsaPlaneGwaLines, true))
      {
        Initialiser.AppResources.Cache.Upsert(keyword, index, gsaPlaneGwaLines.First(), streamId, storey.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaLoadCase>());
      }

      return "";
    }
  }
}
