using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  public abstract class BlankGsaRecord : GsaRecord
  {
    public override bool FromGwa(string gwa) => false;
    public override bool Gwa(out List<string> gwa, bool includeSet = false) { throw new NotImplementedException(); }
  }

  [GsaType(GwaKeyword.ALIGN, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE)]
  public class GsaAlign : BlankGsaRecord { }

  [GsaType(GwaKeyword.GRID_LINE, GwaSetCommandType.Set, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE)]
  public class GsaGridLine: BlankGsaRecord { }

  [GsaType(GwaKeyword.INF_NODE, GwaSetCommandType.SetAt, true, false, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaINfNode : BlankGsaRecord { }

  [GsaType(GwaKeyword.MAT_CONCRETE, GwaSetCommandType.Set, true)]
  public class GsaMatConcrete : BlankGsaRecord { }

  [GsaType(GwaKeyword.MAT_STEEL, GwaSetCommandType.Set, true)]
  public class GsaMatSteel: BlankGsaRecord { }

  [GsaType(GwaKeyword.PATH, GwaSetCommandType.Set, true, GwaKeyword.ALIGN)]
  public class GsaPath : BlankGsaRecord { }

  [GsaType(GwaKeyword.PROP_2D, GwaSetCommandType.Set, true, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE, GwaKeyword.AXIS)]
  public class GsaProp2d : BlankGsaRecord { }

  //This seems to be an alternative to SECTION (corresponding to 1D properties) - to be investigated further
  [GsaType(GwaKeyword.PROP_SEC, GwaSetCommandType.Set, true, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE)]
  public class GsaPropSec : BlankGsaRecord { }

  [GsaType(GwaKeyword.PROP_SPR, GwaSetCommandType.Set, true, GwaKeyword.AXIS)]
  public class GsaPropSpr : BlankGsaRecord { }

  [GsaType(GwaKeyword.USER_VEHICLE, GwaSetCommandType.Set, true, GwaKeyword.LOAD_TITLE, GwaKeyword.TASK)]
  public class GsaUserVehicle : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_GRID_POINT, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridPoint : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_GRID_LINE, GwaSetCommandType.SetAt, true, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridLine : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_2D_FACE, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dFace : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_2D_THERMAL, GwaSetCommandType.SetAt, true, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dThermal : BlankGsaRecord { }

  [GsaType(GwaKeyword.INF_BEAM, GwaSetCommandType.SetAt, true, false, true, GwaKeyword.AXIS, GwaKeyword.EL)]
  public class GsaInfBeam : BlankGsaRecord { }

  [GsaType(GwaKeyword.RIGID, GwaSetCommandType.SetAt, true, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE, GwaKeyword.MEMB)]
  public class GsaRigid : BlankGsaRecord { }

  [GsaType(GwaKeyword.GEN_REST, GwaSetCommandType.SetAt, true, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE)]
  public class GsaGenRest : BlankGsaRecord { }
}
