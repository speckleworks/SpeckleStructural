using System;

namespace SpeckleStructuralGSA.Test
{
  [Serializable]
  public class GwaRecord
  {
    public string ApplicationId { get; set; }
    public string GwaCommand { get; set; }

    public GwaRecord(string applicationId, string gwaCommand)
    {
      ApplicationId = applicationId;
      GwaCommand = gwaCommand;
    }
  }
}
