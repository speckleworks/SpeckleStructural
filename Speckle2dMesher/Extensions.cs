using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Spatial.Euclidean;

namespace Speckle2dMesher
{
  public static class Extensions
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

    //Assumption: this method would never be called when the intersector's end points lie in the middle of an intersectee line
    //This is the case for meshes defined by an ordered set of vertices
    public static bool Intersects(this Line2D intersectee, Line2D intersector)
    {
      var intPtIntersection = intersectee.IntersectWith(intersector);
      if (!intPtIntersection.HasValue)
      { 
        return false; 
      }
      var intPt = intPtIntersection.Value;

      //There is no intersection if it's at the end points of the line doing the suppposed intersection
      if (intersector.StartPoint.EqualsWithinTolerance(intPt) || intersector.EndPoint.EqualsWithinTolerance(intPt))
      {
        return false;
      }

      //By default MathNet defines lines with infinite length so determine if the intersection point is actually within the original bounds of the line
      var intersecteePt = intersectee.ClosestPointTo(intPt, true);
      var intersectorPt = intersector.ClosestPointTo(intPt, true);
      return intersecteePt.EqualsWithinTolerance(intersectorPt);
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

    public static double[] Essential(this IEnumerable<double> coords)
    {
      var pts = coords.ToPoints();
      var reducedPts = pts.Essential();
      var retCoords = new double[reducedPts.Count() * 3];
      for (var i = 0; i < reducedPts.Count(); i++)
      {
        retCoords[i * 3] = reducedPts[i].X;
        retCoords[(i * 3) + 1] = reducedPts[i].Y;
        retCoords[(i * 3) + 2] = reducedPts[i].Z;
      }
      return retCoords;
    }

    public static List<Point3D> Essential(this List<Point3D> origPts)
    {
      var origPtsExtended = new List<Point3D>() { origPts.Last() };
      origPtsExtended.AddRange(origPts);
      origPtsExtended.Add(origPts.First());
      var numPtsExtended = origPtsExtended.Count();
      var retList = new List<Point3D>();

      for (var i = 1; i < (numPtsExtended - 1); i++)
      {
        var prev = origPtsExtended[i - 1];
        var next = origPtsExtended[i + 1];
        if (!origPtsExtended[i].IsOnLineBetween(prev, next))
        {
          retList.Add(origPtsExtended[i]);
        }
      }

      return retList;
    }

    public static bool IsOnLineBetween(this Point3D p, Point3D start, Point3D end)
    {
      var l = new Line3D(start, end);
      return l.IsOnLine(p);
    }

    public static bool IsOnLine(this Line3D l, Point3D p)
    {
      var closest = l.ClosestPointTo(p, true);
      var ret = (closest.Equals(p, 0.001));
      return ret;
    }

    public static List<Point3D> ToPoints(this IEnumerable<double> coords)
    {
      var numPts = (int)(coords.Count() / 3);
      var pts = new List<Point3D>();

      var coordsArray = coords.ToArray();
      for (var i = 0; i < numPts; i++)
      {
        pts.Add(new Point3D(coordsArray[i * 3], coordsArray[(i * 3) + 1], coordsArray[(i * 3) + 2]));
      }
      return pts;
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
