using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ReceiverTests : TestBase
  {
    public ReceiverTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution

      //If this isn't called, then the GetObjectSubtypeBetter method in SpeckleCore will cause a {"Value cannot be null.\r\nParameter name: source"} message
      SpeckleInitializer.Initialize();
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" }, "TestGwaRecords.json", GSATargetLayer.Design)]
    public void ReceiverTestDesignLayer(string[] savedJsonFileNames, string expectedGwaPerIdsFile, GSATargetLayer layer)
    {
      DoReceiverTest(savedJsonFileNames, expectedGwaPerIdsFile, layer);
    }

    private void DoReceiverTest(string[] savedJsonFileNames, string expectedGwaPerIdsFile, GSATargetLayer layer)
    {
      var expectedJson = Helper.ReadFile(expectedGwaPerIdsFile, TestDataDirectory);
      var expectedGwaRecords = Helper.DeserialiseJson<List<GwaRecord>>(expectedJson);

      var mockGsaCom = SetupMockGsaCom();
      Initialiser.GSA.InitializeReceiver(mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, Initialiser.GSA);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var actualGwaRecords);
      Assert.IsNotNull(actualGwaRecords);
      Assert.IsNotEmpty(actualGwaRecords);

      Assert.AreEqual(expectedGwaRecords.Count(), actualGwaRecords.Count(), "Number of GWA records don't match");

      var actualUniqueApplicationIds = actualGwaRecords.Select(r => r.ApplicationId).Distinct();
      var expectedUniqueApplicationIds = expectedGwaRecords.Select(r => r.ApplicationId).Distinct();
      Assert.AreEqual(expectedUniqueApplicationIds.Count(), actualUniqueApplicationIds.Count());

      //Check for any actual records missing from expected
      Assert.IsTrue(actualUniqueApplicationIds.All(a => expectedUniqueApplicationIds.Contains(a)));

      //Check any expected records missing in actual
      Assert.IsTrue(expectedUniqueApplicationIds.All(a => actualUniqueApplicationIds.Contains(a)));

      //Check each actual record has match in expected
      foreach (var actualGwaRecord in actualGwaRecords)
      {
        Assert.GreaterOrEqual(1,
          expectedGwaRecords.Count(er => er.ApplicationId == actualGwaRecord.ApplicationId && er.GwaCommand == actualGwaRecord.GwaCommand),
          "Expected record not found in actual records");
      }
    }
  }
}
