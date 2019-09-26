using Newtonsoft.Json;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class ReceiverTestPrep : TestBase
  {
    public ReceiverTestPrep(string directory) : base(directory) { }

    public bool SetUpReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer)
    {
      //Set up default values
      Initialiser.GSACoincidentNodeAllowance = 0.1;
      Initialiser.GSAResult1DNumPosition = 3;

      return PrepareReceptionTestData(savedJsonFileNames, outputGWAFileName, layer);
    }

    private bool PrepareReceptionTestData(string[] savedJsonFileNames, string outputGWAFileName, GSATargetLayer layer)
    {
      var mockGsaCom = SetupMockGsaCom();
      Initialiser.GSA.InitializeReceiver(mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, Initialiser.GSA, layer);

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
