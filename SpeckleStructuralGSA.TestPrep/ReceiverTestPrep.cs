using System.IO;
using Newtonsoft.Json;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class ReceiverTestPrep : TestBase
  {
    public ReceiverTestPrep(string directory) : base(directory) { }

    public void SetupContext()
    {
      gsaInterfacer = new GSAProxy();
      gsaCache = new GSACache();

      Initialiser.Instance.Cache = gsaCache;
      Initialiser.Instance.Interface = gsaInterfacer;
      Initialiser.Instance.Settings = new MockSettings();
      Initialiser.Instance.AppUI = new TestAppUI();
    }

    public bool SetUpReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer, string subdir)
    {
      return PrepareReceptionTestData(savedJsonFileNames, outputGWAFileName, layer, subdir);
    }

    private bool PrepareReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer, string subdir)
    {
      var mockGsaCom = SetupMockGsaCom();
      gsaInterfacer.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(Path.Combine(TestDataDirectory, subdir), gsaInterfacer, gsaCache, layer);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var gwaRecords, layer);

      //Create JSON file containing pairs of ApplicationId and GWA commands
      var jsonToWrite = JsonConvert.SerializeObject(gwaRecords, Formatting.Indented, jsonSettings);

      //Save JSON file
      Test.Helper.WriteFile(jsonToWrite, outputGWAFileName, Path.Combine(TestDataDirectory, subdir));

      return true;
    }
  }
}
