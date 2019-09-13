using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Gsa_10_0;
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

    private IComAuto mockGsaCom;
    private GSAInterfacer GSAInterfacer;

    //This should match the private member in GSAInterfacer
    private const string SID_TAG = "speckle_app_id";

    public bool SpeckleToGwa(List<SpeckleObject> receivedObjects, out List<Tuple<string, string>> gwaPerIds, bool designLayer = true, bool analysisLayer = false)
    {
      TargetDesignLayer = designLayer;
      TargetAnalysisLayer = analysisLayer;
      GSAInterfacer = Initialiser.GSA;
      IComAuto mockGsaCom = new MockGsaCom();
      gwaPerIds = new List<Tuple<string, string>>();

      // Run initialize receiver method in interfacer
      var assemblies = SpeckleInitializer.GetAssemblies();

      GSAInterfacer.InitializeReceiver(mockGsaCom);

      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);
      
      // Grab all GSA related object
      var objTypes = new List<Type>();
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
          if (interfaceType.IsAssignableFrom(type) && type != interfaceType)
            objTypes.Add(type);
      }

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
      
      GSAInterfacer.Indexer.SetBaseline();
   
      // Run pre receiving method and inject!!!!
      GSAInterfacer.PreReceiving();

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

      GSAInterfacer.Indexer.ResetToBaseline();

      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          //Status.ChangeStatus("Writing " + t.Name);

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

      GSAInterfacer.PostReceiving();

      var gwaCommands = GSAInterfacer.GetSetCache();
      foreach (var gwaC in gwaCommands)
      {
        gwaPerIds.Add(new Tuple<string, string>(ExtractApplicationId(gwaC), gwaC));
      }

      return true;
    }

    private string ExtractApplicationId(string gwaCommand)
    {
      return gwaCommand.Split(new string[] { SID_TAG }, StringSplitOptions.None)[1].Substring(1).Split('}')[0];
    }
  }
}

