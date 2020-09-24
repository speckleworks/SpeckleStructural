using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ResultTests : TestBase
  {
    public ResultTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [TestCase(@"C:\LargeGSAFiles\WithAnalysisResults")]
    public void ExtractionTest(string fileName)
    {

    }
  }
}
