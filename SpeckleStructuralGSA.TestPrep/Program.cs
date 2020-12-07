using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralGSA.Test;

namespace SpeckleStructuralGSA.TestPrep
{
  public class Program
  {
    private static bool blankRefs = true;
    private static bool rxDesign = true;
    private static bool txDesignBeforeAnalysis = true;
    private static bool txDesign = true;
    private static bool txResultsOnly = true;
    private static bool txEmbedded = true;
    private static bool txNotEmbedded = true;

    static void Main(string[] args)
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution

      //If this isn't called, then the GetObjectSubtypeBetter method in SpeckleCore will cause a {"Value cannot be null.\r\nParameter name: source"} message
      SpeckleInitializer.Initialize();

      var TestDataDirectory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\SpeckleStructuralGSA.Test\TestData\";

      ReceiverTestPrep receiverTestPrep;
      if (blankRefs)
      {
        receiverTestPrep = new ReceiverTestPrep(TestDataDirectory);
        receiverTestPrep.SetupContext();
        if (!receiverTestPrep.SetUpReceptionTestData(TestBase.savedBlankRefsJsonFileNames, TestBase.expectedBlankRefsGwaPerIdsFileName, GSATargetLayer.Design, "Blank"))
        {
          Console.WriteLine("Error in preparing test data for the blank refs rx design layer test");
        }
        else
        {
          Console.WriteLine("Prepared reception test data for the blank refs rx design layer test");
        }
        //Don't print any error related to blank references - they're expected
        PrintAnyErrorMessages((TestAppUI)Initialiser.AppUI, new List<string> { "blank" });
      }

      if (rxDesign)
      {
        receiverTestPrep = new ReceiverTestPrep(TestDataDirectory);
        receiverTestPrep.SetupContext();
        if (!receiverTestPrep.SetUpReceptionTestData(TestBase.savedJsonFileNames, TestBase.expectedGwaPerIdsFileName, GSATargetLayer.Design, "NB"))
        {
          Console.WriteLine("Error in preparing test data for the rx design layer test");
        }
        else
        {
          Console.WriteLine("Prepared reception test data for the rx design layer test");
        }
        PrintAnyErrorMessages((TestAppUI)Initialiser.AppUI);
      }

      var senderTestPrep = new SenderTestPrep(TestDataDirectory);

      if (txDesignBeforeAnalysis)
      {
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
        PrintAnyErrorMessages((TestAppUI)Initialiser.AppUI);
      }

      //Next the sender tests using a file with results already generated
      senderTestPrep.SetupContext(SenderTests.gsaFileNameWithResults);

      try
      {
        if (txDesign)
        {
          if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsDesignLayer.json", GSATargetLayer.Design, false, true))
          {
            throw new Exception("Transmission: design layer test preparation failed");
          }
          Console.WriteLine("Prepared test data for the tx design layer test");
        }
        if (txResultsOnly)
        {
          if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsResultsOnly.json", GSATargetLayer.Analysis, true, false, SenderTests.loadCases, SenderTests.resultTypes))
          {
            throw new Exception("Transmission: results-only test preparation failed");
          }
          Console.WriteLine("Prepared test data for the tx results-only test");
        }
        if (txEmbedded)
        {
          if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsEmbedded.json", GSATargetLayer.Analysis, false, true, SenderTests.loadCases, SenderTests.resultTypes))
          {
            throw new Exception("Transmission: embedded test preparation failed");
          }
          Console.WriteLine("Prepared test data for the tx embedded results test");
        }
        if (txNotEmbedded)
        {
          if (!senderTestPrep.SetUpTransmissionTestData("TxSpeckleObjectsNotEmbedded.json", GSATargetLayer.Analysis, false, false, SenderTests.loadCases, SenderTests.resultTypes))
          {
            throw new Exception("Transmission: not-embedded test preparation failed");
          }
          Console.WriteLine("Prepared test data for the tx non-embedded results test");
        }
      }
      catch(Exception e)
      {
        Console.WriteLine(e.Message);
      }
      finally
      {
        senderTestPrep.TearDownContext();
      }
      PrintAnyErrorMessages((TestAppUI)Initialiser.AppUI);

      Console.WriteLine("Press any key to exit ...");
      Console.ReadKey();
    }

    private static void PrintAnyErrorMessages(TestAppUI testAppUI, List<string> excludeWords = null)
    {
      if (testAppUI.Messages.Keys.Count > 0)
      {
        foreach (var k in testAppUI.Messages.Keys)
        {
          if (excludeWords == null || !excludeWords.Any(ew => k.ToLower().Contains(ew.ToLower())))
          {
            foreach (var d in testAppUI.Messages[k])
            {
              Console.WriteLine("Error: " + k + " - " + d);
            }
          }
        }
        testAppUI.Messages.Clear();
      }
    }
  }
}
