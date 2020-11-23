using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class StructuralAssemblyToNative
  {
    public static string ToNative(this StructuralAssembly assembly)
    {
      if (string.IsNullOrEmpty(assembly.ApplicationId) || assembly.Value == null || assembly.Value.Count < 6)
      {
        return "";
      }

      //Need to convert rounding expressed as an epsilon (e.g. 0.001) to a number of decimal places for use in Math.Round calls
      var epsilon = SpeckleStructuralClasses.Helper.PointComparisonEpsilon.ToString();
      var numDecPlaces = epsilon.IndexOf('1') - epsilon.IndexOf('.');

      var keyword = GsaRecord.Keyword<GsaAssembly>();
      var index = Initialiser.Cache.ResolveIndex(keyword, assembly.ApplicationId);
      var streamId = Initialiser.Cache.LookupStream(assembly.ApplicationId);

      var topo1Pt = CoordsToPoint(assembly.Value.Take(3));
      var topo2Pt = CoordsToPoint(assembly.Value.Skip(3).Take(3));
      var orientPt = (assembly.OrientationPoint.Value == null) ? new Point3D(0,0,0) : CoordsToPoint(assembly.OrientationPoint.Value);
      
      var topo1Index = Initialiser.Interface.NodeAt(topo1Pt.X, topo1Pt.Y, topo1Pt.Z, Initialiser.Settings.CoincidentNodeAllowance);
      var topo2Index = Initialiser.Interface.NodeAt(topo2Pt.X, topo2Pt.Y, topo2Pt.Z, Initialiser.Settings.CoincidentNodeAllowance);
      var orientPtIndex = Initialiser.Interface.NodeAt(orientPt.X, orientPt.Y, orientPt.Z, Initialiser.Settings.CoincidentNodeAllowance);

      var entityKeyword = (Initialiser.Settings.TargetLayer == GSATargetLayer.Design) ? GsaRecord.Keyword<GsaMemb>() : GsaRecord.Keyword<GsaEl>();
      var entityIndices = Initialiser.Cache.LookupIndices(entityKeyword, assembly.ElementRefs).Where(i => i.HasValue).Select(i => i.Value).ToList();

      var gsaAssembly = new GsaAssembly()
      {
        Index = index,
        ApplicationId = assembly.ApplicationId,
        StreamId = streamId,
        Name = assembly.Name,
        Topo1 = topo1Index,
        Topo2 = topo2Index,
        OrientNode = orientPtIndex,
        CurveType = CurveType.Lagrange,
        SizeY = assembly.Width ?? 0,
        SizeZ = 0,
        Type = (Initialiser.Settings.TargetLayer == GSATargetLayer.Design) ? GSAEntity.MEMBER : GSAEntity.ELEMENT,
        Entities = entityIndices
      };

      if (assembly.NumPoints.HasValue && assembly.NumPoints.Value > 0)
      {
        gsaAssembly.PointDefn = PointDefinition.Points;
        gsaAssembly.NumberOfPoints = assembly.NumPoints.Value;
      }
      else if (assembly.PointDistances != null && assembly.PointDistances.Count() > 0)
      {
        gsaAssembly.PointDefn = PointDefinition.Explicit;
        var distances = assembly.PointDistances.Select(pd => Math.Round(pd, numDecPlaces)).Distinct().OrderBy(n => n).ToList();
        gsaAssembly.ExplicitPositions.AddRange(distances);
      }
      else if (assembly.StoreyRefs != null && assembly.StoreyRefs.Count() > 0)
      {
        gsaAssembly.PointDefn = PointDefinition.Storey;
        gsaAssembly.StoreyIndices = Initialiser.Cache.LookupIndices(GsaRecord.Keyword<GsaGridPlane>(), assembly.StoreyRefs).Where(i => i.HasValue).Select(i => i.Value).ToList();
        //Not verifying at the moment that the grid planes are indeed storeys
      }
      else
      {
        gsaAssembly.PointDefn = PointDefinition.Points;
        gsaAssembly.NumberOfPoints = 10;
      }

      if (gsaAssembly.Gwa(out var gwaLines, false))
      {
        //axes currently never have a stream nor an application ID
        Initialiser.Cache.Upsert(keyword, gsaAssembly.Index.Value, gwaLines.First(), streamId, assembly.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaAssembly>());
      }

      return "";
    }
    private static Point3D CoordsToPoint(IEnumerable<double> c)
    {
      var coords = c.ToList();
      return new Point3D(coords[0], coords[1], coords[2]);
    }
  }
}
