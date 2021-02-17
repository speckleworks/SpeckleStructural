using System;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA
{
  [GSAObject("SECTION.3", new string[] { "MAT_STEEL.3", "MAT_CONCRETE.17" }, "model", true, true, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) }, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) })]
  public class GSA1DPropertyExplicit : GSABase<Structural1DPropertyExplicit>
  {
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    //Reminder: the SpeckleSA application won't find this on its own because the type (GsaSection) isn't marked as a IGSASpeckleContainer
    public static SpeckleObject ToSpeckle(this GSA1DPropertyExplicit dummyObject) => (new GsaSection()).ToSpeckle();
  }
}
