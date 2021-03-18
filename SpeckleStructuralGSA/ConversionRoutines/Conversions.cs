using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    private static Dictionary<int, string> ToSpeckleBase<T>()
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword().Split('.').First();

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      return Initialiser.AppResources.Cache.GetGwaToSerialise(keyword);
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
      var retList = new List<Point3D>();

      var origPtsExtended = new List<Point3D>();
      
      if (origPts.First().Equals(origPts.Last(), SpeckleStructuralClasses.Helper.PointComparisonEpsilon))
      {
        origPtsExtended.Add(origPts[origPts.Count() - 2]);
        origPtsExtended.AddRange(origPts);
      }
      else
      {
        origPtsExtended.Add(origPts.Last());
        origPtsExtended.AddRange(origPts);
        origPtsExtended.Add(origPts.First());
      }
      var numPtsExtended = origPtsExtended.Count();

      //Check if the first/last point should be included

      //Check every point except the first/last point
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

    public static int ToInt(this string v)
    {
      try
      {
        if (int.TryParse(v, out int result))
        {
          return result;
        }
        return 0;
      }
      catch
      {
        return 0;
      }
    }

    public static bool IsOnLine(this Line3D l, Point3D p)
    {
      var closest = l.ClosestPointTo(p, true);
      var ret = (closest.Equals(p, SpeckleStructuralClasses.Helper.PointComparisonEpsilon));
      return ret;
    }
  }
}
