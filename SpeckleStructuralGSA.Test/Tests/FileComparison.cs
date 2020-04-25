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

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class FileComparison : TestBase
  {
    public FileComparison() : base(@"C:\Nicolaas\Testing\Temp\SpeckleGSA\") { }

    [Ignore]
    [TestCase("OLD_TxSpeckleObjectsEmbedded.json", "NEW_TxSpeckleObjectsEmbedded.json")]
    public void CompareFiles(string fn1, string fn2)
    {
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

      Assert.AreEqual(0, notFoundIn2.Count());
      Assert.AreEqual(o2All.Count() - o1All.Count(), notFoundIn1.Count());
    }

    private void Compare(Dictionary<SpeckleObject, string> d1, Dictionary<SpeckleObject, string> d2, out Dictionary<SpeckleObject, List<SpeckleObject>> no1in2)
    {
      var ret = new Dictionary<SpeckleObject, List<SpeckleObject>>();
      var lockObj = new object();
      var debugLock = new object();
      Parallel.ForEach(d1.Keys, o =>
      {
        var j = d1[o];
        var matchingExpected = d2.Where(e => JsonCompareAreEqual(e.Value, j));
        if (matchingExpected == null)
        {
          var nearest = string.IsNullOrEmpty(o.ApplicationId) ? null : d2.Where(e => e.Value.Contains(o.ApplicationId)).Select(kvp => kvp.Key).ToList();
          lock (lockObj) { ret.Add(o, nearest); }

          if (nearest == null)
          {
            lock (debugLock) { Debug.WriteLine("Found no nearest for " + o.ApplicationId); }
          }
          else
          {
            lock (debugLock)
            {
              Debug.WriteLine("Found nearest for " + o.ApplicationId);
              foreach (var n in nearest)
              {
                Debug.WriteLine("Nearest JSON: " + d2[n]);
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
      });
      no1in2 = ret;
    }
  }
}
