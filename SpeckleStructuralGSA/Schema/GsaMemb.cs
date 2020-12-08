using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.MEMB, GwaSetCommandType.Set, StreamBucket.Model, true, false)]
  public class GsaMemb : GsaRecord
  {
    public GsaMemb() : base()
    {
      Version = 8;
    }

    public override bool FromGwa(string gwa)
    {
      throw new NotImplementedException();
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      throw new NotImplementedException();
    }
  }
}
