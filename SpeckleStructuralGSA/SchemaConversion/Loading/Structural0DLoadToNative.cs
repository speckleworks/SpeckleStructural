using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural0DLoadToNative
  {
    private static readonly LoadDirection[] loadDirSeq = new LoadDirection[] { LoadDirection.X, LoadDirection.Y, LoadDirection.Z, LoadDirection.XX, LoadDirection.YY, LoadDirection.ZZ };

    public static string ToNative(this Structural0DLoad speckleLoad)
    {
      if (!IsValidLoading(speckleLoad.Loading))
      {
        return "";
      }

      var keyword = GsaRecord.Keyword<Schema.Gsa0dLoad>();
      var nodeKeyword = GsaRecord.Keyword<GsaNode>();
      var loadCaseKeyword = GsaRecord.Keyword<GsaLoadCase>();

      var nodeIndices = Initialiser.Cache.LookupIndices(nodeKeyword, speckleLoad.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).OrderBy(i => i).ToList();
      var indexResult = Initialiser.Cache.LookupIndex(loadCaseKeyword, speckleLoad.LoadCaseRef);
      var loadCaseIndex = indexResult ?? Initialiser.Cache.ResolveIndex(loadCaseKeyword, speckleLoad.LoadCaseRef);

      var gwaList = new List<string>();
      var loadingDict = ExplodeLoading(speckleLoad.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var gsaLoad = new Gsa0dLoad()
        {
          ApplicationId = string.Join("_", speckleLoad.ApplicationId, k.ToString()),
          Name = speckleLoad.Name,
          LoadDirection = k,
          Value = loadingDict[k],
          GlobalAxis = true,
          NodeIndices = nodeIndices,
          LoadCaseIndex = loadCaseIndex
        };
        if (gsaLoad.Gwa(out var gwa))
        {
          gwaList.AddRange(gwa);
        }
      }

      return string.Join("\n", gwaList);
    }

    private static Dictionary<LoadDirection, double> ExplodeLoading(StructuralVectorSix loading)
    {
      var valueByDir = new Dictionary<LoadDirection, double>();

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
