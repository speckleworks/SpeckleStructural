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

  [GsaType(GwaKeyword.ALIGN, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.GRID_SURFACE)]
  public class GsaAlign : BlankGsaRecord { }

  [GsaType(GwaKeyword.ANAL, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.LOAD_TASK)]
  public class GsaAnal : BlankGsaRecord { }

  [GsaType(GwaKeyword.ANAL_STAGE, GwaSetCommandType.Set, true, StreamBucket.Model)]
  public class GsaAnalStage : BlankGsaRecord { }

  //Check when implementing: is LOAD_TASK truly a referenced keyword?
  [GsaType(GwaKeyword.COMBINATION, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.LOAD_TASK)]
  public class GsaCombination : BlankGsaRecord { }

  [GsaType(GwaKeyword.GRID_LINE, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE)]
  public class GsaGridLine: BlankGsaRecord { }

  [GsaType(GwaKeyword.INF_NODE, GwaSetCommandType.SetAt, true, StreamBucket.Model, false, true, GwaKeyword.NODE, GwaKeyword.AXIS)]
  public class GsaINfNode : BlankGsaRecord { }

  [GsaType(GwaKeyword.MAT_CONCRETE, GwaSetCommandType.Set, true, StreamBucket.Model)]
  public class GsaMatConcrete : BlankGsaRecord { }

  [GsaType(GwaKeyword.MAT_STEEL, GwaSetCommandType.Set, true, StreamBucket.Model)]
  public class GsaMatSteel: BlankGsaRecord { }

  [GsaType(GwaKeyword.PATH, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.ALIGN)]
  public class GsaPath : BlankGsaRecord { }

  [GsaType(GwaKeyword.PROP_2D, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE, GwaKeyword.AXIS)]
  public class GsaProp2d : BlankGsaRecord { }

  //This seems to be an alternative to SECTION (corresponding to 1D properties) - to be investigated further
  [GsaType(GwaKeyword.PROP_SEC, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.MAT_STEEL, GwaKeyword.MAT_CONCRETE)]
  public class GsaPropSec : BlankGsaRecord { }

  [GsaType(GwaKeyword.PROP_SPR, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.AXIS)]
  public class GsaPropSpr : BlankGsaRecord { }

  [GsaType(GwaKeyword.USER_VEHICLE, GwaSetCommandType.Set, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.LOAD_TASK)]
  public class GsaUserVehicle : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_GRID_POINT, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridPoint : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_GRID_LINE, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.GRID_SURFACE, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS)]
  public class GsaLoadGridLine : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_2D_FACE, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.AXIS, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dFace : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_2D_THERMAL, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB)]
  public class GsaLoad2dThermal : BlankGsaRecord { }

  [GsaType(GwaKeyword.LOAD_GRAVITY, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.LOAD_TITLE, GwaKeyword.EL, GwaKeyword.MEMB, GwaKeyword.NODE)]
  public class GsaLoadGravity : BlankGsaRecord { }

  [GsaType(GwaKeyword.INF_BEAM, GwaSetCommandType.SetAt, true, StreamBucket.Model, false, true, GwaKeyword.AXIS, GwaKeyword.EL)]
  public class GsaInfBeam : BlankGsaRecord { }

  [GsaType(GwaKeyword.RIGID, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE, GwaKeyword.MEMB)]
  public class GsaRigid : BlankGsaRecord { }

  [GsaType(GwaKeyword.GEN_REST, GwaSetCommandType.SetAt, true, StreamBucket.Model, GwaKeyword.ANAL_STAGE, GwaKeyword.NODE)]
  public class GsaGenRest : BlankGsaRecord { }
}
