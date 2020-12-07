using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SpeckleCore;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class FileComparison : TestBase
  {
    public FileComparison() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [Ignore("Just as a utility at this stage")]
    [TestCase("OLD_TxSpeckleObjectsNotEmbedded.json", "TxSpeckleObjectsNotEmbedded.json")]
    [TestCase("OLD_TxSpeckleObjectsEmbedded.json", "TxSpeckleObjectsEmbedded.json")]
    [TestCase("OLD_TxSpeckleObjectsResultsOnly.json", "TxSpeckleObjectsResultsOnly.json")]
    [TestCase("OLD_TxSpeckleObjectsDesignLayer.json", "TxSpeckleObjectsDesignLayer.json")]
    public void CompareFiles(string fn1, string fn2)
    {
      Debug.WriteLine("----------------");
      Debug.WriteLine("Comparing: " + fn1 + " and " + fn2);
      SpeckleInitializer.Initialize();

      var j1Full = Helper.ReadFile(fn1, TestDataDirectory);
      var j2Full = Helper.ReadFile(fn2, TestDataDirectory);

      var o1All = JsonConvert.DeserializeObject<List<SpeckleObject>>(j1Full, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
      var o2All = JsonConvert.DeserializeObject<List<SpeckleObject>>(j2Full, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

      var j1List = o1All.ToDictionary(o => o, o => Regex.Replace(JsonConvert.SerializeObject(o, jsonSettings), jsonDecSearch, "$1"));
      var j2List = o2All.ToDictionary(o => o, o => Regex.Replace(JsonConvert.SerializeObject(o, jsonSettings), jsonDecSearch, "$1"));

      Debug.WriteLine("Listing items in 1 that aren't in 2");

      Compare(j1List, j2List, out Dictionary<SpeckleObject, List<SpeckleObject>> notFoundIn2);

      Debug.WriteLine("Listing items in 2 that aren't in 1");
      Compare(j2List, j1List, out Dictionary<SpeckleObject, List<SpeckleObject>> notFoundIn1);

      Debug.WriteLine("Number of in 1 that isn't found in 2: " + notFoundIn2.Count());
      Debug.WriteLine("Number of in 2 that isn't found in 1: " + notFoundIn1.Count());

      Assert.AreEqual(0, notFoundIn2.Count());
      Assert.AreEqual(o2All.Count() - o1All.Count(), notFoundIn1.Count());
    }

    private bool ResultMatch(StructuralResultBase a, StructuralResultBase b)
    {
      return (a.TargetRef == b.TargetRef && a.LoadCaseRef == b.LoadCaseRef && a.Description == b.Description);
    }

    private void Compare(Dictionary<SpeckleObject, string> d1, Dictionary<SpeckleObject, string> d2, out Dictionary<SpeckleObject, List<SpeckleObject>> no1in2)
    {
      var ret = new Dictionary<SpeckleObject, List<SpeckleObject>>();
      var lockObj = new object();
      var debugLock = new object();
#if DEBUG
      foreach (var o in d1.Keys)
#else
      Parallel.ForEach(d1.Keys, o =>
#endif
      {
        var j = d1[o];
        var matchingExpected = d2.Where(e => JsonCompareAreEqual(e.Value, j));
        if (matchingExpected == null || matchingExpected.Count() == 0)
        {
          if (o.ApplicationId == null && o is StructuralResultBase)
          {
            var r = (StructuralResultBase)o;
            var d2results = d2.Where(kvp => kvp.Key is StructuralResultBase).ToList();

            var nearest = d2results.Where(d => ResultMatch((StructuralResultBase)d.Key, r)).Select(k => k.Key).ToList();

            if (nearest == null || nearest.Count() == 0)
            {
              lock (debugLock) { Debug.WriteLine("Found no nearest for result object " + r.TargetRef + "-" + r.LoadCaseRef + "-" + r.Description); }
            }
            else
            {
              lock (debugLock)
              {
                Debug.WriteLine("Found nearest result object " + r.TargetRef + "-" + r.LoadCaseRef + "-" + r.Description);
                Debug.WriteLine(".. with JSON: " + j);
                foreach (var n in nearest)
                {
                  Debug.WriteLine("Nearest JSON: " + d2[n]);
                }
              }
            }
          }
          else
          {
            //if (o.ApplicationId != null)
            //{
            //  var oAppId = Regex.Replace(o.ApplicationId, @"(?<=\.)(.*)(?=_)", "");
            //}
            
            var nearest = string.IsNullOrEmpty(o.ApplicationId) 
              ? null 
              : d2.Where(e => Helper.RemoveKeywordVersionFromApplicationIds(e.Key.ApplicationId) == Helper.RemoveKeywordVersionFromApplicationIds(o.ApplicationId)).Select(kvp => kvp.Key).ToList();
            lock (lockObj) { ret.Add(o, nearest); }

            if (nearest == null || nearest.Count() == 0)
            {
              lock (debugLock) 
              { 
                Debug.WriteLine("Found no nearest for " + o.ApplicationId); 
              }
            }
            else
            {
              lock (debugLock)
              {
                Debug.WriteLine("Found nearest for " + o.ApplicationId);
                Debug.WriteLine(".. with JSON: " + j);
                foreach (var n in nearest)
                {
                  Debug.WriteLine("Nearest JSON: " + d2[n]);
                }
              }
            }
          }
        }
        else
        {
          foreach (var m in matchingExpected)
          {
            /*
            Debug.WriteLine("Found match for " + o.ApplicationId );
            Debug.WriteLine("Search JSON: " + d1[o]);
            Debug.WriteLine("Found JSON: " + m.Value);
            */
          }
        }
      }
#if !DEBUG
      );
#endif
      no1in2 = ret;
    }
  }
}
