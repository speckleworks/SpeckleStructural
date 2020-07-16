using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA.Test
{
  public partial class MeshArea
  {
    #region helper_classes
    private abstract class IndexSet
    {
      public readonly int[] Indices;

      public IndexSet(List<int> values)
      {
        Indices = values.OrderBy(v => v).ToArray();
      }

      public bool Contains(int index)
      {
        return Indices.Any(i => i == index);
      }

      public bool Matches(IndexSet other)
      {
        return Indices.SequenceEqual(other.Indices);
      }
    }

    private class IndexPair : IndexSet
    {
      public IndexPair(int index1, int index2) : base (new List<int>() { index1, index2 })
      {
      }

      public int? Other(int index)
      {
        return (Indices[0] == index) ? Indices[1] : (Indices[1] == index) ? (int ?) Indices[0] : null;
      }
    }

    private class TriangleIndexSet : IndexSet
    {
      public TriangleIndexSet(int index1, int index2, int index3) : base(new List<int>() {  index1, index2, index3 })
      {
      }
    }

    private class ClosedLoop
    {
      public Dictionary<int, Point2D> pts;
      public Dictionary<IndexPair, Line2D> Loop;
      public int WindingDirection;
      public CoordinateSystem CoordinateTranslation;

      private int ptIndexOffset = 0;

      public bool Init(double[] globalCoords, ref CoordinateSystem CoordinateTranslation, int ptIndexOffset = 0)
      {
        this.ptIndexOffset = ptIndexOffset;

        var essential = globalCoords.Essential();
        var origPts = new List<Point3D>();
        for (var i = 0; i < essential.Length; i += 3)
        {
          origPts.Add(new Point3D(essential[i], essential[i + 1], essential[i + 2]));
        }

        this.pts = new Dictionary<int, Point2D>();

        if (CoordinateTranslation == null)
        {
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
          CoordinateTranslation = new CoordinateSystem((new CoordinateSystem(origin, xDir, yDir, normal)).Inverse());
        }
        else
        {
          this.CoordinateTranslation = CoordinateTranslation;
        }

        //project points onto the plane - if the points are co-planar and translation is done correctly, all Z values should be zero
        var nonCoPlanarPts = 0;
        var n = origPts.Count();
        for (var i = 0; i < n; i++)
        {
          var projectedPt = CoordinateTranslation.Transform(origPts[i]);
          if (!projectedPt.Z.AlmostEqual(0, 3))
          {
            nonCoPlanarPts++;
          }
          AddPoint(projectedPt.X, projectedPt.Y);
        }

        WindingDirection = 0;
        Loop = new Dictionary<IndexPair, Line2D>();
        if (nonCoPlanarPts > 0)
        {
          return false;
        }

        for (var i = 0; i < n; i++)
        {
          var indexPair = new IndexPair(ptIndexOffset + i, ptIndexOffset + ((i == (n - 1)) ? 0 : (i + 1)));
          Loop.Add(indexPair, GetLine(indexPair));
        }

        this.WindingDirection = this.pts.Select(p => p.Value).GetWindingDirection();

        return true;
      }

      private void AddPoint(double x, double y)
      {
        var index = pts.Count() + this.ptIndexOffset;

        pts.Add(index, new Point2D(x, y));
      }

      private Line2D GetLine(IndexPair indexPair)
      {
        return new Line2D(this.pts[indexPair.Indices[0]], this.pts[indexPair.Indices[1]]);
      }
    }
    #endregion

    private CoordinateSystem CoordinateTranslation = null;

    private readonly ClosedLoop ExternalLoop = new ClosedLoop();
    private readonly Dictionary<IndexPair, Line2D> Internals = new Dictionary<IndexPair, Line2D>();
    private readonly List<ClosedLoop> Openings = new List<ClosedLoop>();

    //Assumptions:
    // - input coordinates are an ordered set of external vertices (i.e. not vertices of openings)
    // - the lines defined by these vertices don't intersect each other
    public bool Init(double[] coords, List<double[]> openingCoordsList = null)
    {
      ExternalLoop.Init(coords, ref CoordinateTranslation);

      if (openingCoordsList != null)
      {
        var indexOffset = ExternalLoop.pts.Count();
        foreach (var openingGlobalCoords in openingCoordsList)
        {
          var openingLoop = new ClosedLoop();
          openingLoop.Init(openingGlobalCoords, ref this.CoordinateTranslation, indexOffset);
          indexOffset += openingLoop.pts.Count();
          Openings.Add(openingLoop);
        }
      }
      
      return true;
    }

    //public bool Init(double[] coords, List<double[]> openingCoordsList = null)
    //{
    //  var essential = coords.Essential();
    //  var origPts = new List<Point3D>();
    //  for (var i = 0; i < essential.Length; i += 3)
    //  {
    //    origPts.Add(new Point3D(essential[i], essential[i + 1], essential[i + 2]));
    //  }

    //  if (openingCoordsList != null)
    //  {
    //    foreach (var openingCoords in openingCoordsList)
    //    {
    //      var essentialOpeningCoords = openingCoords.Essential();

    //      for (var i = 0; i < essentialOpeningCoords.Length; i += 3)
    //      {

    //      }
    //    }
    //  }

    //  //Create plane
    //  var plane = Plane.FromPoints(origPts[0], origPts[1], origPts[2]);
    //  var normal = plane.Normal;
    //  var origin = origPts[0];
    //  var xDir = origPts[0].VectorTo(origPts[1]).Normalize();
    //  var yDir = normal.CrossProduct(xDir);

    //  //The CoordinateSystem class in MathNet.Spatial and its methods aren't not very intuitive as discussed in https://github.com/mathnet/mathnet-spatial/issues/53
    //  //Since I don't understand the offsets that seem to be applied by TransformFrom and TransformTo, I focussed on the Transform method,
    //  //which transforms a local point to a global point.
    //  //In order to transform a point from global into local, the coordinate system needs to be reversed so that the resulting coordinateSystem.Transform does the
    //  //transformation from global to local.
    //  CoordinateTranslation = new CoordinateSystem((new CoordinateSystem(origin, xDir, yDir, normal)).Inverse());

    //  //project points onto the plane - if the points are co-planar and translation is done correctly, all Z values should be zero
    //  var nonCoPlanarPts = 0;
    //  var n = origPts.Count();
    //  for (var i = 0; i < n; i++)
    //  {
    //    var projectedPt = coordinateTranslation.Transform(origPts[i]);
    //    if (!projectedPt.Z.AlmostEqual(0, 3))
    //    {
    //      nonCoPlanarPts++;
    //    }
    //    pts.Add(new Point2D(projectedPt.X, projectedPt.Y));
    //  }

    //  for (var i = 0; i < n; i++)
    //  {
    //    var externalIndexPair = new IndexPair(i, (i == (n - 1)) ? 0 : (i + 1));
    //    Externals.Add(externalIndexPair, GetLine(externalIndexPair));
    //  }

    //  if (nonCoPlanarPts > 0)
    //  {
    //    return false;
    //  }

    //  GetWindingDirection();
    //  return true;
    //}

    public int[] Faces()
    {
      var n = ExternalLoop.pts.Count() + ((Openings == null) ? 0 : Openings.Sum(o => o.pts.Count()));
      var faces = new List<int>();

      //Go through each point
      for (var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1) : 0;
        var prevPtIndex = (i > 0) ? i - 1 : (n - 1);
        var dictHalfSweep = PointIndicesSweep(i, nextPtIndex, prevPtIndex);

        if (dictHalfSweep == null)
        {
          //This would only happen if winding direction hasn't been determined yet
          return faces.ToArray();
        }

        if (dictHalfSweep.Keys.Count == 0)
        {
          //This would only happen if the only valid point in the half-sweep is the previous point

          //Check if the internal lines already has the line between the previous and next points, and add it if not
          var prevNextIndexPair = new IndexPair(prevPtIndex, nextPtIndex);
          var line = GetLine(prevNextIndexPair);
          if (ValidNewInternalLine(prevNextIndexPair, line))
          {
            Internals.Add(prevNextIndexPair, line);
          }
        }
        else
        {
          //Go through each direction (ending in an external point) emanating from this candidate point
          foreach (var vector in dictHalfSweep.Keys)
          {
            double? currShortestDistance = null;
            int? shortestPtIndex = null;

            //Go through each point in the same direction from this candidate point (because multiple external points can be in alignment
            //along the same direction)
            foreach (var ptIndex in dictHalfSweep[vector])
            {
              var indexPair = new IndexPair(i, ptIndex);
              var line = GetLine(indexPair);
              if (!ValidNewInternalLine(indexPair, line)) continue;

              //Calculate distance - if currShortestDistance is either -1 or shorter than the shortest, replace the shortest
              var distance = line.Length;
              if (!currShortestDistance.HasValue || (distance < currShortestDistance))
              {
                currShortestDistance = distance;
                shortestPtIndex = ptIndex;
              }
            }

            //Now that the shortest valid line to another point has been found, add it to the list of lines
            if (currShortestDistance > 0 && shortestPtIndex.HasValue && shortestPtIndex > 0)
            {
              //Add line - which has already been checked to ensure it doesn't intersect others, etc
              var shortestIndexPair = new IndexPair(i, shortestPtIndex.Value);
              Internals.Add(shortestIndexPair, GetLine(shortestIndexPair));
              continue;
            }
          }
        }
      }

      //Now determine faces by cycling through each edge line and finding which other point is shared between all lines emanating from this point

      var triangles = new List<TriangleIndexSet>();
      for ( var i = 0; i < n; i++)
      {
        var nextPtIndex = (i < (n - 1)) ? (i + 1) : 0;

        var indicesLinkedToCurr = GetPairedIndices(i);
        indicesLinkedToCurr.Remove(nextPtIndex);
        var indicesLinkedToNext = GetPairedIndices(nextPtIndex);
        indicesLinkedToNext.Remove(i);

        var sharedPtIndices = indicesLinkedToCurr.Where(ci => indicesLinkedToNext.Any(ni => ci == ni)).ToList();

        if (sharedPtIndices != null && sharedPtIndices.Count() > 0)
        {
          if (sharedPtIndices.Count() == 1)
          {
            var currTriangle = new TriangleIndexSet(i, nextPtIndex, sharedPtIndices[0]);
            if (triangles.All(t => !t.Matches(currTriangle)))
            {
              triangles.Add(currTriangle);
            }
          }
          else
          {
            throw new Exception("Found multiple shared indices");
          }
        }
      }

      foreach (var t in triangles)
      {
        faces.Add(0); // signifying a triangle
        faces.AddRange(t.Indices.Take(3));
      }

      return faces.ToArray();
    }

    private Dictionary<IndexPair, Line2D> AllIndexLines
    {
      get
      {
        var pairs = new Dictionary<IndexPair, Line2D>();
        foreach (var k in Internals.Keys)
        {
          pairs.Add(k, Internals[k]);
        }
        foreach (var k in ExternalLoop.Loop.Keys)
        {
          pairs.Add(k, ExternalLoop.Loop[k]);
        }

        foreach (var l in Openings)
        {
          foreach (var k in l.Loop.Keys)
          {
            pairs.Add(k, l.Loop[k]);
          }
        }
        return pairs;
      }
    }

    private bool ExistingLinesContains(IndexPair indexPair)
    {
      var matching = AllIndexLines.Keys.Where(i => i.Matches(indexPair));
      return (matching.Count() > 0);
    }

    private Line2D GetLine(IndexPair indexPair)
    {
      var allPts = GetAllPts();
      return new Line2D(allPts[indexPair.Indices[0]], allPts[indexPair.Indices[1]]);
    }

    private bool ValidNewInternalLine(IndexPair indexPair, Line2D line)
    {
      if (indexPair == null) return false;

      //Check if this line is already in the collection - if so, ignore it
      if (ExistingLinesContains(indexPair)) return false;

      //Check if this line would intersect any external lines in this collection - if so, ignore it
      if (IntersectsBoundaryLines(line)) return false;

      //Check if this line would intersect any already in this collection - if so, ignore it
      if (IntersectsInternalLines(line)) return false;

      return true;
    }

    private List<int> GetPairedIndices(int index)
    {
      return AllIndexLines.Keys.Select(l => l.Other(index)).Where(l => l.HasValue).Cast<int>().ToList();
    }

    private List<Point2D> GetAllPts()
    {
      var allPts = new List<Point2D>();
      allPts.AddRange(ExternalLoop.pts.Select(p => p.Value));
      if (Openings != null)
      {
        foreach (var opening in Openings)
        {
          allPts.AddRange(opening.pts.Select(p => p.Value));
        }
      }
      return allPts;
    }

    private List<Line2D> GetAllBoundaryLines()
    {
      var allBoundaryLines = new List<Line2D>();
      allBoundaryLines.AddRange(ExternalLoop.Loop.Select(p => p.Value));
      foreach (var opening in Openings)
      {
        allBoundaryLines.AddRange(opening.Loop.Select(p => p.Value));
      }
      return allBoundaryLines;
    }

    private bool IntersectsBoundaryLines(Line2D line)
    {
      var allBoundaryLines = GetAllBoundaryLines();
      foreach (var bl in allBoundaryLines)
      {
        if (bl.Intersects(line))
        {
          return true;
        }
      }

      return false;
    }

    private bool IntersectsInternalLines(Line2D line)
    {
      foreach (var ik in Internals.Keys)
      {
        if (Internals[ik].Intersects(line))
        {
          return true;
        }
      }

      return false;
    }

    //Because multiple points can be aligned along the same direction from any given point, a dictionary is returned where
    //the (unit) vectors towards the points are the keys, and all points in that exact direction listed as the values
    private Dictionary<Vector2D, List<int>> PointIndicesSweep(int ptIndex, int nextPtIndex, int prevPtIndex)
    {
      if (ExternalLoop.WindingDirection == 0)
      {
        return null;
      }

      var allPts = GetAllPts();

      var vCurrToNext = allPts[ptIndex].VectorTo(allPts[nextPtIndex]).Normalize();
      var vCurrToPrev = allPts[ptIndex].VectorTo(allPts[prevPtIndex]).Normalize();

      var dict = new Dictionary<Vector2D, List<int>>();

      for (var i = 0; i < allPts.Count(); i++)
      {
        if (i == ptIndex || i == nextPtIndex || i == prevPtIndex) continue;

        var vItem = allPts[ptIndex].VectorTo(allPts[i]).Normalize();

        //The swapping of the vectors below is to align with the fact that the vector angle comparison is always done anti-clockwise
        var isBetween = (ExternalLoop.WindingDirection > 0)
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
  }
}
