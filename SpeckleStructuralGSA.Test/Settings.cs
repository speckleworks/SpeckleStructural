using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  public class Settings : IGSASettings
  {
    public bool SendOnlyMeaningfulNodes = true;
    public bool SeparateStreams = false;
    public int PollingRate = 2000;

    //Default values for properties specified in the interface
    public string Units { get; set; }
    public GSATargetLayer TargetLayer { get; set; } = GSATargetLayer.Design;
    public double CoincidentNodeAllowance { get; set; } = 0.1;
    public bool SendOnlyResults { get; set; } = false;

    public bool SendResults = false;

    public Dictionary<string, Tuple<int, int, List<string>>> NodalResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();
    public Dictionary<string, Tuple<int, int, List<string>>> Element1DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();
    public Dictionary<string, Tuple<int, int, List<string>>> Element2DResults { get; set; } = new Dictionary<string, Tuple<int, int, List<string>>>();
    public Dictionary<string, Tuple<string, int, int, List<string>>> MiscResults { get; set; } = new Dictionary<string, Tuple<string, int, int, List<string>>>();

    public List<string> ResultCases { get; set; } = new List<string>();
    public bool ResultInLocalAxis { get; set; } = false;
    public int Result1DNumPosition { get; set; } = 3;
    public bool EmbedResults { get; set; } = true;
  }
}
