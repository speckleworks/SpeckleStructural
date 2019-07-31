using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using Interop.Gsa_10_0;
using SQLite;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    public Initialiser() { }

    public static GSAInterfacer GSA { get; set; } = new GSAInterfacer();

    public static Dictionary<Type, List<object>> GSASenderObjects { get; set; } = new Dictionary<Type, List<object>>();

    public static string GSAUnits { get; set; }

    public static double GSACoincidentNodeAllowance { get; set; }

    public static GSATargetLayer GSATargetLayer { get; set; }

    public static bool GSATargetDesignLayer { set => GSATargetLayer = value ? GSATargetLayer.Design : GSATargetLayer.Analysis; }

    public static bool GSATargetAnalysisLayer { set => GSATargetLayer = value ? GSATargetLayer.Analysis : GSATargetLayer.Design; }

    public static Dictionary<string, Tuple<int, int, List<string>>> GSANodalResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();
  
    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement1DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();

    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement2DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();

    public static Dictionary<string, Tuple<string, int, int, List<string>>> GSAMiscResults { get; set; } = new Dictionary<string, Tuple<string, int, int, List<string>>>();

    public static List<string> GSAResultCases { get; set; } = new List<string>();

    public static bool GSAResultInLocalAxis { get; set; }

    public static int GSAResult1DNumPosition { get; set; }

    public static bool GSAEmbedResults { get; set; }
  }

  public static partial class Conversions
  {
    public static Dictionary<Type, List<object>> GSASenderObjects { get => Initialiser.GSASenderObjects; }

    public static GSAInterfacer GSA { get => Initialiser.GSA; }

    public static string GSAUnits { get => Initialiser.GSAUnits; }

    public static double GSACoincidentNodeAllowance { get => Initialiser.GSACoincidentNodeAllowance; }

    public static GSATargetLayer GSATargetLayer { get => Initialiser.GSATargetLayer; }
    
    public static Dictionary<string, Tuple<int, int, List<string>>> GSANodalResults { get => Initialiser.GSANodalResults; }

    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement1DResults { get => Initialiser.GSAElement1DResults; }

    public static Dictionary<string, Tuple<int, int, List<string>>> GSAElement2DResults { get => Initialiser.GSAElement2DResults; }

    public static Dictionary<string, Tuple<string, int, int, List<string>>> GSAMiscResults { get => Initialiser.GSAMiscResults; }

    public static List<string> GSAResultCases { get => Initialiser.GSAResultCases; }

    public static bool GSAResultInLocalAxis { get => Initialiser.GSAResultInLocalAxis; }

    public static int GSAResult1DNumPosition { get => Initialiser.GSAResult1DNumPosition; }

    public static bool GSAEmbedResults { get => Initialiser.GSAEmbedResults; }
  }
}
