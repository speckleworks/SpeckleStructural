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

    protected GSAProxy GSAInterfacer;
    protected GSACache GSACache;

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
              foreach (var kvp in TypePrerequisites)
                kvp.Value.Remove(t);
        }
      }

      // Generate which GSA object to cast for each type
      TypeCastPriority = TypePrerequisites.ToList();
      TypeCastPriority.Sort((x, y) => x.Value.Count().CompareTo(y.Value.Count()));
    }

    protected void ProcessDeserialiseReturnObject(object deserialiseReturnObject, out string keyword, out int index, out string gwa, out GwaSetCommandType gwaSetCommandType)
    {
      index = 0;
      keyword = "";
      gwa = "";
      gwaSetCommandType = GwaSetCommandType.Set;

      if (!(deserialiseReturnObject is string))
      {
        return;
      }

      var fullGwa = (string)deserialiseReturnObject;

      var pieces = fullGwa.ListSplit("\t").ToList();
      if (pieces.Count() < 2)
      {
        return;
      }

      if (pieces[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
      {
        gwaSetCommandType = GwaSetCommandType.SetAt;
        pieces.Remove(pieces[0]);
      }
      else if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        gwaSetCommandType = GwaSetCommandType.Set;
        pieces.Remove(pieces[0]);
      }

      gwa = string.Join("\t", pieces);
      gwa.ExtractKeywordApplicationId(out keyword, out int? foundIndex, out string applicationId, out string gwaWithoutSet);
      int.TryParse(pieces[1], out index);

      return;
    }
  }
}
