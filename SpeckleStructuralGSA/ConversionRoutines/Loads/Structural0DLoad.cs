using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_NODE.2", new string[] { "NODE.3", "AXIS.1" }, "model", true, true, new Type[] { typeof(GSANode), typeof(GSALoadCase) }, new Type[] { typeof(GSANode), typeof(GSALoadCase) })]
  public class GSA0DLoad : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoad();

  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSA0DLoad dummyObject)
    {
      //Reminder: the SpeckleSA application won't find this on its own because the type (Gsa0dLoad) isn't marked as a IGSASpeckleContainer
      return (new GsaLoadNode()).ToSpeckle();
    }
  }
}
