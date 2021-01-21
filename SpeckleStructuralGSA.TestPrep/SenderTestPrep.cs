using System.IO;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class SenderTestPrep : TestBase
  {
    public SenderTestPrep(string directory) : base(directory) { }

    public void SetupContext(string gsaFileName)
    {
      Initialiser.AppResources = new MockGSAApp();
      Initialiser.AppResources.Proxy.OpenFile(Path.Combine(TestDataDirectory, gsaFileName));
    }

    public bool SetUpTransmissionTestData(string outputJsonFileName, GSATargetLayer layer,
      bool resultsOnly, bool embedResults, string[] cases = null, string[] resultsToSend = null)
    {
      var speckleObjects = ModelToSpeckleObjects(layer, resultsOnly, embedResults, cases, resultsToSend);
      if (speckleObjects == null)
      {
        return false;
      }

      //Create JSON file containing serialised SpeckleObjects
      speckleObjects.Sort((a, b) => CompareForOutputFileOrdering(a, b));
      var jsonToWrite = JsonConvert.SerializeObject(speckleObjects, Formatting.Indented);

      Test.Helper.WriteFile(jsonToWrite, outputJsonFileName, TestDataDirectory);

      return true;
    }

    private int CompareForOutputFileOrdering(SpeckleObject a, SpeckleObject b)
    {
      var typeCompare = string.Compare(a.Type, b.Type);
      if (typeCompare == 0)
      {
        return string.Compare(a.ApplicationId, b.ApplicationId);
      }
      else
      {
        return typeCompare;
      }
    }

    public void TearDownContext()
    {
      Initialiser.AppResources.Proxy.Close();
    }
  }
}
