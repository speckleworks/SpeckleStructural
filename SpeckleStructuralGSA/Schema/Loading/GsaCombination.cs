using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  //Check when implementing: is TASK truly a referenced keyword?
  [GsaType(GwaKeyword.COMBINATION, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.TASK)]
  public class GsaCombination : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public string Desc;
    public bool Bridge;
    public string Note;

    public GsaCombination()
    {
      Version = 1;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }

      //COMBINATION | case | name | desc | bridge | note
      return FromGwaByFuncs(remainingItems, out var _, AddName, (v) => AddStringValue(v, out Desc), AddBridge, (v) => AddStringValue(v, out Note));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //COMBINATION | case | name | desc | bridge | note
      AddItems(ref items, Name, Desc, AddBridge(), Note);

      gwa = Join(items, out var gwaLine) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    #region to_gwa_fns
    private string AddBridge()
    {
      return (Bridge) ? "BRIDGE" : "";
    }
    #endregion

    #region from_gwa_fns
    private bool AddBridge(string v)
    {
      Bridge = !(string.IsNullOrEmpty(v));
      return false;
    }

    #endregion
  }
}
