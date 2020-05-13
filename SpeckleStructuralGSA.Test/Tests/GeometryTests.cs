using System.Linq;
using NUnit.Framework;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA.Test.Tests
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
  }
}
