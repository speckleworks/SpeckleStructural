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
    public void Test1()
    {
      //

      var testFileNames = new[] { "lfsaIEYkR.json", "NaJD7d5kq.json", "U7ntEJkzdZ.json", "UNg87ieJG.json" };

      SpeckleInitializer.Initialize();
      var responses = Helper.DeserialiseTestData(testFileNames);

      var json = Helper.ReadFile(testFileNames.First());
      var jt = JToken.Parse(json);

      //Read pairs of application IDs and their GWA commands
      var readJson = Helper.ReadFile("TestGwaRecords.json");
      var readObj = JsonConvert.DeserializeObject<List<GwaRecord>>(readJson);

      //Test if two objects are the same
      var jAreEqual = JToken.DeepEquals(JToken.Parse(@"{""a"":1,""b"":2}"), JToken.Parse(@"{""b"":2,""a"":1}"));

    }
    #endregion

    #region Transmission_Tests
    public void Test2()
    {

    }
    #endregion

    
  }
}
