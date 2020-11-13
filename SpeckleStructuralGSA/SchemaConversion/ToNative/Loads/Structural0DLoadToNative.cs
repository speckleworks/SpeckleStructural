using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural0DLoadToNative
  {
    private static readonly LoadDirection6[] loadDirSeq = new LoadDirection6[] { LoadDirection6.X, LoadDirection6.Y, LoadDirection6.Z, LoadDirection6.XX, LoadDirection6.YY, LoadDirection6.ZZ };

    public static string ToNative(this Structural0DLoad load)
    {
      if (string.IsNullOrEmpty(load.ApplicationId) && !IsValidLoading(load.Loading))
      {
        return "";
      }

      var keyword = GsaRecord.Keyword<GsaLoadNode>();
      var nodeKeyword = GsaRecord.Keyword<GsaNode>();
      var loadCaseKeyword = GsaRecord.Keyword<GsaLoadCase>();

      var nodeIndices = Initialiser.Cache.LookupIndices(nodeKeyword, load.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).OrderBy(i => i).ToList();
      var loadCaseIndex = Initialiser.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

      var gwaList = new List<string>();
      var loadingDict = ExplodeLoading(load.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", load.ApplicationId, k.ToString());
        var index = Initialiser.Cache.ResolveIndex(keyword, applicationId);
        var gsaLoad = new GsaLoadNode()
        {
          Index = index,
          ApplicationId = applicationId,
          Name = load.Name,
          LoadDirection = k,
          Value = loadingDict[k],
          GlobalAxis = true,
          NodeIndices = nodeIndices,
          LoadCaseIndex = loadCaseIndex
        };
        if (gsaLoad.Gwa(out var gwa, true))
        {
          gwaList.AddRange(gwa);
        }
      }

      return string.Join("\n", gwaList);
    }

    private static Dictionary<LoadDirection6, double> ExplodeLoading(StructuralVectorSix loading)
    {
      var valueByDir = new Dictionary<LoadDirection6, double>();

      for (var i = 0; i < loadDirSeq.Count(); i++)
      {
        if (loading.Value[i] != 0)
        {
          valueByDir.Add(loadDirSeq[i], loading.Value[i]);
        }
      }

      return valueByDir;
    }

    private static bool IsValidLoading(StructuralVectorSix loading)
    {
      return (loading != null && loading.Value != null && loading.Value.Count() == loadDirSeq.Count() && loading.Value.Any(v => v != 0));
    }
  }
}
