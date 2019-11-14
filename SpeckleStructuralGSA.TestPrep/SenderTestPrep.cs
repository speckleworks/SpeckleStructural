using Newtonsoft.Json;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class SenderTestPrep : TestBase
  {
    public SenderTestPrep(string directory) : base(directory) { }

    public void SetupContext(string gsaFileName)
    {
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();
      Initialiser.Interface = gsaInterfacer;
      Initialiser.Cache = gsaCache;
      Initialiser.Settings = new Settings();
      gsaInterfacer.OpenFile(Helper.ResolveFullPath(gsaFileName, TestDataDirectory));
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
      var jsonToWrite = JsonConvert.SerializeObject(speckleObjects, Formatting.Indented);

      Helper.WriteFile(jsonToWrite, outputJsonFileName, TestDataDirectory);

      return true;
    }

    public void TearDownContext()
    {
      gsaInterfacer.Close();
    }
  }
}
