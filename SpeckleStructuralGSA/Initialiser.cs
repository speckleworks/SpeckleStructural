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
    public static Dictionary<Type, List<object>> GSASenderObjects { get; } = new Dictionary<Type, List<object>>();
    public static ISpeckleGSAAppUI AppUI { get; set; }
  }
}
