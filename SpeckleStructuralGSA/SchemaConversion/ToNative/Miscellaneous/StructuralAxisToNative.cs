using System.Linq;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class StructuralAxisToNative
  {
    //These methods are structured slightly differently as currently an axis is a special case - it's not directly part of the structural schema
    //and only called as part of ToNative calls for other higher-level types which create axes as necessary

    public static string ToNative(this GsaAxis gsaAxis)
    {
      var keyword = GsaRecord.GetKeyword<GsaAxis>();  

      if (gsaAxis.Gwa(out var gwaLines, false))
      {
        //axes currently never have an application ID
        Initialiser.AppResources.Cache.Upsert(keyword, gsaAxis.Index.Value, gwaLines.First(), gsaAxis.StreamId, "", GsaRecord.GetGwaSetCommandType<GsaAxis>());
      }
      return "";
    }

    public static GsaAxis ToNativeSchema(this StructuralAxis axis)
    {
      var keyword = GsaRecord.GetKeyword<GsaAxis>();
      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword);
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
      return gsaAxis;
    }

    private static bool Valid(SpecklePoint p)
    {
      return (p != null && p.Value != null);
    }
  }
}

