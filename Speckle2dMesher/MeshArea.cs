using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Spatial.Euclidean;

namespace Speckle2dMesher
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

    private class MeshPoint
    {
      public int Index;
      public Point2D Local;
      public Point3D Global;
      public MeshPoint(int index, Point2D local, Point3D global)
      {
        this.Local = local;
        this.Index = index;
        this.Global = global;
      }
    }

    private class ClosedLoop
    {
      public List<MeshPoint> MeshPoints = new List<MeshPoint>();
      public Dictionary<IndexPair, Line2D> IndexedLines = new Dictionary<IndexPair, Line2D>();
      public int WindingDirection;
      public CoordinateSystem CoordinateTranslation;

      public bool Init(double[] globalCoords, ref CoordinateSystem CoordinateTranslation, int ptIndexOffset = 0)
      {
        var essential = globalCoords.Essential();

        var origPts = new List<Point3D>();
        for (var i = 0; i < essential.Length; i += 3)
        {
          origPts.Add(new Point3D(essential[i], essential[i + 1], essential[i + 2]));
        }

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
          var localPt = new Point2D(projectedPt.X, projectedPt.Y);
          MeshPoints.Add(new MeshPoint(ptIndexOffset + i, localPt, origPts[i]));
        }

        WindingDirection = 0;
        IndexedLines = new Dictionary<IndexPair, Line2D>();
        if (nonCoPlanarPts > 0)
        {
          return false;
        }

        for (var i = 0; i < n; i++)
        {
          var indexPair = new IndexPair(ptIndexOffset + i, ptIndexOffset + ((i == (n - 1)) ? 0 : (i + 1)));
          IndexedLines.Add(indexPair, new Line2D(MeshPointByIndex(indexPair.Indices[0]).Local, MeshPointByIndex(indexPair.Indices[1]).Local));
        }

        WindingDirection = MeshPoints.Select(mp => mp.Local).GetWindingDirection();

        return true;
      }

      public int NextIndex(int currIndex) => (currIndex == MeshPoints.Last().Index) ? MeshPoints.First().Index : currIndex + 1;
      public int PrevIndex(int currIndex) => (currIndex == MeshPoints.First().Index) ? MeshPoints.Last().Index : currIndex - 1;
      public int FirstIndex() => MeshPoints.First().Index;
      public int LastIndex() => MeshPoints.Last().Index;

      public void ReverseDirection()
      {
        WindingDirection *= -1;
      }

      private MeshPoint MeshPointByIndex(int index) => MeshPoints.FirstOrDefault(mp => mp.Index == index);
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
        var indexOffset = ExternalLoop.MeshPoints.Count();
        foreach (var openingGlobalCoords in openingCoordsList)
        {
          var openingLoop = new ClosedLoop();
          openingLoop.Init(openingGlobalCoords, ref this.CoordinateTranslation, indexOffset);
          //openings are inverse loops - the inside should be considered the outside, to reverse the winding order
          openingLoop.ReverseDirection();
          indexOffset += openingLoop.MeshPoints.Count();
          Openings.Add(openingLoop);
        }
      }
      
      return true;
    }

    public bool GenerateInternals()
    {
      var loops = GetLoops();

      //Go through each loop
      foreach (var l in loops)
      {
        for (var i = l.FirstIndex(); i <= l.LastIndex(); i++)
        {
          var nextPtIndex = l.NextIndex(i);
          var prevPtIndex = l.PrevIndex(i);
          var dictHalfSweep = PointIndicesSweep(i, nextPtIndex, prevPtIndex, l.WindingDirection);

          if (dictHalfSweep == null)
          {
            //This would only happen if winding direction hasn't been determined yet
            return false;
          }

          if (dictHalfSweep.Keys.Count > 0)
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
      }

      return true;
    }

    public List<double[]> GetInternalGlobalCoords()
    {
      var l = new List<double[]>();
      var indexPoints = AllGlobalPoints;

      foreach (var internalPair in Internals.Keys)
      {
        var startPt = indexPoints[internalPair.Indices[0]];
        var endPt = indexPoints[internalPair.Indices[1]];
        l.Add(new double[] { startPt.X, startPt.Y, startPt.Z, endPt.X, endPt.Y, endPt.Z });
      }

      return l;
    }

    public int[] Faces()
    {
      var faces = new List<int>();

      //Now determine faces by cycling through each edge line and finding which other point is shared between all lines emanating from this point

      var triangles = new List<TriangleIndexSet>();
      var boundaryIndexPairs = GetAllBoundaryIndexPairs();

      foreach (var l in GetLoops())
      {
        for(var i = l.FirstIndex(); i <= l.LastIndex(); i++)
        {
          var nextPtIndex = l.NextIndex(i);
          var indicesLinkedToCurr = GetPairedIndices(i).Except(new[] { nextPtIndex }).ToList();
          var indicesLinkedToNext = GetPairedIndices(nextPtIndex).Except(new[] { i }).ToList();

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
      }

      foreach (var t in triangles)
      {
        faces.Add(0); // signifying a triangle
        faces.AddRange(t.Indices.Take(3));
      }

      return faces.ToArray();
    }

    private Dictionary<IndexPair, Line2D> AllIndexLocalLines
    {
      get
      {
        var pairs = new Dictionary<IndexPair, Line2D>();
        foreach (var k in Internals.Keys)
        {
          pairs.Add(k, Internals[k]);
        }
        foreach (var k in ExternalLoop.IndexedLines.Keys)
        {
          pairs.Add(k, ExternalLoop.IndexedLines[k]);
        }

        foreach (var l in Openings)
        {
          foreach (var k in l.IndexedLines.Keys)
          {
            pairs.Add(k, l.IndexedLines[k]);
          }
        }
        return pairs;
      }
    }

    private Dictionary<int, Point3D> AllGlobalPoints
    {
      get
      {
        var indexPoints = new Dictionary<int, Point3D>();

        foreach (var l in GetLoops())
        {
          foreach (var mp in l.MeshPoints)
          {
            indexPoints.Add(mp.Index, mp.Global);
          }
        }
        return indexPoints;
      }
    }

    private bool ExistingLinesContains(IndexPair indexPair)
    {
      var matching = AllIndexLocalLines.Keys.Where(i => i.Matches(indexPair));
      return (matching.Count() > 0);
    }

    private Line2D GetLine(IndexPair indexPair)
    {
      var allPts = GetAllPts();
      return new Line2D(allPts[indexPair.Indices[0]], allPts[indexPair.Indices[1]]);
    }

    private List<ClosedLoop> GetLoops()
    {
      var l = new List<ClosedLoop>() { ExternalLoop };
      if (Openings != null && Openings.Count() > 0)
      {
        l.AddRange(Openings);
      }
      return l;
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
      return AllIndexLocalLines.Keys.Select(l => l.Other(index)).Where(l => l.HasValue).Cast<int>().ToList();
    }

    private List<Point2D> GetAllPts()
    {
      var allPts = new List<Point2D>();
      allPts.AddRange(ExternalLoop.MeshPoints.Select(mp => mp.Local));
      if (Openings != null)
      {
        foreach (var opening in Openings)
        {
          allPts.AddRange(opening.MeshPoints.Select(p => p.Local));
        }
      }
      return allPts;
    }

    private List<Line2D> GetAllBoundaryLines()
    {
      var allBoundaryLines = new List<Line2D>();
      allBoundaryLines.AddRange(ExternalLoop.IndexedLines.Select(p => p.Value));
      foreach (var opening in Openings)
      {
        allBoundaryLines.AddRange(opening.IndexedLines.Select(p => p.Value));
      }
      return allBoundaryLines;
    }

    private List<IndexPair> GetAllBoundaryIndexPairs()
    {
      var allBoundaryPairs = new List<IndexPair>();
      allBoundaryPairs.AddRange(ExternalLoop.IndexedLines.Select(p => p.Key));
      foreach (var opening in Openings)
      {
        allBoundaryPairs.AddRange(opening.IndexedLines.Select(p => p.Key));
      }
      return allBoundaryPairs;
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
    private Dictionary<Vector2D, List<int>> PointIndicesSweep(int ptIndex, int nextPtIndex, int prevPtIndex, int windingDirection)
    {
      if (windingDirection == 0)
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
        var isBetween = (windingDirection > 0) ? vItem.IsBetweenVectors(vCurrToNext, vCurrToPrev) : vItem.IsBetweenVectors(vCurrToPrev, vCurrToNext);

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
