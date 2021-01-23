using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleGSAInterfaces;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Load plane corresponds to GRID_SURFACE
  public static class StructuralLoadPlaneToNative
  {
    public static string ToNative(this StructuralLoadPlane loadPlane)
    {
      if (string.IsNullOrEmpty(loadPlane.ApplicationId))
      {
        return "";
      }

      var keyword = GsaRecord.GetKeyword<GsaGridSurface>();
      var storeyKeyword = GsaRecord.GetKeyword<GsaGridPlane>();
      var streamId = Initialiser.AppResources.Cache.LookupStream(loadPlane.ApplicationId);

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, loadPlane.ApplicationId);
      var gsaGridSurface = new GsaGridSurface()
      {
        ApplicationId = loadPlane.ApplicationId,
        StreamId = streamId,
        Index = index,
        Name = loadPlane.Name,
        Tolerance = loadPlane.Tolerance,
        Angle = loadPlane.SpanAngle,

        Type = (loadPlane.ElementDimension.HasValue && loadPlane.ElementDimension.Value == 1)
          ? GridSurfaceElementsType.OneD
          : (loadPlane.ElementDimension.HasValue && loadPlane.ElementDimension.Value == 2)
            ? GridSurfaceElementsType.TwoD
            : GridSurfaceElementsType.NotSet,

        Span = (loadPlane.Span.HasValue && loadPlane.Span.Value == 1)
          ? GridSurfaceSpan.One
          : (loadPlane.Span.HasValue && loadPlane.Span.Value == 2)
            ? GridSurfaceSpan.Two
            : GridSurfaceSpan.NotSet,

        //There is no support for entity references in the structural schema, so select "all" here
        AllIndices = true,

        //There is no support for this argument in the Structural schema, and was even omitted from the GWA 
        //in the previous version of the ToNative code
        Expansion = GridExpansion.PlaneCorner
      };

      if (!string.IsNullOrEmpty(loadPlane.StoreyRef))
      {
        var gridPlaneIndex = Initialiser.AppResources.Cache.LookupIndex(storeyKeyword, loadPlane.StoreyRef);

        if (gridPlaneIndex.ValidNonZero())
        {
          gsaGridSurface.PlaneRefType = GridPlaneAxisRefType.Reference;
          gsaGridSurface.PlaneIndex = gridPlaneIndex;
        }
      }
      else if (loadPlane.Axis.ValidNonZero())
      {
        gsaGridSurface.PlaneRefType = GridPlaneAxisRefType.Reference;

        //Create axis
        //Create new axis on the fly here
        var gsaAxis = StructuralAxisToNative.ToNativeSchema(loadPlane.Axis);
        StructuralAxisToNative.ToNative(gsaAxis);

        //Create plane - the key here is that it's not a storey, but a general, type of grid plane, 
        //which is why the ToNative() method for SpeckleStorey shouldn't be used as it only creates storey-type GSA grid plane
        var gsaPlaneKeyword = GsaRecord.GetKeyword<GsaGridPlane>();
        var planeIndex = Initialiser.AppResources.Cache.ResolveIndex(gsaPlaneKeyword);
        
        var gsaPlane = new GsaGridPlane()
        {
          Index = planeIndex,
          Name = loadPlane.Name,
          Type = GridPlaneType.General,
          AxisRefType = GridPlaneAxisRefType.Reference,
          AxisIndex = gsaAxis.Index
        };
        if (gsaPlane.Gwa(out var gsaPlaneGwas, true))
        {
          Initialiser.AppResources.Cache.Upsert(gsaPlaneKeyword, planeIndex, gsaPlaneGwas.First(), streamId, "", GsaRecord.GetGwaSetCommandType<GsaGridPlane>());
        }
        gsaGridSurface.PlaneIndex = planeIndex;
      }
      else
      {
        gsaGridSurface.PlaneRefType = GridPlaneAxisRefType.Global;
      }

      if (gsaGridSurface.Gwa(out var gwaLines, false))
      {
        Initialiser.AppResources.Cache.Upsert(keyword, index, gwaLines.First(), streamId, loadPlane.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaGridSurface>());
      }

      return "";
    }
  }
}
