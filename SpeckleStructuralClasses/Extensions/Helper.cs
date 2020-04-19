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
    private static bool IsSimple(this Type type)
    {
      return (type.IsPrimitive || type.Equals(typeof(string)));
    }

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

            foreach (var k2 in prop2ndLevelDict.Keys)
            {
              var simples = new Dictionary<string, object>();
              var complexDict = new Dictionary<string, object>();
              if (prop2ndLevelDict[k2].GetType().IsSimple())
              {
                simples.Add(k2, prop2ndLevelDict[k2]);
              }
              else
              {
                complexDict.Add(k2, prop2ndLevelDict[k2]);
              }
              complexDict = so.ScaleProperties(complexDict, factor);

              ((Dictionary<string, object>)so.Properties[key]).Clear();
              so.Properties[key] = simples;
              foreach (var ck in complexDict.Keys)
              {
                ((Dictionary<string, object>)so.Properties[key])[ck] = complexDict[ck];
              }
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
  }
}
