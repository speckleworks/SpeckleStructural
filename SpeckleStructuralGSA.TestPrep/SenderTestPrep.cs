using System.Linq;
using Newtonsoft.Json;
using SpeckleCore;
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
      Initialiser.AppUI = new TestAppUI();
      gsaInterfacer.OpenFile(Test.Helper.ResolveFullPath(gsaFileName, TestDataDirectory));
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
      gsaInterfacer.Close();
    }
  }
}
