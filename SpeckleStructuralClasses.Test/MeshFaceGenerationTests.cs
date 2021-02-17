using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using NUnit.Framework;
using SpeckleStructuralClasses.PolygonMesher;

namespace SpeckleStructuralClasses.Test
{
  [TestFixture]
  public class MeshFaceGenerationTests
  {
    private PolygonMesher.PolygonMesher pm;

    private readonly double[] decagonCoor = new double[]
      {
        0, 123, 13,
        0, 108.485292, 57.671679,
        0, 70.485292, 85.280295,
        0, 23.514708, 85.280295,
        0, -14.485292, 57.671679,
        0, -29, 13,
        0, -14.485292, -31.671679,
        0, 23.514708, -59.280295,
        0, 70.485292, -59.280295,
        0, 108.485292, -31.671679
      };
    private readonly double[] doorwayWall = new double[] {
        41079.0479, 88847.4378, 2850,
        45177.0349, 89591.5655, 2850,
        45177.0349, 89591.5655, 5200,
        46485.6358, 89829.1862, 5200,
        46485.6358, 89829.1862, 2850,
        46756.1572, 89878.3084, 2850,
        46756.1572, 89878.3084, 7450,
        41079.0479, 88847.4378, 7450,
      };


    [SetUp]
    public void MeshSetup()
    {
      pm = new PolygonMesher.PolygonMesher();
    }

    [Test]
    public void TestSublistCheck()
    {
      var sequence = new List<int>() { 1, 3, 4, 7, 2, 4, 6, 8 };
      Assert.IsTrue(sequence.ContainsSublist(new int[] { 1, 3 }));
      Assert.IsFalse(sequence.ContainsSublist(new int[] { 3, 5 }, 2));
      Assert.IsTrue(sequence.ContainsSublist(new int[] { 2, 4, 6, 8 }));
      Assert.IsTrue(sequence.ContainsSublist(new int[] { 2, 4, 6, 8 }, 4));
      Assert.IsTrue(sequence.ContainsSublist(sequence));
    }

    [Test]
    public void TestInsideOutside()
    {
      var pts = new List<Point2D> { new Point2D(0, 0), new Point2D(10, 0), new Point2D(10, 20), new Point2D(0, 20) };
      Assert.True(pts.IsInside(new Point2D(5, 5)));
      Assert.False(pts.IsInside(new Point2D(-5, 5)));
    }

