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

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();
      OpenGsaFile("20180906 - Existing structure GSA_V7_modified.gwb");
    }

    [TestCase("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true)]
    [TestCase("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false, new[] { "A2", "C1" }, new[] { "Nodal Reaction", "0D Element Displacement", "1D Element Force" })]
    [TestCase("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true, new[] { "A2", "C1" }, new[] { "Nodal Reaction", "0D Element Displacement", "1D Element Force" })]
    [TestCase("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false, new[] { "A2", "C1" }, new[] { "Nodal Reaction", "0D Element Displacement", "1D Element Force" })]
    public void RunTransmissionTest(string inputJsonFileName, GSATargetLayer layer, bool resultsOnly, bool embedResults, string[] cases = null, string[] resultsToSend = null)
    {
      //Deserialise into Speckle Objects so that these can be compared in any order

      var expectedFullJson = Helper.ReadFile(inputJsonFileName, TestDataDirectory);

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      var expectedObjects = JsonConvert.DeserializeObject<List<SpeckleObject>>(expectedFullJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
      expectedObjects = expectedObjects.OrderBy(a => a.ApplicationId).ToList();

      var actualObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults, cases, resultsToSend);
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
