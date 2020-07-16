using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA.Test
{
  public static class MeshHelper
  {

    //Using pseudocode found in https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order/1180256#1180256
    public static int GetWindingDirection(this IEnumerable<Point2D> loopPoints)
    {
      var pts = loopPoints.ToArray();
      double signedArea = 0;
      var n = pts.Count();
      for (var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1) : 0;
        var x1 = pts[i].X;
        var y1 = pts[i].Y;
        var x2 = pts[nextPtIndex].X;
        var y2 = pts[nextPtIndex].Y;

        signedArea += (x1 * y2 - x2 * y1);
      }

      if (signedArea > 0)
      {
        return 1;
      }
      else if (signedArea < 0)
      {
        return -1;
      }
      else
      {
        return 0;
      }
    }

    public static bool Intersects(this Line2D candidate, Line2D other)
    {
      var intPtIntersection = candidate.IntersectWith(other);
      if (!intPtIntersection.HasValue)
      { 
        return false; 
      }
      var intPt = intPtIntersection.Value;

      //By default MathNet defines lines with infinite length so determine if the intersection point is actually within the original bounds of the line

      //The endpoints don't count
      if (intPt.EqualsWithinTolerance(candidate.StartPoint) || intPt.EqualsWithinTolerance(candidate.EndPoint))
      {
        return false;
      }

      var intPtOnCandidateLine = candidate.ClosestPointTo(intPt, true);
      var intPtOnOtherLine = other.ClosestPointTo(intPt, true);

      return intPtOnCandidateLine.EqualsWithinTolerance(intPtOnOtherLine);
    }

    public static bool EqualsWithinTolerance(this Point2D pt1, Point2D pt2, int toleranceNumDecimalPlaces = 3)
    {
      return pt1.DistanceTo(pt2).AlmostEqual(0, toleranceNumDecimalPlaces);
    }

    //Between does not include being parallel to either vectors - the value returned will be zer0
    public static bool IsBetweenVectors(this Vector2D candidate, Vector2D vFrom, Vector2D vTo)
    {
      var candidateDia = candidate.DiamondAngle();
      var fromDia = vFrom.DiamondAngle();
      var toDia = vTo.DiamondAngle();

      if(fromDia > toDia)
      {
        toDia += 4; //4 is the maximum diamond angle, so this will add a revolution
      }

      return (candidateDia > fromDia && candidateDia < toDia);
    }

    //Alternative to the expensive calculation of vector angles using trigonometry.
    //Sourced from: https://stackoverflow.com/questions/1427422/cheap-algorithm-to-find-measure-of-angle-between-vectors
    public static double DiamondAngle(this Vector2D v)
    {
      var x = v.X;
      var y = v.Y;
      if (y >= 0)
      {
        return (x >= 0 ? y / (x + y) : 1 - x / (-x + y));
      }
      else
      {
        return (x < 0 ? 2 - y / (-x - y) : 3 + x / (x - y));
      }
    }
  }
}
