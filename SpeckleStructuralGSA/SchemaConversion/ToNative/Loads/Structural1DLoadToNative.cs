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
      var keyword = GsaRecord.Keyword<GsaLoadBeam>();
      var gwaSetCommandType = GsaRecord.GetGwaSetCommandType<GsaLoadBeam>();
      var streamId = Initialiser.Cache.LookupStream(load.ApplicationId);

      var loadCaseIndex = Initialiser.Cache.ResolveIndex(GsaRecord.Keyword<GsaLoadCase>(), load.LoadCaseRef);

      var entityKeyword = (Initialiser.Settings.TargetLayer == GSATargetLayer.Design) ? GsaRecord.Keyword<GsaMemb>() : GsaRecord.Keyword<GsaEl>();
      var entityIndices = Initialiser.Cache.LookupIndices(entityKeyword, load.ElementRefs).Where(i => i.HasValue).Select(i => i.Value).ToList();

      var loadingDict = Helper.ExplodeLoading(load.Loading);
      foreach (var k in loadingDict.Keys)
      {
        var applicationId = string.Join("_", load.ApplicationId, k.ToString());
        var index = Initialiser.Cache.ResolveIndex(keyword, applicationId);
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
            Initialiser.Cache.Upsert(keyword, index, gwaLine, streamId, applicationId, gwaSetCommandType);
          }
        }
      }

      return "";
    }
  }
}
