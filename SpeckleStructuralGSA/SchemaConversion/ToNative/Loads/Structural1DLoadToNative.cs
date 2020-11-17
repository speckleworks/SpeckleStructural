using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural1DLoadToNative
  {
    public static string ToNative(this Structural1DLoad load)
    {
      if (string.IsNullOrEmpty(load.ApplicationId) && !Helper.IsValidLoading(load.Loading))
      {
        return "";
      }

      //Note: only LOAD_BEAM_UDL is supported at this stage
      var keyword = GsaRecord.Keyword<GsaLoadNode>();

      var loadCaseIndex = Initialiser.Cache.LookupIndex(GsaRecord.Keyword<GsaLoadCase>(), load.LoadCaseRef);

      var gwaList = new List<string>();
      var loadingDict = Helper.ExplodeLoading(load.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", load.ApplicationId, k.ToString());
        var index = Initialiser.Cache.ResolveIndex(keyword, applicationId);
        var gsaLoad = new GsaLoadBeamUdl()
        {
          Index = index,
          ApplicationId = applicationId,
          Name = load.Name,
          LoadDirection = k,
          Load = loadingDict[k],
          Projected = false,
          AxisRefType = LoadBeamAxisRefType.Global,
          LoadCaseIndex = loadCaseIndex
        };
        if (gsaLoad.Gwa(out var gwa, true))
        {
          gwaList.AddRange(gwa);
        }
      }

      return string.Join("\n", gwaList);
    }
  }
}
