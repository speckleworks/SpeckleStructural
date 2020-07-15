using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SpeckleStructuralGSA.Test.Tests
{
  [TestFixture]
  public class MeshFaceGenerationTests
  {
    private static double[] coor = new double[] {
        41079.0479, 88847.4378, 2850,
        45177.0349, 89591.5655, 2850,
        45177.0349, 89591.5655, 5200,
        46485.6358, 89829.1862, 5200,
        46485.6358, 89829.1862, 2850,
        46756.1572, 89878.3084, 2850,
        46756.1572, 89878.3084, 7450,
        41079.0479, 88847.4378, 7450,
      };

    [Test]
    public void Test1()
    {
      var ma = new MeshArea();
      Assert.IsTrue(ma.Init(coor));
    }
  }
}
