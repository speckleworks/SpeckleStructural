using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Helper
  {
    #region loading
    public static readonly LoadDirection6[] loadDirSeq = new LoadDirection6[] { LoadDirection6.X, LoadDirection6.Y, LoadDirection6.Z, LoadDirection6.XX, LoadDirection6.YY, LoadDirection6.ZZ };

    public static bool IsValidLoading(StructuralVectorSix loading)
    {
      return (loading != null && loading.Value != null && loading.Value.Count() == loadDirSeq.Count() && loading.Value.Any(v => v != 0));
    }

    public static Dictionary<LoadDirection6, double> ExplodeLoading(StructuralVectorSix loading)
    {
      var valueByDir = new Dictionary<LoadDirection6, double>();

      for (var i = 0; i < Helper.loadDirSeq.Count(); i++)
      {
        if (loading.Value[i] != 0)
        {
          valueByDir.Add(Helper.loadDirSeq[i], loading.Value[i]);
        }
      }

      return valueByDir;
    }
    #endregion

    public static void AddCustomStructuralProperty(SpeckleObject obj, string key, object value)
    {
      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", value);
    }

    public static bool IsZeroAxis(StructuralAxis axis)
    {
      var bp = axis.basePlane;
      var zeroVector = new double[3] { 0, 0, 0 };
      return (bp.Xdir.Value.SequenceEqual(zeroVector) && bp.Ydir.Value.SequenceEqual(zeroVector));
    }

    public static string GridExpansionToString(GridExpansion expansion)
    {
      switch(expansion)
      {
        case GridExpansion.PlaneAspect: return "PLANE_ASPECT";
        case GridExpansion.PlaneSmooth: return "PLANE_SMOOTH";
        case GridExpansion.PlaneCorner: return "PLANE_CORNER";
        default: return "LEGACY";
      }
    }

    public static GridExpansion StringToGridExpansion(string expansion)
    {
      switch (expansion)
      {
        case "PLANE_ASPECT": return GridExpansion.PlaneAspect;
        case "PLANE_SMOOTH":  return GridExpansion.PlaneSmooth;
        case "PLANE_CORNER": return GridExpansion.PlaneCorner;
        default: return GridExpansion.Legacy;
      }
    }

    public static StructuralLoadCaseType StringToLoadCaseType(string type)
    {
      switch (type)
      {
        case "DEAD":
        case "LC_PERM_SELF": 
          return StructuralLoadCaseType.Dead;
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
        case StructuralLoadCaseType.Dead: return ("LC_PERM_SELF");
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
      var keyword = typeof(T).GetGSAKeyword().Split('.').First();
      var newLines = Initialiser.Cache.GetGwaToSerialise(keyword);
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

    public static StructuralVectorSix GsaLoadToLoading(LoadDirection6 ld, double value)
    {
      switch (ld)
      {
        case LoadDirection6.X: return new StructuralVectorSix(value, 0, 0, 0, 0, 0);
        case LoadDirection6.Y: return new StructuralVectorSix(0, value, 0, 0, 0, 0);
        case LoadDirection6.Z: return new StructuralVectorSix(0, 0, value, 0, 0, 0);
        case LoadDirection6.XX: return new StructuralVectorSix(0, 0, 0, value, 0, 0);
        case LoadDirection6.YY: return new StructuralVectorSix(0, 0, 0, 0, value, 0);
        case LoadDirection6.ZZ: return new StructuralVectorSix(0, 0, 0, 0, 0, value);
        default: return null;
      }
    }
  }
}
