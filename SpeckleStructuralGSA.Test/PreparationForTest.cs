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
      string[] savedJsonFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" }; //Prepared externally 
      string outputGWAFileName = "TestGwaRecords.json";  //Output by this method

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      //ModuleInitialiserCopy.Initialize();
      SpeckleInitializer.Initialize();

      //Read JSON files into objects
      var speckleObjects = Helper.ExtractObjects(savedJsonFileNames);

      var receiverProcessor = new ReceiverProcessor();

      //Run conversion to GWA keywords
      Assert.IsTrue(receiverProcessor.SpeckleToGwa(speckleObjects, out var gwaPerIds));
      
      //Create JSON file containing pairs of ApplicationId and GWA commands
      var obj = new List<GwaRecord>();
      foreach (var tuple in gwaPerIds)
      {
        obj.Add(new GwaRecord(tuple.Item1, tuple.Item2));
      }
      var jsonToWrite = JsonConvert.SerializeObject(obj, Formatting.Indented);

      //Save JSON file
      Helper.WriteFile(jsonToWrite, outputGWAFileName);

      return;
    }

    [Test]
    public void SetUpTransmissionTestData()
    {
      string savedGsaFileName = ""; //Prepared externally
      string outputJsonFileName = ""; //Output by this method

      //Load GSA file and compile all GWA commands with application IDs

      

      return;
    }
  }
}
