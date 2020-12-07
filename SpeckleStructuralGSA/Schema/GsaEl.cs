using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.EL, GwaSetCommandType.Set, StreamBucket.Model)]
  public class GsaEl : GsaRecord
  {
    public GsaEl() : base()
    {
      Version = 4;
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
