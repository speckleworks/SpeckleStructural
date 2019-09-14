using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class SpeckleStructuralGSATest
  {
    #region Reception_Tests
    //Reception test
    //- Load saved JSON files from 
    [Test]
    public void ReceiverTest()
    {
      var savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" }; //Prepared externally 
      var expectedGwaPerIdsFile = "TestGwaRecords.json";

      var expectedJson = Helper.ReadFile(expectedGwaPerIdsFile);
      var expectedGwaRecords = Helper.DeserialiseJson<List<GwaRecord>>(expectedJson);

      var receiverProcessor = new ReceiverProcessor();

      //Run conversion to GWA keywords
      Assert.IsTrue(receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var actualGwaRecords));

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
    #endregion

    [Test]
    #region Transmission_Tests
    public void TransmissionTestAllWithEmbeddedResults()
    {

    }

    [Test]
    public void TransmissionTestAllWithSeparateResults()
    {

    }

    [Test]
    public void TransmissionTestAlResultsOnly()
    {

    }
    #endregion

    /* Might be useful later ..
      var json = Helper.ReadFile(testFileNames.First());
      var jt = JToken.Parse(json);

      //Test if two objects are the same
      var jAreEqual = JToken.DeepEquals(JToken.Parse(@"{""a"":1,""b"":2}"), JToken.Parse(@"{""b"":2,""a"":1}"));
      */
  }
}
