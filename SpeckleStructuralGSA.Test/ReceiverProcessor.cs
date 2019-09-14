using System;
using System.Collections.Generic;
using System.Linq;
using Interop.Gsa_10_0;
using Moq;
using SpeckleCore;


namespace SpeckleStructuralGSA.Test
{
  //Copied from the Receiver class in SpeckleGSA - this will be refactored to simplify and avoid dependency
  public class ReceiverProcessor
  {
    private Dictionary<Type, List<Type>> TypePrerequisites = new Dictionary<Type, List<Type>>();
    private List<KeyValuePair<Type, List<Type>>> TypeCastPriority = new List<KeyValuePair<Type, List<Type>>>();

    private bool TargetDesignLayer = true;
    private bool TargetAnalysisLayer = false;

    private GSAInterfacer GSAInterfacer = Initialiser.GSA;
    private List<SpeckleObject> receivedObjects;

    //This should match the private member in GSAInterfacer
    private const string SID_TAG = "speckle_app_id";

    public bool JsonSpeckleStreamsToGwaRecords(IEnumerable<string> savedJsonFileNames, out List<GwaRecord> gwaRecords, bool designLayer = true, bool analysisLayer = false)
    {
      gwaRecords = new List<GwaRecord>();

      receivedObjects = JsonSpeckleStreamsToSpeckleObjects(savedJsonFileNames);

      TargetDesignLayer = designLayer;
      TargetAnalysisLayer = analysisLayer;

      SetupMockGsaCom();

      ConstructTypeCastPriority();

      GSAInterfacer.Indexer.SetBaseline();

      GSAInterfacer.PreReceiving();

      ScaleObjects();

      GSAInterfacer.Indexer.ResetToBaseline();

      ConvertSpeckleObjectsToGsaInterfacerCache();

      var gwaCommands = GSAInterfacer.GetSetCache();
      foreach (var gwaC in gwaCommands)
      {
        gwaRecords.Add(new GwaRecord(ExtractApplicationId(gwaC), gwaC));
      }

      return true;
    }

    #region private_methods
    private void SetupMockGsaCom()
    {
      var mockGsaCom = new Mock<IComAuto>();
      //So far only these methods are actually called
      mockGsaCom.Setup(x => x.Gen_NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns((double x, double y, double z, double coin) => 1);
      mockGsaCom.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });

      GSAInterfacer.InitializeReceiver(mockGsaCom.Object);
    }

    private List<SpeckleObject> JsonSpeckleStreamsToSpeckleObjects(IEnumerable<string> savedJsonFileNames)
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();

      //Read JSON files into objects
      return Helper.ExtractObjects(savedJsonFileNames.ToArray());
    }

    private void ConstructTypeCastPriority()
    {
      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);

      // Grab all GSA related object
      var ass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "SpeckleStructuralGSA");
      var objTypes = ass.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t != interfaceType).ToList();

      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if (TargetAnalysisLayer && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if (TargetDesignLayer && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        var prereq = new List<Type>();
        if (t.GetAttribute("WritePrerequisite", attributeType) != null)
          prereq = ((Type[])t.GetAttribute("WritePrerequisite", attributeType)).ToList();

        TypePrerequisites[t] = prereq;
      }

      // Remove wrong layer objects from prerequisites
      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if (TargetAnalysisLayer && !(bool)t.GetAttribute("AnalysisLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if (TargetDesignLayer && !(bool)t.GetAttribute("DesignLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);
      }

      // Generate which GSA object to cast for each type
      TypeCastPriority = TypePrerequisites.ToList();
      TypeCastPriority.Sort((x, y) => x.Value.Count().CompareTo(y.Value.Count()));
    }

    private void ScaleObjects()
    {
      //Status.ChangeStatus("Scaling objects");
      var scaleFactor = (1.0).ConvertUnit("mm", "m");
      foreach (SpeckleObject o in receivedObjects)
      {
        try
        {
          o.Scale(scaleFactor);
        }
        catch { }
      }
    }

    private void ConvertSpeckleObjectsToGsaInterfacerCache()
    {
      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);

          var valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var targetObjects = receivedObjects.Where(o => o.GetType() == valueType);
          Converter.Deserialise(targetObjects);

          receivedObjects.RemoveAll(x => targetObjects.Any(o => x == o));

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      // Write leftover
      Converter.Deserialise(receivedObjects);
    }

    private string ExtractApplicationId(string gwaCommand)
    {
      return gwaCommand.Split(new string[] { SID_TAG }, StringSplitOptions.None)[1].Substring(1).Split('}')[0];
    }
    #endregion
  }
}

