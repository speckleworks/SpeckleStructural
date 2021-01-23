using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleGSAInterfaces;
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
      var keyword = GsaRecord.GetKeyword<GsaLoadBeam>();
      var gwaSetCommandType = GsaRecord.GetGwaSetCommandType<GsaLoadBeam>();
      var streamId = Initialiser.AppResources.Cache.LookupStream(load.ApplicationId);

      var loadCaseIndex = Initialiser.AppResources.Cache.ResolveIndex(GsaRecord.GetKeyword<GsaLoadCase>(), load.LoadCaseRef);

      var entityKeyword = (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design) ? GsaRecord.GetKeyword<GsaMemb>() : GsaRecord.GetKeyword<GsaEl>();
      var entityIndices = Initialiser.AppResources.Cache.LookupIndices(entityKeyword, load.ElementRefs).Where(i => i.HasValue).Select(i => i.Value).ToList();

      var loadingDict = Helper.ExplodeLoading(load.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", load.ApplicationId, k.ToString());
        var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, applicationId);
        var gsaLoad = new GsaLoadBeamUdl()
        {
          Index = index,
          ApplicationId = applicationId,
          StreamId = streamId,
          Name = load.Name,
          LoadDirection = k,
          Load = loadingDict[k],
          Projected = false,
          AxisRefType = LoadBeamAxisRefType.Global,
          LoadCaseIndex = loadCaseIndex,
          Entities = entityIndices
        };

        if (gsaLoad.Gwa(out var gwa, false))
        {
          foreach (var gwaLine in gwa)
          {
            Initialiser.AppResources.Cache.Upsert(keyword, index, gwaLine, streamId, applicationId, gwaSetCommandType);
          }
        }
      }

      return "";
    }
  }
}
