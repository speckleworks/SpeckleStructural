using SpeckleCore;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public class Initialiser : ISpeckleInitializer
  {
    private static GsaKit gsaKit = new GsaKit();

    public static IGSAKit GsaKit { get => gsaKit; }
    public static IGSAAppResources AppResources { get; set; }
  }

}
