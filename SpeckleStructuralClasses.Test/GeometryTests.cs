using System.Linq;
using NUnit.Framework;
using SpeckleCoreGeometryClasses;
using System.Collections.Generic;
using System;
using SpeckleStructuralClasses.PolygonMesher;

namespace SpeckleStructuralClasses.Test
{
  [TestFixture]
  public class GeometryTests
  {
    //The order is important here, as the code assumes it will be a sequential loop of nodes
    [TestCase(4, new double[] { 30.83812141, 7.500011921, 46.72499847, 30.83812141, 7.500011921, 93.44999695, 30.83812141, 7.500011921, 140.1750031, 30.83812141, 7.500011921, 163.5375061, 30.83812141, 7.500011921, 186.8999939, 30.83812141, 16.85632896, 186.8999939, 30.83812141, 16.85632896, 163.5375061, 30.83812141, 16.85632896, 140.1750031, 30.83812141, 16.85632896, 93.44999695, 30.83812141, 16.85632896, 46.72499847, 30.83812141, 16.85632896, 0, 30.83812141, 7.500011921, 0 })]
    [TestCase(5, new double[] { 0, 0, 0, 0, 5, 0, 3, 7, 0, 6, 5, 0, 6, 0, 0 })]
    [TestCase(5, new double[] { 0, 0, 0, 0, 3.5, 0, 0, 5, 0, 3, 7, 0, 6, 5, 0, 6, 0, 0, 2.5, 0, 0 })]
    public void TestPointsReduction(int numExpectedPts, double[] coords)
    {
      var essentialCoords = coords.Essential();
      Assert.AreEqual(numExpectedPts * 3, essentialCoords.Count());
    }

    [Test]
    public void TestCoordinateMappings()
    {
      //< axis vector multiplier, global point , expected local point relative to axis >
      var globalExpected = new List<Tuple<double, SpecklePoint, SpecklePoint>>()
      {
        { new Tuple<double, SpecklePoint, SpecklePoint>(1, new SpecklePoint(3, 4, 5), new SpecklePoint(0,0,0)) },
        { new Tuple<double, SpecklePoint, SpecklePoint>(1, new SpecklePoint(3, 4, 6), new SpecklePoint(0,0,1)) },
        { new Tuple<double, SpecklePoint, SpecklePoint>(2, new SpecklePoint(3, 4 + (2 * Math.Sqrt(8)), 5), new SpecklePoint(2, 2, 0)) }
      };

      foreach (var tuple in globalExpected)
      {
        var axisFactor = tuple.Item1;
        var pt = tuple.Item2;
        var expectedPoint = tuple.Item3;

        var xdir = new StructuralVectorThree(1, 1, 0);
        xdir.Normalise();
        xdir.Scale(axisFactor);
        var ydir = new StructuralVectorThree(-1, 1, 0);
        ydir.Normalise();
        ydir.Scale(axisFactor);

        var axis = new StructuralAxis(xdir, ydir)
        {
          Origin = new SpecklePoint(3, 4, 5)
        };

        var localCoords = pt.Value.MapGlobal2Local(axis);
        Assert.AreEqual(expectedPoint.Value[0], localCoords[0]);
        Assert.AreEqual(expectedPoint.Value[1], localCoords[1]);
        Assert.AreEqual(expectedPoint.Value[2], localCoords[2]);
      }
    }
  }
}
