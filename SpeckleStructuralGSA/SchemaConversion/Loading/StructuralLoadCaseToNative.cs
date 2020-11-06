using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  //Note: LOAD_TITLE is one of the keywords that forgets SID between setting and retrieving from the GSA model
  public static class StructuralLoadCaseToNative
  {
    public static string ToNative(this StructuralLoadCase speckleLoad)
    {
      var keyword = GsaRecord.Keyword<GsaLoadCase>();
      var gsaLoad = new GsaLoadCase()
      {
        ApplicationId = speckleLoad.ApplicationId,
        Title = speckleLoad.Name,
        Index = Initialiser.Cache.ResolveIndex(keyword, speckleLoad.ApplicationId),
        CaseType = speckleLoad.CaseType
      };

      if (gsaLoad.Gwa(out var gwaLines, true))
      {
        return string.Join("\n", gwaLines);
      }
      //TO DO: add to error messages shown on UI
      return "";
    }
  }
}
