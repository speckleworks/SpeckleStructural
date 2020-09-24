using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    public static IGSASettings Settings { get; set; }
    public static IGSAProxy Interface { get; set; }
    public static IGSACacheForKit Cache { get; set; }
    public static ISpeckleGSAAppUI AppUI { get; set; }
    public static IGSAResultsContext ResultsContext { get; set; }
    //The variable below must be a property (i.e. with { get; }) and of Dictionary<Type, List<object>> type so that SpeckleGSA
    //can recognise this as a kit it can work with
    public static IGSASenderDictionary GSASenderObjects { get; } = new GSASenderDictionary();

    
  }
}
