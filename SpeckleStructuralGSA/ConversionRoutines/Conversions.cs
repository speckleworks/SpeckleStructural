using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    private static Dictionary<int, string> ToSpeckleBase<T>()
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword();

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      return Initialiser.Cache.GetGwaToSerialise(keyword);
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

    public static List<Point3D> Essential(this List<Point3D> origPts)
    {
      var pts = new List<Point3D>();

      for (var i = 0; i < origPts.Count(); i++)
      {
        var otherOrigPts = origPts.GetWithoutIndex(i);

        var linesBnAllPts = LinesBetweenAllPoints(otherOrigPts).ToList();

        var found = false;
        foreach (var l in linesBnAllPts)
        {
          if (IsOnLine(l, origPts[i]))
          {
            found = true;
            break;
          }
        }
        if (found == false)
        {
          pts.Add(origPts[i]);
        }
      }

      return pts;
    }

    public static bool IsOnLine(this Line3D l, Point3D p)
    {
      var closest = l.ClosestPointTo(p, true);
      var ret = (closest.Equals(p, 0.001));
      return ret;
    }

    private static List<Point3D> GetWithoutIndex(this IEnumerable<Point3D> pts, int index)
    {
      var otherPts = new List<Point3D>();
      var ptsArray = pts.ToArray();
      for (int i = 0; i < pts.Count(); i++)
      {
        if (i != index)
        {
          otherPts.Add(ptsArray[i]);
        }
      }
      return otherPts;
    }

    private static IEnumerable<Line3D> LinesBetweenAllPoints(this IEnumerable<Point3D> pts)
    {
      var ptsArray = pts.ToArray();
      for (var i = 0; i < pts.Count(); i++)
      {
        for (var j = i + 1; j < pts.Count(); j++)
        {
          yield return new Line3D(ptsArray[i], ptsArray[j]);
        }
      }
    }
  }
}
