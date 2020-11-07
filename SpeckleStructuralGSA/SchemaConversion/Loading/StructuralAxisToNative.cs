using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class StructuralAxisToNative
  {
    //Currently this is only used by other ToNative methods if they need to create axes
    public static string ToNative(StructuralAxis axis)
    {
      var keyword = GsaRecord.Keyword<GsaAxis>();
      var index = Initialiser.Cache.ResolveIndex(keyword);

      var origin = (Valid(axis.Origin)) ? axis.Origin : new SpecklePoint(0, 0, 0);
      var gsaAxis = new GsaAxis()
      {
        Index = index,
        Name = axis.Name,
        OriginX = origin.Value[0],
        OriginY = origin.Value[1],
        OriginZ = origin.Value[2],
        XDirX = axis.Xdir.Value[0],
        XDirY = axis.Xdir.Value[1],
        XDirZ = axis.Xdir.Value[2],
        XYDirX = axis.Ydir.Value[0],
        XYDirY = axis.Ydir.Value[1],
        XYDirZ = axis.Ydir.Value[2]
      };
      if (gsaAxis.Gwa(out var gwaLines, true))
      {
        return string.Join("\n", gwaLines);
      }
      return "";
    }

    private static bool Valid(SpecklePoint p)
    {
      return (p != null && p.Value != null);
    }
  }
}

