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

      Initialiser.Indexer = gsaCache;
      Initialiser.Interface = gsaInterfacer;
      Initialiser.Settings = new Settings();
    }

    public bool SetUpReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer)
    {
      return PrepareReceptionTestData(savedJsonFileNames, outputGWAFileName, layer);
    }

    private bool PrepareReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer)
    {
      var mockGsaCom = SetupMockGsaCom();
      gsaInterfacer.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, gsaInterfacer, gsaCache, layer);

      //Run conversion to GWA keywords
      receiverProcessor.JsonSpeckleStreamsToGwaRecords(savedJsonFileNames, out var gwaRecords);

      //Create JSON file containing pairs of ApplicationId and GWA commands
      var jsonToWrite = JsonConvert.SerializeObject(gwaRecords, Formatting.Indented, jsonSettings);

      //Save JSON file
      Helper.WriteFile(jsonToWrite, outputGWAFileName, TestDataDirectory);

      return true;
    }
  }
}
