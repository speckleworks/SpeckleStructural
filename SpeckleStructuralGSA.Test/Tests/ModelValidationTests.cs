using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ModelValidationTests : TestBase
  {
    public ModelValidationTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [SetUp]
    public void BeforeEachTest()
    {
      Initialiser.Settings = new Settings();
    }

    [Test]
    public void ReceiverGsaValidation()
    {
      SpeckleInitializer.Initialize();
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();

      Initialiser.Cache = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.AppUI = new SpeckleAppUI();
      gsaInterfacer.NewFile(false);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(ReceiverTests.savedJsonFileNames, out var gwaRecordsFromFile, GSATargetLayer.Design);

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
  }
}
