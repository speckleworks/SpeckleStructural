using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using NUnit.Framework;
using Speckle2dMesher;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class MeshFaceGenerationTests
  {
    private MeshArea ma;

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
    private readonly double[] decagonOpening1Coor = new double[] { 0, 13.079462, 31.710661, 0, 53.079462, 31.710661, 0, 53.079462, 58.710661, 0, 13.079462, 58.710661 };
    private readonly double[] decagonOpening2Coor = new double[] { 0, 108.485292, -31.671679, 0, 85, 16, 0, 40, -27 }; //This opening has a vertex that is an external vertex
    private readonly double[] doorwayWall =  new double[] {
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
      ma = new MeshArea();
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
      Assert.IsTrue(ma.Init(doorwayWall));
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 2,
        0, 2, 3, 6,
        0, 3, 4, 5,
        0, 3, 5, 6,
        0, 0, 6, 7
        }));

    }

    [Test]
    public void TestDecagon()
    {
      Assert.IsTrue(ma.Init(decagonCoor));
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[] 
      {
        0, 0, 1, 2,
        0, 0, 2, 3,
        0, 0, 3, 4,
        0, 0, 4, 5,
        0, 0, 5, 6,
        0, 0, 6, 7,
        0, 0, 7, 8,
        0, 0, 8, 9
      }
        ));
    }

    [Test]
    public void TestTriangle()
    {
      var coor = new double[] { 0, 0, 0, 100, 0, 0, 80, 50, 0 };

      Assert.IsTrue(ma.Init(coor));
      var faces = ma.Faces();
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
      var coor = new double[] { 111, -29, 0, 52, -29, 0, 83, 13, 0, 174, 13, 0};

      Assert.IsTrue(ma.Init(coor));
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 2,
        0, 0, 2, 3
      }));
    }

    [Test]
    public void TestExtraNonEssentialVertices()
    {
      var coor = new double[] { 0, -107, -1, 0, -192.456877, -1, 0, -234, -1, 0, -234, 31.270637, 0, -234, 82, 0, -168, 20, 0, -107, 82.0 };

      Assert.IsTrue(ma.Init(coor));
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 3,
        0, 1, 2, 3,
        0, 0, 3, 4
      }));
    }

    [Test]
    public void TestSingleOpeningRectangle()
    {
      var coor = new double[] { 0, 0, 0, 0, 50, 0, 100, 60, 0, 80, 0, 0 };
      var coorOpening = new double[] { 20, 20, 0, 40, 20, 0, 40, 40, 0, 20, 40, 0 };

      Assert.IsTrue(ma.Init(coor, new List<double[]> { coorOpening }));
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 7,
        0, 1, 2, 6,
        0, 2, 3, 5,
        0, 0, 3, 5,
        0, 0, 4, 5,
        0, 2, 5, 6,
        0, 1, 6, 7,
        0, 0, 4, 7
      }));
    }

    [Test]
    public void TestMultipleOpenings()
    {
      var diamondCoor = new double[] { -196, -284, 0, -127, -205, 0, -52, -284, 0, -124, -352.476069, 0 };
      var opening1Coor = new double[] { -135, -268, 0, -109, -268, 0, -109, -231, 0, -135, -231, 0 };
      var opening2Coor = new double[] { -127.5, -286, 0, -96.5, -286, 0, -96.5, -278, 0, -127.5, -278, 0 };

      Assert.IsTrue(ma.Init(diamondCoor, new List<double[]> { opening1Coor, opening2Coor }));
      var internalGlobalCoords = ma.GetInternalGlobalCoords();
      var faces = ma.Faces();
      Assert.IsNotNull(faces);
      Assert.True(faces.SequenceEqual(new[]
      {
        0, 0, 1, 7,
        0, 1, 2, 6,
        0, 2, 3, 9,
        0, 0, 3, 8,
        0, 0, 4, 5,
        0, 2, 5, 6,
        0, 1, 6, 7,
        0, 0, 4, 7,
        0, 3, 8, 9,
        0, 2, 9, 10,
        0, 5, 10, 11,
        0, 0, 8, 11
      }));
    }
  }
}
