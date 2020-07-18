using System.Linq;
using NUnit.Framework;

namespace SpeckleStructuralClasses.Test
{
  [TestFixture]
  public class MeshTests
  {
    [Test]
    public void TestKristjanSpeckleConstructor()
    {
      var coor = new double[] {
        41079.0479, 88847.4378, 2850,
        45177.0349, 89591.5655, 2850,
        45177.0349, 89591.5655, 5200,
        46485.6358, 89829.1862, 5200,
        46485.6358, 89829.1862, 2850,
        46756.1572, 89878.3084, 2850,
        46756.1572, 89878.3084, 7450,
        41079.0479, 88847.4378, 7450,
      };
      var dummyMesh = new Structural2DElementMesh(coor, null, Structural2DElementType.Wall, null, null, null);
      Assert.IsNotNull(dummyMesh.Faces);
      var faces = dummyMesh.Faces;
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 1, 2 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 2, 3, 6 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 3, 4, 5 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 3, 5, 6 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 6, 7 }));
      Assert.True(faces.ContainsSublist(new[] { 0, 0, 2, 6 }));
    }
  }
}
