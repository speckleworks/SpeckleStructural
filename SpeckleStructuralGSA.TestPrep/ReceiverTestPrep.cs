using System.IO;
using Newtonsoft.Json;
using SpeckleGSAInterfaces;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class ReceiverTestPrep : TestBase
  {
    public ReceiverTestPrep(string directory) : base(directory) { }

    public void SetupContext()
    {
      Initialiser.AppResources = new MockGSAApp();
    }

    public bool SetUpReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer, string subdir)
    {
      return PrepareReceptionTestData(savedJsonFileNames, outputGWAFileName, layer, subdir);
    }

    private bool PrepareReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer, string subdir)
    {
      var mockGsaCom = SetupMockGsaCom();
      Initialiser.AppResources.Proxy.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(Path.Combine(TestDataDirectory, subdir), Initialiser.AppResources, layer);

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
