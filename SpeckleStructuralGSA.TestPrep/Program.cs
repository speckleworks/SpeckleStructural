using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  class Program
  {
    static void Main(string[] args)
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution

      //If this isn't called, then the GetObjectSubtypeBetter method in SpeckleCore will cause a {"Value cannot be null.\r\nParameter name: source"} message
      SpeckleInitializer.Initialize();

      var TestDataDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\SpeckleStructuralGSA.Test\TestData\";

      var receiverTestPrep = new ReceiverTestPrep(TestDataDirectory);
      receiverTestPrep.SetupContext();
      if (!receiverTestPrep.SetUpReceptionTestData(ReceiverTests.savedJsonFileNames, ReceiverTests.expectedGwaPerIdsFileName, GSATargetLayer.Design))
      {
        Console.WriteLine("Error in preparing test data for the rx design layer test");
      }
      else
      {
        Console.WriteLine("Prepared reception test data for the rx design layer test");
      }

      var senderTestPrep = new SenderTestPrep(TestDataDirectory);

      //First the sender test for design layer data without any results being in the file
      senderTestPrep.SetupContext(SenderTests.gsaFileNameWithoutResults);
      try
      {
        if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsDesignLayerBeforeAnalysis.json", GSATargetLayer.Design, false, true))
        {
          throw new Exception("Transmission: design layer test preparation failed");
        }
        Console.WriteLine("Prepared test data for the tx design layer before analysis test");
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
      }
      finally
      {
        senderTestPrep.TearDownContext();
      }

      //Next the sender tests using a file with results already generated
      senderTestPrep.SetupContext(SenderTests.gsaFileNameWithResults);

      try
      {
        if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true))
        {
          throw new Exception("Transmission: design layer test preparation failed");
        }
        Console.WriteLine("Prepared test data for the tx design layer test");
        if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false, SenderTests.loadCases, SenderTests.resultTypes))
        {
          throw new Exception("Transmission: results-only test preparation failed");
        }
        Console.WriteLine("Prepared test data for the tx results-only test");
        if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true, SenderTests.loadCases, SenderTests.resultTypes))
        {
          throw new Exception("Transmission: embedded test preparation failed");
        }
        Console.WriteLine("Prepared test data for the tx embedded results test");
        if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false, SenderTests.loadCases, SenderTests.resultTypes))
        {
          throw new Exception("Transmission: not-embedded test preparation failed");
        }
        Console.WriteLine("Prepared test data for the tx non-embedded results test");
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
      }
      finally
      {
        senderTestPrep.TearDownContext();
      }

      Console.WriteLine("Press any key to exit ...");
      Console.ReadKey();
    }
  }
}
