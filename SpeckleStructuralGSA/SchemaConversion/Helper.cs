using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Helper
  {
    public static bool IsZeroAxis(StructuralAxis axis)
    {
      var bp = axis.basePlane;
      var zeroVector = new double[3] { 0, 0, 0 };
      return (bp.Xdir.Value.SequenceEqual(zeroVector) && bp.Ydir.Value.SequenceEqual(zeroVector));
    }

    public static StructuralLoadCaseType StringToLoadCaseType(string type)
    {
      switch (type)
      {
        case "DEAD": return StructuralLoadCaseType.Dead;
        case "LC_VAR_IMP": return StructuralLoadCaseType.Live;
        case "WIND": return StructuralLoadCaseType.Wind;
        case "SNOW": return StructuralLoadCaseType.Snow;
        case "SEISMIC": return StructuralLoadCaseType.Earthquake;
        case "LC_PERM_SOIL": return StructuralLoadCaseType.Soil;
        case "LC_VAR_TEMP": return StructuralLoadCaseType.Thermal;
        default: return StructuralLoadCaseType.Generic;
      }
    }

    public static string LoadCaseTypeToString(StructuralLoadCaseType caseType)
    {
      switch (caseType)
      {
        case StructuralLoadCaseType.Dead: return ("DEAD");
        case StructuralLoadCaseType.Live: return ("LC_VAR_IMP");
        case StructuralLoadCaseType.Wind: return ("WIND");
        case StructuralLoadCaseType.Snow: return ("SNOW");
        case StructuralLoadCaseType.Earthquake: return ("SEISMIC");
        case StructuralLoadCaseType.Soil: return ("LC_PERM_SOIL");
        case StructuralLoadCaseType.Thermal: return ("LC_VAR_TEMP");
        default: return ("LC_UNDEF");
      }
    }

    public static List<U> GetNewFromCache<T, U>() where U : GsaRecord  //T = old type, U = new schema type
    {
      //Convert all raw GWA into GSA schema objects
      var newLines = Initialiser.Cache.GetGwaToSerialise(typeof(T).GetGSAKeyword());
      var schemaObjs = new List<U>();
      foreach (var index in newLines.Keys)
      {
        var obj = (GsaRecord)Activator.CreateInstance(typeof(U));
        obj.Index = index;

        if (!obj.FromGwa(newLines[index]))
        {
          Initialiser.AppUI.Message(typeof(U).Name + ": Unable to parse GWA", index.ToString());
        }
        schemaObjs.Add((U)obj);
      }
      return schemaObjs;
    }

    public static List<U> GetAllFromCache<T, U>() where U : GsaRecord  //T = old type, U = new schema type
    {
      //Convert all raw GWA into GSA schema objects
      Initialiser.Cache.GetKeywordRecordsSummary(typeof(T).GetGSAKeyword(), out var gwa, out var indices, out var appIds);
      var schemaObjs = new List<U>();
      for (var i = 0; i < gwa.Count(); i++)
      {
        var index = indices[i];

        var obj = (GsaRecord)Activator.CreateInstance(typeof(U));
        obj.Index = index;

        if (!obj.FromGwa(gwa[i]))
        {
          Initialiser.AppUI.Message(typeof(U).Name + ": Unable to parse GWA", index.ToString());
        }
        schemaObjs.Add((U)obj);
      }
      return schemaObjs;
    }

    public static StructuralVectorSix GsaLoadToLoading(LoadDirection ld, double value)
    {
      switch (ld)
      {
        case LoadDirection.X: return new StructuralVectorSix(value, 0, 0, 0, 0, 0);
        case LoadDirection.Y: return new StructuralVectorSix(0, value, 0, 0, 0, 0);
        case LoadDirection.Z: return new StructuralVectorSix(0, 0, value, 0, 0, 0);
        case LoadDirection.XX: return new StructuralVectorSix(0, 0, 0, value, 0, 0);
        case LoadDirection.YY: return new StructuralVectorSix(0, 0, 0, 0, value, 0);
        case LoadDirection.ZZ: return new StructuralVectorSix(0, 0, 0, 0, 0, value);
        default: return null;
      }
    }
  }
}
