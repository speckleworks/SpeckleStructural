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
      string savedJsonFileName = "lfsaIEYkR.json"; //Prepared externally 
      string outputGWAFileName = "TestGwaRecords.json";  //Output by this method

      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();

      //Read JSON files into objects
      var speckleObjects = Helper.ExtractObjects(savedJsonFileName);

      //Run conversion to GWA keywords
      foreach (var so in speckleObjects)
      {
        SpeckleCore.Converter.Deserialise(speckleObjects)
      }


      //Create JSON file containing pairs of ApplicationId and GWA commands

      //Save JSON file
      var obj = new List<GwaRecord>();
      obj.Add(new GwaRecord("testAppId01", "testGwaCommand01"));
      obj.Add(new GwaRecord("testAppId02", "testGwaCommand02"));
      var jsonToWrite = JsonConvert.SerializeObject(obj, Formatting.Indented);

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
