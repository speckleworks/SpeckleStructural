using System;
using System.Linq;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Note: LOAD_TITLE is one of the keywords that forgets SID between setting and retrieving from the GSA model
  public static class StructuralLoadCaseToNative
  {
    public static string ToNative(this StructuralLoadCase loadCase)
    {
      if (string.IsNullOrEmpty(loadCase.ApplicationId))
      {
        return "";
      }
      return Helper.ToNativeTryCatch(loadCase, () =>
        {
          var keyword = GsaRecord.GetKeyword<GsaLoadCase>();
          var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, loadCase.ApplicationId);
          var streamId = Initialiser.AppResources.Cache.LookupStream(loadCase.ApplicationId);
          var gsaLoad = new GsaLoadCase()
          {
            ApplicationId = loadCase.ApplicationId,
            Index = index,
            StreamId = streamId,
            Title = loadCase.Name,
            CaseType = loadCase.CaseType
          };

          if (gsaLoad.Gwa(out var gwaLines, false))
          {
            Initialiser.AppResources.Cache.Upsert(keyword, index, gwaLines.First(), streamId, loadCase.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaLoadCase>());
          }
          //TO DO: add to error messages shown on UI
          return "";
        }
      );
    }
  }
}
