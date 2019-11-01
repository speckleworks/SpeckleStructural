using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ReceiverTests : TestBase
  {
    public static string[] savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" };
    public static string expectedGwaPerIdsFileName = "TestGwaRecords.json";

    public ReceiverTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution

      //If this isn't called, then the GetObjectSubtypeBetter method in SpeckleCore will cause a {"Value cannot be null.\r\nParameter name: source"} message
      SpeckleInitializer.Initialize();
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();

      Initialiser.Indexer = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.Settings = new Settings();
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(GSATargetLayer.Design)]
    public void ReceiverTestDesignLayer(GSATargetLayer layer)
    {
      RunReceiverTest(savedJsonFileNames, expectedGwaPerIdsFileName, layer);
    }

    private void RunReceiverTest(string[] savedJsonFileNames, string expectedGwaPerIdsFile, GSATargetLayer layer)
    {
      var expectedJson = Helper.ReadFile(expectedGwaPerIdsFile, TestDataDirectory);
      var expectedGwaRecords = Helper.DeserialiseJson<List<GwaRecord>>(expectedJson);

      var mockGsaCom = SetupMockGsaCom();
      ((GSAProxy)gsaInterfacer).OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var actualGwaRecords);
      Assert.IsNotNull(actualGwaRecords);
      Assert.IsNotEmpty(actualGwaRecords);

      var keywords = receiverProcessor.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Design, false).Select(i => i.Key.GetGSAKeyword()).Distinct().ToList();

      //Log outcome to file

      foreach (var keyword in keywords)
      {
        var expectedGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < expectedGwaRecords.Count(); i++)
        {
          expectedGwaRecords[i].GwaCommand.ExtractKeywordApplicationId(out var recordKeyword, out var foundIndex, out var applicationId, out var gwaWithoutSet);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            expectedGwaRecordsForKeyword.Add(expectedGwaRecords[i]);
          }
        }

        var actualGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < actualGwaRecords.Count(); i++)
        {
          actualGwaRecords[i].GwaCommand.ExtractKeywordApplicationId(out var recordKeyword, out var foundIndex, out var applicationId, out var gwaWithoutSet);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            actualGwaRecordsForKeyword.Add(actualGwaRecords[i]);
          }
        }

        if (expectedGwaRecordsForKeyword.Count() == 0) continue;

        Assert.GreaterOrEqual(actualGwaRecordsForKeyword.Count(), expectedGwaRecordsForKeyword.Count(), "Number of GWA records don't match");

        var actualUniqueApplicationIds = actualGwaRecordsForKeyword.Where(r => !string.IsNullOrEmpty(r.ApplicationId)).Select(r => r.ApplicationId).Distinct();
        var expectedUniqueApplicationIds = expectedGwaRecordsForKeyword.Where(r => !string.IsNullOrEmpty(r.ApplicationId)).Select(r => r.ApplicationId).Distinct();
        Assert.AreEqual(expectedUniqueApplicationIds.Count(), actualUniqueApplicationIds.Count());

        //Check for any actual records missing from expected
        Assert.IsTrue(actualUniqueApplicationIds.All(a => expectedUniqueApplicationIds.Contains(a)));

        //Check any expected records missing in actual
        Assert.IsTrue(expectedUniqueApplicationIds.All(a => actualUniqueApplicationIds.Contains(a)));

        //Check each actual record has match in expected
        foreach (var actualGwaRecord in actualGwaRecordsForKeyword)
        {
          Assert.GreaterOrEqual(1,
            expectedGwaRecordsForKeyword.Count(er => er.ApplicationId == actualGwaRecord.ApplicationId && er.GwaCommand.Equals(actualGwaRecord.GwaCommand, StringComparison.InvariantCultureIgnoreCase)),
            "Expected record not found in actual records");
        }
      }
    }
  }
}
