using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public abstract class ProcessorBase
  {
    protected Dictionary<Type, List<Type>> TypePrerequisites = new Dictionary<Type, List<Type>>();
    protected List<KeyValuePair<Type, List<Type>>> TypeCastPriority = new List<KeyValuePair<Type, List<Type>>>();
    protected string TestDataDirectory;

    protected GSAInterfacer GSAInterfacer;

    //This should match the private member in GSAInterfacer
    protected const string SID_TAG = "speckle_app_id";

    protected enum ioDirection
    {
      Unknown = 0,
      Receive = 1,
      Send = 2
    }

    protected ProcessorBase(string directory)
    {
      TestDataDirectory = directory;
    }

    protected void ConstructTypeCastPriority(ioDirection ioDirection, bool resultsOnly)
    {
      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);

      var ioAttribute = (ioDirection == ioDirection.Receive) ? "WritePrerequisite" : "ReadPrerequisite";

      // Grab all GSA related object
      var ass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "SpeckleStructuralGSA");
      var objTypes = ass.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t != interfaceType).ToList();

      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((Initialiser.Settings.TargetLayer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        if (ioDirection == ioDirection.Send)
        {
          if (t.GetAttribute("Stream", attributeType) != null)
            if (resultsOnly && t.GetAttribute("Stream", attributeType) as string != "results") continue;
        }

        var prereq = new List<Type>();
        if (t.GetAttribute(ioAttribute, attributeType) != null)
          prereq = ((Type[])t.GetAttribute(ioAttribute, attributeType)).ToList();

        TypePrerequisites[t] = prereq;
      }

      // Remove wrong layer objects from prerequisites
      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((Initialiser.Settings.TargetLayer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (ioDirection == ioDirection.Send)
        {
          if (t.GetAttribute("Stream", attributeType) != null)
            if (resultsOnly && t.GetAttribute("Stream", attributeType) as string != "results")
              foreach (KeyValuePair<Type, List<Type>> kvp in TypePrerequisites)
                kvp.Value.Remove(t);
        }
      }

      // Generate which GSA object to cast for each type
      TypeCastPriority = TypePrerequisites.ToList();
      TypeCastPriority.Sort((x, y) => x.Value.Count().CompareTo(y.Value.Count()));
    }
  }
}
