using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural0DLoadToNative
  {
    public static string ToNative(this Structural0DLoad load)
    {
      if (string.IsNullOrEmpty(load.ApplicationId) && !Helper.IsValidLoading(load.Loading))
      {
        return "";
      }

      var keyword = GsaRecord.GetKeyword<GsaLoadNode>();
      var nodeKeyword = GsaRecord.GetKeyword<GsaNode>();
      var loadCaseKeyword = GsaRecord.GetKeyword<GsaLoadCase>();

      var nodeIndices = Initialiser.AppResources.Cache.LookupIndices(nodeKeyword, load.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).OrderBy(i => i).ToList();
      var loadCaseIndex = Initialiser.AppResources.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);
      var streamId = Initialiser.AppResources.Cache.LookupStream(load.ApplicationId);
      var gwaSetCommandType = GsaRecord.GetGwaSetCommandType<GsaLoadNode>();

      var gwaList = new List<string>();
      var loadingDict = Helper.ExplodeLoading(load.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", load.ApplicationId, k.ToString());
        var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, applicationId);
        var gsaLoad = new GsaLoadNode()
        {
          Index = index,
          ApplicationId = applicationId,
          StreamId = streamId,
          Name = load.Name,
          LoadDirection = k,
          Value = loadingDict[k],
          GlobalAxis = true,
          NodeIndices = nodeIndices,
          LoadCaseIndex = loadCaseIndex
        };
        if (gsaLoad.Gwa(out var gwa, false))
        {
          Initialiser.AppResources.Cache.Upsert(keyword, index, gwa.First(), streamId, applicationId, gwaSetCommandType);
        }
      }

      return "";
    }
  }
}
