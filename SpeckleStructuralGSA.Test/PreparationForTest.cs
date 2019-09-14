using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class PreparationForTest
  {
    //Need:
    // - Reception: JSON for a stream and associated GWA keywords
    // - Transmission: GSA model and associated JSON

    [Test]
    public void SetUpReceptionTestData()
    {
      var savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" }; //Prepared externally 
      var outputGWAFileName = "TestGwaRecords.json";  //Output by this method

      var receiverProcessor = new ReceiverProcessor();

      //Run conversion to GWA keywords
      Assert.IsTrue(receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var gwaRecords));
      
      //Create JSON file containing pairs of ApplicationId and GWA commands
      var jsonToWrite = JsonConvert.SerializeObject(gwaRecords, Formatting.Indented);

      //Save JSON file
      Helper.WriteFile(jsonToWrite, outputGWAFileName);

      return;
    }

    [Test]
    public void SetUpTransmissionTestData()
    {
      var savedGsaFileName = ""; //Prepared externally
      var outputJsonFileName = ""; //Output by this method

      //Load GSA file and compile all GWA commands with application IDs

      

      return;
    }
  }
}
