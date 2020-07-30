using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
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
    public static string[] savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" };
    public static string expectedGwaPerIdsFileName = "TestGwaRecords.json";

    public static string[] savedBlankRefsJsonFileNames = new[] { "P40rt5c8I.json" };
    public static string expectedBlankRefsGwaPerIdsFileName = "BlankRefsGwaRefords.json";

    public static string[] savedSharedLoadPlaneJsonFileNames = new[] { "nagwSLyPE.json" };
    public static string expectedSharedLoadPlaneGwaPerIdsFileName = "SharedLoadPlaneGwaRefords.json";

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

      Initialiser.Cache = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.AppUI = new SpeckleAppUI();
    }

    [SetUp]
    public void BeforeEachTest()
    {
      Initialiser.Settings = new Settings();
    }

    [TearDown]
    public void AfterEachTest()
    {
      Initialiser.Interface.Close();
      ((IGSACacheForTesting) Initialiser.Cache).Clear();
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(GSATargetLayer.Design)]
    public void ReceiverTestDesignLayer(GSATargetLayer layer)
    {
      RunReceiverTest(savedJsonFileNames, expectedGwaPerIdsFileName, layer);
    }

    [Ignore("Just used for debugging at this stage, will be finished in the future as a test")]
    [TestCase(GSATargetLayer.Analysis, "8V_CIKfmt.json")]
    //[TestCase(GSATargetLayer.Analysis, "S5pNxjmUH.json")]
    public void ReceiverTestForDebug(GSATargetLayer layer, string fileName)
    {
      var json = Helper.ReadFile(fileName, TestDataDirectory);

      var mockGsaCom = SetupMockGsaCom();
      gsaInterfacer.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache);

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
          Initialiser.Interface.ParseGeneralGwa(actualGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            actualGwaRecordsForKeyword.Add(actualGwaRecords[i]);
          }
        }

        var actualUniqueApplicationIds = actualGwaRecordsForKeyword.Where(r => !string.IsNullOrEmpty(r.ApplicationId)).Select(r => r.ApplicationId).Distinct();
      }
    }

    [Test]
    public void ReceiverGsaValidation()
    {
      Initialiser.Interface = new GSAProxy();
      Initialiser.Cache = new GSACache();
      Initialiser.AppUI = new SpeckleAppUI();
      Initialiser.Interface.NewFile(false);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var gwaRecordsFromFile, GSATargetLayer.Design);

      //Run conversion to GWA keywords
      Assert.IsNotNull(gwaRecordsFromFile);
      Assert.IsNotEmpty(gwaRecordsFromFile);

      var designTypeHierarchy = Helper.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Design, false);
      var analysisTypeHierarchy = Helper.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Analysis, false);
      var keywords = designTypeHierarchy.Select(i => i.Key.GetGSAKeyword()).ToList();
      keywords.AddRange(designTypeHierarchy.SelectMany(i => i.Key.GetSubGSAKeyword()));
      keywords.AddRange(analysisTypeHierarchy.Select(i => i.Key.GetGSAKeyword()));
      keywords.AddRange(analysisTypeHierarchy.SelectMany(i => i.Key.GetSubGSAKeyword()));
      keywords = keywords.Distinct().ToList();

      foreach (var gwa in gwaRecordsFromFile.Select(r => r.GwaCommand))
      {
        Initialiser.Interface.SetGwa(gwa);
      }

      Initialiser.Interface.Sync();

      var retrievedGwa = Initialiser.Interface.GetGwaData(keywords, true);

      var retrievedDict = new Dictionary<string, List<string>>();
      foreach (var gwa in retrievedGwa)
      {
        Initialiser.Interface.ParseGeneralGwa(gwa.GwaWithoutSet, out string keyword, out _, out _, out _, out _, out _);
        if (!retrievedDict.ContainsKey(keyword))
        {
          retrievedDict.Add(keyword, new List<string>());
        }
        retrievedDict[keyword].Add(gwa.GwaWithoutSet);
      }

      var fromFileDict = new Dictionary<string, List<string>>();
      foreach (var r in gwaRecordsFromFile)
      {
        Initialiser.Interface.ParseGeneralGwa(r.GwaCommand, out string keyword, out _, out _, out _, out _, out _);
        if (!fromFileDict.ContainsKey(keyword))
        {
          fromFileDict.Add(keyword, new List<string>());
        }
        fromFileDict[keyword].Add(r.GwaCommand);
      }

      Initialiser.Interface.Close();

      var unmatching = new Dictionary<string, (List<string> retrieved, List<string> fromFile)>();
      foreach (var keyword in fromFileDict.Keys)
      {
        if ((!retrievedDict.ContainsKey(keyword)) || (retrievedDict[keyword].Count != fromFileDict[keyword].Count))
        {
          unmatching[keyword] = (retrievedDict.ContainsKey(keyword) ? retrievedDict[keyword] : null, fromFileDict.ContainsKey(keyword) ? fromFileDict[keyword] : null);
        }
      }

      Assert.AreEqual(0, unmatching.Count());
      
    }

    //Reception test
    //- Load saved JSON files from 
    [TestCase(GSATargetLayer.Design)]
    public void ReceiverTestBlankRefsDesignLayer(GSATargetLayer layer)
    {
      RunReceiverTest(savedBlankRefsJsonFileNames, expectedBlankRefsGwaPerIdsFileName, layer);
    }

    private void RunReceiverTest(string[] savedJsonFileNames, string expectedGwaPerIdsFile, GSATargetLayer layer)
    {
      var expectedJson = Helper.ReadFile(expectedGwaPerIdsFile, TestDataDirectory);
      var expectedGwaRecords = Helper.DeserialiseJson<List<GwaRecord>>(expectedJson);

      var mockGsaCom = SetupMockGsaCom();
      gsaInterfacer.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache);

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
          Initialiser.Interface.ParseGeneralGwa(expectedGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
          if (recordKeyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
          {
            expectedGwaRecordsForKeyword.Add(expectedGwaRecords[i]);
          }
        }

        var actualGwaRecordsForKeyword = new List<GwaRecord>();
        for (var i = 0; i < actualGwaRecords.Count(); i++)
        {
          Initialiser.Interface.ParseGeneralGwa(actualGwaRecords[i].GwaCommand, out var recordKeyword, out var foundIndex, out var foundStreamId, out string foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);
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
