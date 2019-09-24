using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Interop.Gsa_10_0;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class SenderTests : TestBase
  {
    public SenderTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    public static string[] resultTypes = new[] { "Nodal Reaction", "1D Element Strain Energy Density", "1D Element Force", "Nodal Displacements", "1D Element Stress" };
    public static string[] loadCases = new[] { "A2", "C1" };
    public static string gsaFileName = "20180906 - Existing structure GSA_V7_modified.gwb";

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();
      OpenGsaFile(gsaFileName);
    }

    [TestCase("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true)]
    [TestCase("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false)]
    [TestCase("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true)]
    [TestCase("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false)]
    public void RunTransmissionTest(string inputJsonFileName, GSATargetLayer layer, bool resultsOnly, bool embedResults)
    {
      //Deserialise into Speckle Objects so that these can be compared in any order

      var expectedFullJson = Helper.ReadFile(inputJsonFileName, TestDataDirectory);

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      var expectedObjects = JsonConvert.DeserializeObject<List<SpeckleObject>>(expectedFullJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
      expectedObjects = expectedObjects.OrderBy(a => a.ApplicationId).ToList();

      var actualObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults, loadCases, resultTypes);
      Assert.IsNotNull(actualObjects);

      actualObjects = actualObjects.OrderBy(a => a.ApplicationId).ToList();

      Assert.AreEqual(expectedObjects.Count(), actualObjects.Count());
      Assert.AreEqual(expectedObjects.Count(), expectedObjects.Count());

      var expectedJsons = expectedObjects.Select(e => Regex.Replace(JsonConvert.SerializeObject(e, jsonSettings), jsonDecSearch, "$1")).ToList();

      //Compare each object
      foreach (var actualObject in actualObjects)
      {
        var actualJson = JsonConvert.SerializeObject(actualObject, jsonSettings);

        actualJson = Regex.Replace(actualJson, jsonDecSearch, "$1");

        var matchingExpected = expectedJsons.FirstOrDefault(e => JsonCompareAreEqual(e, actualJson));

        Assert.NotNull(matchingExpected, "Expected and actual JSON representations for " + string.Join(" ", new[] { actualObject.ApplicationId, actualObject.Name, actualObject.Type, actualObject.Hash }));

        expectedJsons.Remove(matchingExpected);
      }
    }

    [OneTimeTearDown]
    public void TearDownTests()
    {
      CloseGsaFile();
    }
  }
}
