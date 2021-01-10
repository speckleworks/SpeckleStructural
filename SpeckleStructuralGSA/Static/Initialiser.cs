using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer, IGSAKit
  {
    public static Initialiser Instance { get; } = new Initialiser();

    public IGSASettings Settings { get; set; }
    public IGSAProxy Interface { get; set; }
    public IGSACacheForKit Cache { get; set; }
    public ISpeckleGSAAppUI AppUI { get; set; }
    //The variable below must be a property (i.e. with { get; }) and of Dictionary<Type, List<object>> type so that SpeckleGSA
    //can recognise this as a kit it can work with
    public IGSASenderDictionary GSASenderObjects { get; } = new GSASenderDictionary();
    //These the list of types which can be processed in parallel during reception - so within any batch, a group of objects of any
    //of these types are able to be processed in parallel and still ensure the order they appear in GSA matches the order in the stream.
    //In most cases a type is added to this list if:
    //- in practice objects of the type are one of the most numerous in a typical stream
    //- indices can be managed as a COM client - for doesn't work for nodes since the NodeAt does it
    //- there is a simple 1:1 relationship between objects of that type and GSA records, enabling GSA record indices to be reserved first, 
    //  then processed in parallel
    public List<Type> RxParallelisableTypes { get; } = new List<Type>
    { typeof(StructuralLoadCase), typeof(StructuralLoadCombo), typeof(StructuralAssembly),
      typeof(Structural1DElement), typeof(Structural2DElement), typeof(Structural2DElementMesh), typeof(Structural2DVoid),
      typeof(Structural1DProperty), typeof(Structural1DPropertyExplicit), typeof(Structural2DProperty), typeof(StructuralSpringProperty),
      typeof(StructuralMaterialConcrete), typeof(StructuralMaterialSteel)
    };

    //For now, it returns old GSA schema (i.e. those implementing the IGSASpeckleContainer and IGSAContainer interfaces.
    //When the new GSA schema is fully integrated, it will return SpeckleStructural types
    public Dictionary<Type, List<Type>> RxTypeDependencies()
    {
      var assembly = GetType().Assembly; //This assembly
      var assemblyTypes = assembly.GetTypes();
      var rxTypeDependencies = new Dictionary<Type, List<Type>>();

      var newGsaSchematypes = assemblyTypes.Where(t => t.IsSubclassOf(typeof(GsaRecord))
        && t.CustomAttributes.Any(ca => ca.AttributeType == typeof(GsaType))).ToList();

      var keywordDependencies = new Dictionary<GwaKeyword, List<GwaKeyword>>();

      //First get all keywords of the layer - this is needed because the referenced keywords in the GsaType attribute of GsaRecord
      //classes sometimes span both layers
      var newGsaLayerTypes = newGsaSchematypes.Where(t => GsaRecord.IsSelfContained(t) && MatchesLayer(t, Settings.TargetLayer)).ToList();
      var layerKeywords = newGsaLayerTypes.Select(t => GsaRecord.GetGwaKeyword(t)).ToList();

      //Build up dictionary of new GSA schema types and keywords - to be used to construct dependencies based on these new types
      var layerKwByType = newGsaLayerTypes.ToDictionary(t => t, t => GsaRecord.GetGwaKeyword(t));

      var kwDependencies = newGsaLayerTypes.ToDictionary(t => layerKwByType[t], 
        t => GsaRecord.GetReferencedKeywords(t).Where(kw => layerKeywords.Contains(kw)).ToList());

      var oldGsaInterface = typeof(IGSASpeckleContainer);
      var oldGsaTypes = assemblyTypes.Where(t => oldGsaInterface.IsAssignableFrom(t) && !t.IsAbstract).ToList();
      foreach (var oldT in oldGsaTypes)
      {
        var oldTkw = ((string)oldT.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();
        if (!kwDependencies.Keys.Any(k => k.GetStringValue().Equals(oldTkw, StringComparison.InvariantCultureIgnoreCase)))
        {
          continue;
        }

        if (!rxTypeDependencies.ContainsKey(oldT))
        {
          rxTypeDependencies.Add(oldT, new List<Type>());
        }

        var attVal = oldT.GetAttribute<GSAObject>("WritePrerequisite");
        var prereqs = (attVal != null) ? ((Type[])attVal).ToList() : new List<Type>();

        foreach (var tPrereq in prereqs)
        {
          //Remove version for comparison with keyword enum
          var kwPrereq = ((string)tPrereq.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();

          if (kwDependencies.Keys.Any(k => k.GetStringValue().Equals(kwPrereq, StringComparison.InvariantCultureIgnoreCase)))
          {
            rxTypeDependencies[oldT].Add(tPrereq);
          }
        }
      }

      return rxTypeDependencies;
    }

    private bool MatchesLayer(Type t, GSATargetLayer layer)
    {
      return ((Settings.TargetLayer == GSATargetLayer.Design && GsaRecord.IsDesignLayer(t))
            || (Settings.TargetLayer == GSATargetLayer.Analysis && GsaRecord.IsAnalysisLayer(t)));
    }

  }
}
