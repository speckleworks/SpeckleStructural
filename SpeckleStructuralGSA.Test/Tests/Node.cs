using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class Node
  {
    [Test]
    public void RoundTrip()
    {
      SpeckleStructuralClasses.StructuralNode n = new SpeckleStructuralClasses.StructuralNode();
      n.Value = new List<double> { 1.0, 2.0, 3.0 };
      var expected = JsonConvert.SerializeObject(n);

      SpeckleStructuralGSA.GSANode gn = new GSANode();
      gn.Value = n;
      gn.SetGWACommand();
      gn.ParseGWACommand();

      var actual = JsonConvert.SerializeObject(gn.Value);

      Assert.AreEqual(expected, actual);
    }
  }
}
