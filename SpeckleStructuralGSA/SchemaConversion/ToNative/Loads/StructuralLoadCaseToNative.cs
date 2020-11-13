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

      var keyword = GsaRecord.Keyword<GsaLoadCase>();
      var gsaLoad = new GsaLoadCase()
      {
        ApplicationId = loadCase.ApplicationId,
        Title = loadCase.Name,
        Index = Initialiser.Cache.ResolveIndex(keyword, loadCase.ApplicationId),
        CaseType = loadCase.CaseType
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
