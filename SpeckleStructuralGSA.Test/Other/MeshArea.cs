using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Providers.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;

namespace SpeckleStructuralGSA.Test
{
  public partial class MeshArea
  {
    private class IndexPair : IEqualityComparer
    {
      public readonly int[] Indices = new int[2];
      public IndexPair(int index1, int index2)
      {
        Indices[0] = index1;
        Indices[1] = index2;
      }

      public bool Contains(int index)
      {
        return ((Indices[0] == index) || (Indices[1] == index));
      }

      public int? Other(int index)
      {
        if (Indices[0] == index)
        {
          return Indices[1];
        }
        else if (Indices[1] == index)
        {
          return 0;
        }
        else
        {
          return null;
        }
      }

      public new bool Equals(object x, object y)
      {
        var l1 = (IndexPair)x;
        var l2 = (IndexPair)y;

        return ((l1.Indices[0] == l2.Indices[0]) && (l1.Indices[1] == l2.Indices[1]) || (l1.Indices[0] == l2.Indices[1]) && (l1.Indices[1] == l2.Indices[0]));
      }

      public int GetHashCode(object obj)
      {
        if (obj == null)
        {
          return 0;
        }
        IndexPair line = null;
        try
        {
          line = ((IndexPair)obj);
        }
        catch { }
        return (line == null) ? 0 : line.Indices[0].GetHashCode() ^ line.Indices[1].GetHashCode();
      }
    }

    private readonly List<Point2D> pts = new List<Point2D>(); //Ordered
    private int windingDirection = 0; //Will be 1 for left and -1 for right orientation wrt line direction along plane

    private readonly List<IndexPair> InternalIndexPairs = new List<IndexPair>();

    public MeshArea()
    {
      
    }

    //Assumptions:
    // - input coordinates are an ordered set of external vertices (i.e. not vertices of openings)
    // - the lines defined by these vertices don't intersect each other
    public bool Init(double[] coords)
    {
      var origPts = new List<Point3D>();
      for (var i = 0; i < coords.Length; i += 3)
      {
        origPts.Add(new Point3D(coords[i], coords[i + 1], coords[i + 2]));
      }

      //Create plane
      var plane = Plane.FromPoints(origPts[0], origPts[1], origPts[2]);
      var normal = plane.Normal;
      var origin = origPts[0];
      var xDir = origPts[0].VectorTo(origPts[1]).Normalize();
      var yDir = normal.CrossProduct(xDir);

      //The CoordinateSystem class in MathNet.Spatial and its methods aren't not very intuitive as discussed in https://github.com/mathnet/mathnet-spatial/issues/53
      //Since I don't understand the offsets that seem to be applied by TransformFrom and TransformTo, I focussed on the Transform method,
      //which transforms a local point to a global point.
      //In order to transform a point from global into local, the coordinate system needs to be reversed so that the resulting coordinateSystem.Transform does the
      //transformation from global to local.
      var coordinateTranslation = new CoordinateSystem((new CoordinateSystem(origin, xDir, yDir, normal)).Inverse());

      //project points onto the plane - if the points are co-planar and translation is done correctly, all Z values should be zero
      int nonCoPlanarPts = 0;
      for (var i = 0; i < origPts.Count(); i++)
      {
        var projectedPt = coordinateTranslation.Transform(origPts[i]);
        if (!projectedPt.Z.AlmostEqual(0,3))
        {
          nonCoPlanarPts++;
        }
        pts.Add(new Point2D(projectedPt.X, projectedPt.Y));
      }

      if (nonCoPlanarPts > 0)
      {
        return false;
      }

      GetWindingDirection();
      return true;
    }

    public int[] Faces()
    {
      var faces = new List<int>();
      var n = pts.Count();

      var externalLines = GetExternalLines();

      //Go through each point
      for (var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1): 0;
        var prevPtIndex = (i > 0) ? i - 1 : (n - 1);
        var dictHalfSweep = PointIndicesInHalfSweep(i, nextPtIndex, prevPtIndex);

        if (dictHalfSweep == null)
        {
          return faces.ToArray();
        }
        
        //Go through each line emanating from this point
        foreach (var vector in dictHalfSweep.Keys)
        {
          double? currShortestDistance = null;
          int? shortestPtIndex = null;
          foreach (var ptIndex in dictHalfSweep[vector])
          {
            var indexPair = new IndexPair(i, ptIndex);

            //Create line from the candidate point to this item point
            var currLine = GetLine(indexPair);

            //Check if this line is already in the collection - if so, ignore it
            if (InternalIndexPairs.Contains(indexPair)) continue;

            //Check if this line would intersect any external lines in this collection - if so, ignore it
            if (IntersectsExternalLines(indexPair)) continue;

            //Check if this line would intersect any already in this collection - if so, ignore it
            if (IntersectsInternalLines(indexPair)) continue;

            //Calculate distance - if currShortestDistance is either -1 or shorter than the shortest, replace the shortest
            var distance = GetLength(indexPair);
            if (!currShortestDistance.HasValue || (distance < currShortestDistance))
            {
              currShortestDistance = distance;
              shortestPtIndex = ptIndex;
            }
          }

          //Now that the shortest valid line to another point has been found, add it to the list of lines
          if (currShortestDistance > 0 && shortestPtIndex.HasValue && shortestPtIndex > 0)
          {
            //Add line
            InternalIndexPairs.Add(new IndexPair(i, shortestPtIndex.Value));
          }
        }
      }

      //Now determine faces by cycling through each edge line and finding which other point is shared between all lines emanating from this point
      for (var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1) : 0;

        var indicesLinkedToCurr = GetPairedIndices(i);
        var indicesLinkedToNext = GetPairedIndices(nextPtIndex);

