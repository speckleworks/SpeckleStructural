using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    public static IGSASettings Settings { get; set; }
    public static IGSAInterfacer Interface { get; set; }

    public Initialiser() { }

    //public static Initialiser.Interfacer GSA { get; set; } = new Initialiser.Interfacer();

    public static Dictionary<Type, List<object>> GSASenderObjects { get; set; } = new Dictionary<Type, List<object>>();

    public static Dictionary<string, Tuple<int, int, List<string>>> GSANodalResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();
  
    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement1DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();

    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement2DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();

    public static Dictionary<string, Tuple<string, int, int, List<string>>> GSAMiscResults { get; set; } = new Dictionary<string, Tuple<string, int, int, List<string>>>();

    public static List<string> GSAResultCases { get; set; } = new List<string>();

    public static bool GSAResultInLocalAxis { get; set; }

    public static int GSAResult1DNumPosition { get; set; }

    public static bool GSAEmbedResults { get; set; }
  }
}
