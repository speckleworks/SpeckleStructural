using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public abstract class ProcessorBase
  {
    protected string TestDataDirectory;

    //protected GSAProxy GSAInterfacer;
    //protected GSACache GSACache;
    protected IGSAAppResources appResources;

    //This should match the private member in GSAInterfacer
    protected const string SID_APPID_TAG = "speckle_app_id";

    protected ProcessorBase(string directory)
    {
      TestDataDirectory = directory;
      appResources = new MockGSAApp();
    }

    public List<string> GetKeywords(GSATargetLayer layer)
    {
      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);

      // Grab all GSA related object
      var ass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "SpeckleStructuralGSA");
      var objTypes = ass.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t != interfaceType && !t.IsAbstract).ToList();

      var TypePrerequisites = new Dictionary<Type, List<Type>>();

      var keywords = new List<string>();

      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        var typeKeyword = t.GetGSAKeyword();
        if (!keywords.Contains(typeKeyword))
        {
          keywords.Add(typeKeyword);
        }
        var subtypeKeywords = t.GetSubGSAKeyword();
        if (subtypeKeywords != null && subtypeKeywords.Count() > 0)
        {
          for (var i = 0; i < subtypeKeywords.Count(); i++)
          {
            if (!keywords.Contains(subtypeKeywords[i]))
            {
              keywords.Add(subtypeKeywords[i]);
            }
          }
        }
      }

      return keywords;
    }
  }
}