        var sharedPtIndices = indicesLinkedToCurr.Where(ci => indicesLinkedToNext.Any(ni => ci == ni));

        if (sharedPtIndices != null)
        {
          if (sharedPtIndices.Count() == 1)
          {
            faces.AddRange(new[] { 0, i, nextPtIndex, sharedPtIndices.First() });
          }
          else
          {
            throw new Exception("Found multiple shared indices");
          }
        }
      }

      return faces.ToArray();
    }

    private List<int> GetPairedIndices(int index)
    {
      return InternalIndexPairs.Select(l => l.Other(index)).Where(l => l.HasValue).Cast<int>().ToList();
    }

    private double GetLength(IndexPair indexPair)
    {
      Line2D? line = null;
      try
      {
        line = GetLine(indexPair);
      }
      catch { }
      return (line == null) ? 0 : line.Value.Length;
    }

    private Line2D GetLine(IndexPair indexPair)
    {
      return new Line2D(pts[indexPair.Indices[0]], pts[indexPair.Indices[1]]);
    }

    private List<Line2D> GetExternalLines()
    {
      var lines = new List<Line2D>();
      var n = pts.Count();
      for (var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1) : 0;
        lines.Add(new Line2D(pts[i], pts[nextPtIndex]));
      }
      return lines;
    }

    private List<Line2D> GetInternalLines()
    {
      var lines = new List<Line2D>();
      var n = InternalIndexPairs.Count();
      for (var i = 0; i < n; i++)
      {
        lines.Add(GetLine(InternalIndexPairs[i]));
      }
      return lines;
    }

    private bool IntersectsExternalLines(IndexPair indexPair)
    {
      var line = GetLine(indexPair);

      return GetExternalLines().Any(el => el.Intersects(line));
    }

    private bool IntersectsInternalLines(IndexPair indexPair)
    {
      var line = GetLine(indexPair);

      return GetExternalLines().Any(el => el.Intersects(line));
    }

    private bool Intersects(IndexPair i1, IndexPair i2)
    {
      var line1 = GetLine(i1);
      var line2 = GetLine(i2);

      return line1.Intersects(line2);
    }

    //Using pseudocode found in https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order/1180256#1180256
    private void GetWindingDirection()
    {
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
        windingDirection = 1;
      }
      else if (signedArea < 0)
      {
        windingDirection = -1;
      }
    }

    //Because multiple points can be aligned along the same direction from any given point, a dictionary is returned where
    //the (unit) vectors towards the points are the keys, and all points in that exact direction listed as the values
    private Dictionary<Vector2D, List<int>> PointIndicesInHalfSweep(int ptIndex, int nextPtIndex, int prevPtIndex)
    {
      if (windingDirection == 0)
      {
        return null;
      }
      var vCurrToNext = pts[ptIndex].VectorTo(pts[nextPtIndex]).Normalize();
      var vCurrToPrev = pts[ptIndex].VectorTo(pts[prevPtIndex]).Normalize();

      var dict = new Dictionary<Vector2D, List<int>>();

      for (var i = 0; i < pts.Count(); i++)
      {
        if (i == ptIndex) continue;

        var vItem = pts[ptIndex].VectorTo(pts[i]).Normalize();

        //The swapping of the vectors below is to align with the fact that the vector angle comparison is always done anti-clockwise
        var isBetween = (windingDirection > 0)
          ? vItem.IsBetweenVectors(vCurrToNext, vCurrToPrev)
          : vItem.IsBetweenVectors(vCurrToPrev, vCurrToNext);

        if (isBetween)
        {
          if (!dict.ContainsKey(vItem))
          {
            dict.Add(vItem, new List<int>());
          }
          dict[vItem].Add(i);
        }
      }
      return dict;
    }

    // wn_PnPoly(): winding number test for a point in a polygon
    //      Input:   P = a point,
    //               V[] = vertex points of a polygon V[n+1] with V[n]=V[0]
    //      Return:  wn = the winding number (=0 only when P is outside)
    // Adapted from: http://geomalgorithms.com/a03-_inclusion.html
    private int Wn_PnPoly(Point2D P, List<Point2D> V)
    {
      int wn = 0;    // the  winding number counter
      int n = V.Count();

      // loop through all edges of the polygon
      for (int i = 0; i < n; i++)
      {   // edge from V[i] to  V[i+1]
        if (V[i].Y <= P.Y)
        {          // start y <= P.y
          if (V[i + 1].Y > P.Y)      // an upward crossing
          {
            if (IsLeft(V[i], V[i + 1], P) > 0)  // P left of  edge
            {
              ++wn;            // have  a valid up intersect
            }
          }
        }
        else
        {                        // start y > P.y (no test needed)
          if (V[i + 1].Y <= P.Y)     // a downward crossing
          {
            if (IsLeft(V[i], V[i + 1], P) < 0)  // P right of  edge
            {
              --wn;            // have  a valid down intersect
            }
          }
        }
      }
      return wn;
    }

    // isLeft(): tests if a point is Left|On|Right of an infinite line.
    //    Input:  three points P0, P1, and P2
    //    Return: >0 for P2 left of the line through P0 and P1
    //            =0 for P2  on the line
    //            <0 for P2  right of the line
    //    See: Algorithm 1 "Area of Triangles and Polygons"
    // Adapted from: http://geomalgorithms.com/a03-_inclusion.html
    private int IsLeft(Point2D P0, Point2D P1, Point2D P2)
    {
      var result = ( ((P1.X - P0.X) * (P2.Y - P0.Y)
              - (P2.X - P0.X) * (P1.Y - P0.Y)));
      return (result == 0) ? 0 : (result < 0) ? -1 : 1;
    }
  }
}
