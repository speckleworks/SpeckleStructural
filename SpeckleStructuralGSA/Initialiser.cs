using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    public static IGSASettings Settings { get; set; }
    public static IGSAProxy Interface { get; set; }
    public static IGSACacheForKit Cache { get; set; }
    public static ISpeckleGSAAppUI AppUI { get; set; }
    //The variable below must be a property (i.e. with { get; }) and of Dictionary<Type, List<object>> type so that SpeckleGSA
    //can recognise this as a kit it can work with
    public static IGSASenderDictionary GSASenderObjects { get; } = new GSASenderDictionary();
    //These the list of types which can be processed in parallel during reception - so within any batch, a group of objects of any
    //of these types are able to be processed in parallel and still ensure the order they appear in GSA matches the order in the stream.
    //In most cases a type is added to this list if:
    //- in practice objects of the type are one of the most numerous in a typical stream
    //- indices can be managed as a COM client - for doesn't work for nodes since the NodeAt does it
    //- there is a simple 1:1 relationship between objects of that type and GSA records, enabling GSA record indices to be reserved first, 
    //  then processed in parallel
    public static List<Type> RxParallelisableTypes { get; } = new List<Type>
    { typeof(StructuralLoadCase), typeof(StructuralLoadCombo), typeof(StructuralAssembly),
      typeof(Structural1DElement), typeof(Structural2DElement), typeof(Structural2DElementMesh), typeof(Structural2DVoid),
      typeof(Structural1DProperty), typeof(Structural1DPropertyExplicit), typeof(Structural2DProperty), typeof(StructuralSpringProperty),
      typeof(StructuralMaterialConcrete), typeof(StructuralMaterialSteel)
    };
  }
}
