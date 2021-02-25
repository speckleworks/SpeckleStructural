using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using System.IO;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ModelValidationTests : TestBase
  {
    public ModelValidationTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    internal class UnmatchedData
    {
      public List<string> Retrieved;
      public List<string> FromFile;
    }

    [Test]
    public void ReceiverGsaValidationSimple()
    {
      ReceiverGsaValidation("Simple", simpleDataJsonFileNames, GSATargetLayer.Design);
    }

    [Test]
    public void ReceiverGsaValidationNb()
    {
      ReceiverGsaValidation("NB", savedJsonFileNames, GSATargetLayer.Design);
    }

    private void ReceiverGsaValidation(string subdir, string[] jsonFiles, GSATargetLayer layer)
    {
      // Takes a saved Speckle stream with structural objects
      // converts to GWA and sends to GSA
      // then reads the data back out of GSA
      // and compares the two sets of GWA
      // if successful then there will be the same number
      // of each of the keywords in as out

      SpeckleInitializer.Initialize();
      Initialiser.AppResources = new MockGSAApp(proxy: new GSAProxy());
      Initialiser.GsaKit.Clear();
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = layer;
      Initialiser.AppResources.Proxy.NewFile(false);

      var dir = TestDataDirectory;
      if (subdir != String.Empty)
      {
        dir = Path.Combine(TestDataDirectory, subdir);
        dir = dir + @"\"; // TestDataDirectory setup unconvetionally with trailing seperator - follow suit
      }

      var receiverProcessor = new ReceiverProcessor(dir, Initialiser.AppResources);

      // Run conversion to GWA keywords
      // Note that it can be one model split over several json files
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(jsonFiles, out var gwaRecordsFromFile, layer);

      //Run conversion to GWA keywords
      Assert.IsNotNull(gwaRecordsFromFile);
      Assert.IsNotEmpty(gwaRecordsFromFile);

      var typeHierarchy = Initialiser.GsaKit.RxTypeDependencies;
      var keywords = typeHierarchy.Select(i => i.Key.GetGSAKeyword()).ToList();
      keywords.AddRange(typeHierarchy.SelectMany(i => i.Key.GetSubGSAKeyword()));
      keywords = keywords.Where(k => k.Length > 0).Select(k => Helper.RemoveVersionFromKeyword(k)).Distinct().ToList();

      Initialiser.AppResources.Proxy.Sync(); // send GWA to GSA

      var retrievedGwa = Initialiser.AppResources.Proxy.GetGwaData(keywords, true); // read GWA from GSA

      var retrievedDict = new Dictionary<string, List<string>>();
      foreach (var gwa in retrievedGwa)
      {
        Initialiser.AppResources.Proxy.ParseGeneralGwa(gwa.GwaWithoutSet, out string keyword, out _, out _, out _, out _, out _);
        if (!retrievedDict.ContainsKey(keyword))
        {
          retrievedDict.Add(keyword, new List<string>());
        }
        retrievedDict[keyword].Add(gwa.GwaWithoutSet);
      }

      var fromFileDict = new Dictionary<string, List<string>>();
      foreach (var r in gwaRecordsFromFile)
      {
        Initialiser.AppResources.Proxy.ParseGeneralGwa(r.GwaCommand, out string keyword, out _, out _, out _, out string gwaWithoutSet, out _);
        if (!fromFileDict.ContainsKey(keyword))
        {
          fromFileDict.Add(keyword, new List<string>());
        }
        fromFileDict[keyword].Add(gwaWithoutSet);
      }

      Initialiser.AppResources.Proxy.Close();

      var unmatching = new Dictionary<string, UnmatchedData>();
      foreach (var keyword in fromFileDict.Keys)
      {
        if (!retrievedDict.ContainsKey(keyword))
        {
          unmatching[keyword] = new UnmatchedData();
          unmatching[keyword].FromFile = fromFileDict[keyword];
        }
        else if (retrievedDict[keyword].Count != fromFileDict[keyword].Count)
        {
          unmatching[keyword] = new UnmatchedData();
          unmatching[keyword].Retrieved = (retrievedDict.ContainsKey(keyword)) ? retrievedDict[keyword] : null;
          unmatching[keyword].FromFile = fromFileDict[keyword];
        }
      }

      Assert.AreEqual(0, unmatching.Count());

      // GSA sometimes forgets the SID - should check that this has passed through correctly here
    }

    /*
    private Dictionary<Type, List<SpeckleObject>> CollateRxObjectsByType(List<SpeckleObject> rxObjs)
    {
      var rxTypePrereqs = GSA.RxTypeDependencies;
      var rxSpeckleTypes = rxObjs.Select(k => k.GetType()).Distinct().ToList();

      ///[ GSA type , [ SpeckleObjects ]]
      var d = new Dictionary<Type, List<SpeckleObject>>();
      foreach (var o in rxObjs)
      {
        var speckleType = o.GetType();

        var matchingGsaTypes = rxTypePrereqs.Keys.Where(t => dummyObjectDict[t].SpeckleObject.GetType() == speckleType);
        if (matchingGsaTypes.Count() == 0)
        {
          matchingGsaTypes = rxTypePrereqs.Keys.Where(t => speckleType.IsSubclassOf(dummyObjectDict[t].SpeckleObject.GetType()));
        }

        if (matchingGsaTypes.Count() == 0)
        {
          continue;
        }

        var gsaType = matchingGsaTypes.First();
        if (!d.ContainsKey(gsaType))
        {
          d.Add(gsaType, new List<SpeckleObject>());
        }
        d[gsaType].Add(o);
      }

      return d;
    }
    */
  }
}
