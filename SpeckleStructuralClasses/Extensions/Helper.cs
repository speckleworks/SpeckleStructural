using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;

namespace SpeckleStructuralClasses
{
  public static class Helper
  {
    public static void ScaleProperties(Dictionary<string, object> dict, double factor)
    {
      ScaleDictionary(ref dict, factor);
    }

    private static void ScaleDictionary(ref Dictionary<string, object> dict, double factor)
    {
      var keys = dict.Keys.ToList();
      foreach (var k in keys)
      {
        var v = dict[k];
        if (ScaleValue(ref v, factor))
        {
          dict[k] = v;
        }
      }
    }

    private static bool ScaleValue(ref object o, double factor)
    {
      if (ScalePrimitive(ref o, factor))
      {
        return true;
      }
      else
      {
        if (o is Dictionary<string, object> d)
        {
          ScaleDictionary(ref d, factor);
        }
        else if (o is Array || o is List<object>)
        {
          var list = ((IEnumerable<object>)o).ToList();
          for (var i = 0; i < list.Count(); i++)
          {
            var candidate = list[i];
            ScaleValue(ref candidate, factor);
          }
        }
        else
        {
          return ScaleObject(ref o, factor);
        }
      }
      return false;
    }

    private static bool ScaleObject(ref object o, double factor)
    {
      try
      {
        var scaleMethod = o.GetType().GetMethod("Scale");
        scaleMethod.Invoke(o, new object[] { factor });
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static bool ScalePrimitive(ref object p, double factor)
    {
      if (p is double)
      {
        p = (double)p * factor;
        return true;
      }
      else if (p is float)
      {
        p = (float)p * factor;
        return true;
      }
      else if (p is decimal)
      {
        p = (decimal)p * (decimal)factor;
        return true;
      }
      return (p is string);
    }

    /*
    public static void ScaleProperties(SpeckleObject so, double factor)
    {
      if (so.Properties != null)
      {
        var singles = new Dictionary<string, object>();
        var rest = new Dictionary<string, object>();

        foreach (var key in so.Properties.Keys)
        {
          if (so.Properties[key] is Dictionary<string, object>)
          {
            var prop2ndLevelDict = (Dictionary<string, object>)so.Properties[key];

            var simples = new Dictionary<string, object>();
            var complexDict = new Dictionary<string, object>();

            foreach (var k2 in prop2ndLevelDict.Keys)
            {
              if (prop2ndLevelDict[k2].GetType().IsSimple() || prop2ndLevelDict[k2] is Array || prop2ndLevelDict[k2] is List<object>)
              {
                simples.Add(k2, prop2ndLevelDict[k2]);
              }
              else
              {
                complexDict.Add(k2, prop2ndLevelDict[k2]);
              }
            }
            complexDict = so.ScaleProperties(complexDict, factor);

            ((Dictionary<string, object>)so.Properties[key]).Clear();
            so.Properties[key] = simples;
            foreach (var ck in complexDict.Keys)
            {
              ((Dictionary<string, object>)so.Properties[key])[ck] = complexDict[ck];
            }
          }
          else
          {
            singles.Add(key, so.Properties[key]);
          }
        }
        so.Properties = so.ScaleProperties(singles, factor);
        foreach (var k in rest.Keys)
        {
          so.Properties.Add(k, rest[k]);
        }
      }
    }
    */
  }
}
