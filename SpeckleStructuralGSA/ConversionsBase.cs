using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Gsa_10_0;
using SQLite;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    public static Dictionary<Type, List<object>> GSASenderObjects { get => Initialiser.GSASenderObjects; }

    public static IGSAInterfacer GSA { get => Initialiser.Interface; }

    public static string GSAUnits { get => Initialiser.Settings.Units; }

    public static double GSACoincidentNodeAllowance { get => Initialiser.Settings.CoincidentNodeAllowance; }

    public static GSATargetLayer GSATargetLayer { get => (Initialiser.Settings.TargetDesignLayer) ? GSATargetLayer.Design : GSATargetLayer.Analysis ; }
    
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
