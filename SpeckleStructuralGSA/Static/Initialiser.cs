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
    //This is a dictionary of types (and their keywords for advance GSA record index reservation) which can be processed in parallel 
    //ensure the order they appear in GSA matches the order in the stream.
    //In most cases a type is added to this list if:
    //- in practice objects of the type are one of the most numerous in a typical stream
    //- indices can be managed as a COM client - for doesn't work for nodes since the NodeAt does it
    //- there is a simple 1:1 relationship between objects of that type and GSA records, enabling GSA record indices to be reserved first, 
    //  then processed in parallel
    public Dictionary<Type, string> RxParallelisableTypes
    {
      get
      {
        var elementKw = Settings.TargetLayer == GSATargetLayer.Design ? GwaKeyword.MEMB : GwaKeyword.EL;
        return new Dictionary<Type, string>()
        { { typeof(StructuralLoadCase), GwaKeyword.LOAD_TITLE.GetStringValue() },
          { typeof(StructuralLoadCombo), GwaKeyword.COMBINATION.GetStringValue() },
          { typeof(StructuralAssembly), GwaKeyword.ASSEMBLY.GetStringValue() },
          { typeof(Structural1DElement), elementKw.GetStringValue() },
          { typeof(Structural2DElement), elementKw.GetStringValue() },
          { typeof(Structural2DElementMesh), elementKw.GetStringValue() },
          { typeof(Structural2DVoid), elementKw.GetStringValue() },
          { typeof(Structural1DProperty), GwaKeyword.SECTION.GetStringValue() },
          { typeof(Structural1DPropertyExplicit), GwaKeyword.SECTION.GetStringValue() },
          { typeof(Structural2DProperty), GwaKeyword.PROP_2D.GetStringValue() },
          { typeof(StructuralSpringProperty), GwaKeyword.PROP_SPR.GetStringValue() },
          { typeof(StructuralMaterialConcrete), GwaKeyword.MAT_CONCRETE.GetStringValue() },
          { typeof(StructuralMaterialSteel), GwaKeyword.MAT_STEEL.GetStringValue() }
        };
      }
    }

    private readonly List<Type> schemaTypes = new List<Type>();  //ALL schema types, from both layers
    private readonly List<Type> oldSchemaTypes = new List<Type>();  //ALL old schema types, from both layers
    private readonly Dictionary<GSATargetLayer, Dictionary<Type, GwaKeyword>> layerKeywordTypes 
      = new Dictionary<GSATargetLayer, Dictionary<Type, GwaKeyword>>();
    private readonly List<TypeDependencyData> typeDepData = new List<TypeDependencyData>();

    public List<string> Keywords => layerKeywordTypes[Settings.TargetLayer].Values.Select(kw => kw.GetStringValue()).ToList();

    public Initialiser()
    {
      var assembly = GetType().Assembly; //This assembly
      var assemblyTypes = assembly.GetTypes();

      var gsaBaseType = typeof(GsaRecord);
      var gsaAttributeType = typeof(GsaType);
      schemaTypes.AddRange(assemblyTypes.Where(t => t.IsSubclassOf(gsaBaseType) && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)));

      var oldGsaInterface = typeof(IGSASpeckleContainer);
      oldSchemaTypes.AddRange(assemblyTypes.Where(t => oldGsaInterface.IsAssignableFrom(t) && !t.IsAbstract));

      //First get all keywords of the layer - this is needed because the referenced keywords in the GsaType attribute of GsaRecord
      //classes sometimes span both layers
      layerKeywordTypes.Add(GSATargetLayer.Design, new Dictionary<Type, GwaKeyword>());
      layerKeywordTypes.Add(GSATargetLayer.Analysis, new Dictionary<Type, GwaKeyword>());

      foreach (var t in schemaTypes.Where(t => GsaRecord.IsSelfContained(t)))
      {
        if (MatchesLayer(t, GSATargetLayer.Design))
        {
          layerKeywordTypes[GSATargetLayer.Design].Add(t, GsaRecord.GetGwaKeyword(t));
        }
        if (MatchesLayer(t, GSATargetLayer.Analysis))
        {
          layerKeywordTypes[GSATargetLayer.Analysis].Add(t, GsaRecord.GetGwaKeyword(t));
        }
      }
    }

    //For now, it returns old GSA schema (i.e. those implementing the IGSASpeckleContainer and IGSAContainer interfaces.
    //When the new GSA schema is fully integrated, it will return SpeckleStructural types
    public Dictionary<Type, List<Type>> RxTypeDependencies
    {
      get
      {
        if (!typeDepData.Any(td => td.Direction == StreamDirection.Receive && td.Layer == Settings.TargetLayer))
        {
          typeDepData.Add(new TypeDependencyData(StreamDirection.Receive, Settings.TargetLayer, TypeDependencies(StreamDirection.Receive)));
        }
        return typeDepData.FirstOrDefault(td => td.Direction == StreamDirection.Receive && td.Layer == Settings.TargetLayer).Dependencies;
      }
    }

    public Dictionary<Type, List<Type>> TxTypeDependencies
    {
      get
      {
        if (!typeDepData.Any(td => td.Direction == StreamDirection.Send && td.Layer == Settings.TargetLayer))
        {
          typeDepData.Add(new TypeDependencyData(StreamDirection.Send, Settings.TargetLayer, TypeDependencies(StreamDirection.Send)));
        }
        return typeDepData.FirstOrDefault(td => td.Direction == StreamDirection.Send && td.Layer == Settings.TargetLayer).Dependencies;
      }
    }

    private Dictionary<Type, List<Type>> TypeDependencies(StreamDirection direction)
    {
      var typeDependencies = new Dictionary<Type, List<Type>>();

      //Build up dictionary of new GSA schema types and keywords - to be used to construct dependencies based on these new types
      var layerSchemaDict = layerKeywordTypes[Settings.TargetLayer];
      var layerSchemaTypes = layerSchemaDict.Keys;
      var layerSchemaKeywords = layerSchemaDict.Values;
      var kwDependencies = layerSchemaTypes.ToDictionary(t => layerSchemaDict[t], t => GsaRecord.GetReferencedKeywords(t).Where(kw => layerSchemaKeywords.Contains(kw)).ToList());

      foreach (var oldT in oldSchemaTypes)
      {
        var oldTKeyword = ((string)oldT.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();
        if (!kwDependencies.Keys.Any(k => k.GetStringValue().Equals(oldTKeyword, StringComparison.InvariantCultureIgnoreCase)))
        {
          continue;
        }

        if (!typeDependencies.ContainsKey(oldT))
        {
          typeDependencies.Add(oldT, new List<Type>());
        }

        var attVal = oldT.GetAttribute<GSAObject>(((direction == StreamDirection.Receive) ? "Write" : "Read") + "Prerequisite");
        var prereqs = (attVal != null) ? ((Type[])attVal).ToList() : new List<Type>();

        foreach (var tPrereq in prereqs)
        {
          //Remove version for comparison with keyword enum
          var kwPrereq = ((string)tPrereq.GetAttribute<GSAObject>("GSAKeyword")).Split('.').First();

          if (kwDependencies.Keys.Any(k => k.GetStringValue().Equals(kwPrereq, StringComparison.InvariantCultureIgnoreCase)))
          {
            typeDependencies[oldT].Add(tPrereq);
          }
        }
      }

      return typeDependencies;
    }

    private bool MatchesLayer(Type t, GSATargetLayer layer)
    {
      return ((layer == GSATargetLayer.Design && GsaRecord.IsDesignLayer(t))
            || (layer == GSATargetLayer.Analysis && GsaRecord.IsAnalysisLayer(t)));
    }
  }

  internal class TypeDependencyData
  {
    public StreamDirection Direction;
    public GSATargetLayer Layer;
    public Dictionary<Type, List<Type>> Dependencies;

    internal TypeDependencyData(StreamDirection dir, GSATargetLayer layer, Dictionary<Type, List<Type>> deps)
    {
      this.Direction = dir;
      this.Layer = layer;
      this.Dependencies = deps;
    }

    internal TypeDependencyData(StreamDirection dir, GSATargetLayer layer)
    {
      this.Direction = dir;
      this.Layer = layer;
      this.Dependencies = new Dictionary<Type, List<Type>>();
    }

    internal void Add(Type t, List<Type> prereqs)
    {
      if (!Dependencies.ContainsKey(t))
      {
        Dependencies.Add(t, new List<Type>());
      }
      foreach (var pr in prereqs)
      {
        if (!Dependencies[t].Contains(pr))
        {
          Dependencies[t].Add(pr);
        }
      }
    }
  }
}
