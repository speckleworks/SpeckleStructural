using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using Microsoft.Expression.Interactivity.Media;
using Moq;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

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
      Initialiser.AppResources = new MockGSAApp();
      //gsaInterfacer = new GSAProxy();
      //gsaCache = new GSACache();

      //Initialiser.Instance.Cache = gsaCache;
      //Initialiser.Instance.Interface = gsaInterfacer;
      //Initialiser.Instance.AppUI = new SpeckleAppUI();
    }

    [SetUp]
    public void BeforeEachTest()
    {
      ((MockGSAApp) Initialiser.AppResources).Settings = new MockSettings();
    }

    [TearDown]
    public void AfterEachTest()
    {
      Initialiser.AppResources.Proxy.Close();
      ((IGSACacheForTesting) Initialiser.AppResources.Cache).Clear();
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(GSATargetLayer.Design)]
    public void ReceiverTestDesignLayer(GSATargetLayer layer)
    {
      RunReceiverTest(savedJsonFileNames, expectedGwaPerIdsFileName, "NB", layer);
    }

    [TestCase(GSATargetLayer.Design, "EC_mxfJ2p.json", 2, 2, 2, 2, 4)]
    [TestCase(GSATargetLayer.Analysis, "EC_mxfJ2p.json", 2, 2, 2, 2, 4)]
    public void ReceiverTestLoadRelated(GSATargetLayer layer, string fileName,
      int expectedNum0DLoads, int expectedNum2DBeamLoads, int expectedNum2dFaceLoads, int expectedNumLoadTasks, int expectedLoadCombos)
    {
      var json = Helper.ReadFile(fileName, TestDataDirectory);

      var mockGsaCom = SetupMockGsaCom();
      Initialiser.AppResources.Proxy.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, Initialiser.AppResources);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(new[] { fileName }, out var actualGwaRecords, layer);
      Assert.IsNotNull(actualGwaRecords);
      Assert.IsNotEmpty(actualGwaRecords);

      var keywords = Helper.GetTypeCastPriority(ioDirection.Receive, layer, false).Select(i => i.Key.GetGSAKeyword()).Distinct().ToList();

      //Log outcome to file

      var actualGwaRecordsForKeyword = new Dictionary<string, List<string>>();
      for (var i = 0; i < actualGwaRecords.Count(); i++)
      {
        Initialiser.AppResources.Proxy.ParseGeneralGwa(actualGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
        if (!actualGwaRecordsForKeyword.ContainsKey(recordKeyword))
        {
          actualGwaRecordsForKeyword.Add(recordKeyword, new List<string>());
        }
        actualGwaRecordsForKeyword[recordKeyword].Add(actualGwaRecords[i].GwaCommand);
      }

      Assert.AreEqual(expectedNum0DLoads, actualGwaRecords.Where(r => r.GwaCommand.Contains("LOAD_NODE")).Count());
      Assert.AreEqual(expectedNum2DBeamLoads, actualGwaRecords.Where(r => r.GwaCommand.Contains("LOAD_BEAM_UDL")).Count());
      Assert.AreEqual(expectedNum2dFaceLoads, actualGwaRecords.Where(r => r.GwaCommand.Contains("LOAD_2D_FACE")).Count());
      Assert.AreEqual(expectedNumLoadTasks, actualGwaRecords.Where(r => r.GwaCommand.Contains("TASK")).Count());
      Assert.AreEqual(expectedLoadCombos, actualGwaRecords.Where(r => r.GwaCommand.Contains("COMBINATION")).Count());
      /*
      Assert.AreEqual(expectedNum2DBeamLoads, actualGwaRecordsForKeyword["LOAD_BEAM_UDL.2"].Distinct().Count());
      Assert.AreEqual(expectedNum2dFaceLoads, actualGwaRecordsForKeyword["LOAD_2D_FACE.2"].Distinct().Count());
      Assert.AreEqual(expectedNumLoadTasks, actualGwaRecordsForKeyword["TASK.1"].Distinct().Count());
      Assert.AreEqual(expectedLoadCombos, actualGwaRecordsForKeyword["COMBINATION.1"].Distinct().Count());
      */
    }

    [Ignore("Just used for debugging at this stage, will be finished in the future as a test")]
    [TestCase(GSATargetLayer.Design, "gMu-Xgpc.json")]
    //[TestCase(GSATargetLayer.Analysis, "S5pNxjmUH.json")]
    public void ReceiverTestForDebug(GSATargetLayer layer, string fileName)
    {
      var json = Helper.ReadFile(fileName, TestDataDirectory);

      var mockGsaCom = SetupMockGsaCom();
      Initialiser.AppResources.Proxy.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, Initialiser.AppResources);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(new[] { fileName }, out var actualGwaRecords, layer);
      Assert.IsNotNull(actualGwaRecords);
      Assert.IsNotEmpty(actualGwaRecords);

      var keywords = Helper.GetTypeCastPriority(ioDirection.Receive, layer, false).Select(i => i.Key.GetGSAKeyword()).Distinct().ToList();

      //Log outcome to file

      foreach (var keyword in keywords)
      {
        var actualGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < actualGwaRecords.Count(); i++)
        {
          Initialiser.AppResources.Proxy.ParseGeneralGwa(actualGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            actualGwaRecordsForKeyword.Add(actualGwaRecords[i]);
          }
        }

        var actualUniqueApplicationIds = actualGwaRecordsForKeyword.Where(r => !string.IsNullOrEmpty(r.ApplicationId)).Select(r => r.ApplicationId).Distinct();
      }
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(GSATargetLayer.Design)]
    public void ReceiverTestBlankRefsDesignLayer(GSATargetLayer layer)
    {
      RunReceiverTest(savedBlankRefsJsonFileNames, expectedBlankRefsGwaPerIdsFileName, "Blank",layer);
    }

    #region addition_fns
    private void RunReceiverTest(string[] savedJsonFileNames, string expectedGwaPerIdsFile, string subdir,GSATargetLayer layer)
    {
      var dir = System.IO.Path.Combine(TestDataDirectory, subdir) + "\\";
      
      var expectedJson = Helper.ReadFile(expectedGwaPerIdsFile, dir);
      var expectedGwaRecords = Helper.DeserialiseJson<List<GwaRecord>>(expectedJson);

      var mockGsaCom = SetupMockGsaCom();
      Initialiser.AppResources.Proxy.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(dir, Initialiser.AppResources);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var actualGwaRecords, layer);
      Assert.IsNotNull(actualGwaRecords);
      Assert.IsNotEmpty(actualGwaRecords);

      var keywords = Helper.GetTypeCastPriority(ioDirection.Receive, layer, false).Select(i => i.Key.GetGSAKeyword()).Distinct().ToList();

      //Log outcome to file

      foreach (var keyword in keywords)
      {
        var expectedGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < expectedGwaRecords.Count(); i++)
        {
          Initialiser.AppResources.Proxy.ParseGeneralGwa(expectedGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            expectedGwaRecordsForKeyword.Add(expectedGwaRecords[i]);
          }
        }

        var actualGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < actualGwaRecords.Count(); i++)
        {
          Initialiser.AppResources.Proxy.ParseGeneralGwa(actualGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
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
    #endregion
  }
}