    [Test]
    public void TestDoorway()
    {
      Assert.IsTrue(pm.Init(doorwayWall));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 1, 2 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 2, 3, 6 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 3, 4, 5 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 3, 5, 6 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 6, 7 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 2, 6 }));
    }

    [Test]
    public void TestDecagon()
    {
      Assert.IsTrue(pm.Init(decagonCoor));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      var facesGrouped = new List<int[]>
      {
        new [] { 0, 0, 1, 2 },
        new [] { 0, 0, 2, 3 },
        new [] { 0, 0, 3, 4 },
        new [] { 0, 0, 4, 5 },
        new [] { 0, 0, 5, 6 },
        new [] { 0, 0, 6, 7 },
        new [] { 0, 0, 7, 8 },
        new [] { 0, 0, 8, 9 }
      };
      foreach (var g in facesGrouped)
      {
        Assert.True(faces.ContainsSublist(g));
      }
      Assert.AreEqual(facesGrouped.Count(), faces.Count() / 4);
    }

    [Test]
    public void TestTriangle()
    {
      var coor = new double[] { 0, 0, 0, 100, 0, 0, 80, 50, 0 };

      Assert.IsTrue(pm.Init(coor));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 2
      }));
    }

    [Test]
    public void TestRectangle()
    {
      //Unlike the others, this is already on the x-Y plane
      var coor = new double[] { 111, -29, 0, 52, -29, 0, 83, 13, 0, 174, 13, 0 };

      Assert.IsTrue(pm.Init(coor));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      var facesGrouped = new List<int[]>
      {
        new[] { 0, 0, 1, 2 },
        new[] { 0, 0, 2, 3 }
      };
      foreach (var g in facesGrouped)
      {
        Assert.True(faces.ContainsSublist(g));
      }
      Assert.AreEqual(facesGrouped.Count(), faces.Count() / 4);
    }

    [Test]
    public void TestExtraNonEssentialVertices()
    {
      var coor = new double[] { 0, -107, -1, 0, -192.456877, -1, 0, -234, -1, 0, -234, 31.270637, 0, -234, 82, 0, -168, 20, 0, -107, 82.0 };

      Assert.IsTrue(pm.Init(coor));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      Assert.AreEqual(15, pm.Coordinates.Count());

      var facesGrouped = new List<int[]>
      {
        new[] { 0, 0, 1, 3 },
        new[] { 0, 1, 2, 3 },
        new[] { 0, 0, 3, 4 }
      };
      foreach (var g in facesGrouped)
      {
        Assert.True(faces.ContainsSublist(g));
      }
      Assert.AreEqual(facesGrouped.Count(), faces.Count() / 4);
    }

    [Test]
    public void TestSingleOpeningRectangle()
    {
      var coor = new double[] { 0, 0, 0, 0, 50, 0, 100, 60, 0, 80, 0, 0 };
      var coorOpening = new double[] { 20, 20, 0, 40, 20, 0, 40, 40, 0, 20, 40, 0 };

      Assert.IsTrue(pm.Init(coor, new List<double[]> { coorOpening }));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      var facesGrouped = new List<int[]>
      {
        new[] { 0, 0, 1, 7 },
        new[] { 0, 1, 2, 6 },
        new[] { 0, 2, 3, 5 },
        new[] { 0, 0, 3, 5 },
        new[] { 0, 0, 4, 5 },
        new[] { 0, 2, 5, 6 },
        new[] { 0, 1, 6, 7 },
        new[] { 0, 0, 4, 7 }
      };
      foreach (var g in facesGrouped)
      {
        Assert.True(faces.ContainsSublist(g));
      }
      Assert.AreEqual(facesGrouped.Count(), faces.Count() / 4);
    }

    [Test]
    public void TestMultipleOpenings()
    {
      var diamondCoor = new double[] { -196, -284, 0, -127, -205, 0, -52, -284, 0, -124, -352.476069, 0 };
      var opening1Coor = new double[] { -135, -268, 0, -109, -268, 0, -109, -231, 0, -135, -231, 0 };
      var opening2Coor = new double[] { -127.5, -286, 0, -96.5, -286, 0, -96.5, -278, 0, -127.5, -278, 0 };

      Assert.IsTrue(pm.Init(diamondCoor, new List<double[]> { opening1Coor, opening2Coor }));
      var faces = pm.Faces();
      Assert.IsNotNull(faces);
      var facesGrouped = new List<int[]>
      {
        new[] { 0, 0, 1, 7 },
        new[] { 0, 1, 2, 6 },
        new[] { 0, 2, 3, 9 },
        new[] { 0, 0, 3, 8 },
        new[] { 0, 0, 4, 5 },
        new[] { 0, 2, 5, 6 },
        new[] { 0, 1, 6, 7 },
        new[] { 0, 0, 4, 7 },
        new[] { 0, 3, 8, 9 },
        new[] { 0, 2, 9, 10 },
        new[] { 0, 5, 10, 11 },
        new[] { 0, 0, 8, 11 },
        new [] { 0, 0, 5, 11 },
        new [] { 0, 2, 5, 10 }
      };

      foreach (var g in facesGrouped)
      {
        Assert.True(faces.ContainsSublist(g));
      }
      Assert.AreEqual(facesGrouped.Count(), faces.Count() / 4);
    }

    [Test]
    public void TestKristjanFaces()
    {
      var coord = new double[]
      {
        42106.3537, 83189.9603, -1350,
        42064.965, 83417.8922, -1350,
        42064.965, 83417.8922, 1000,
        41836.2774, 84677.298, 1000,
        41836.2774, 84677.298, -1350,
        41079.0479, 88847.4378, -1350,
        41079.0479, 88847.4378, 2850,
        42106.3537, 83189.9603, 2850
      };

      Assert.IsTrue(pm.Init(coord));
      var faces = pm.Faces();

      Assert.IsNotNull(faces);
    }
  }
}
