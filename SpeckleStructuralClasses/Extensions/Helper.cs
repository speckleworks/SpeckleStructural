using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public static class Helper
  {
    public static double PointComparisonEpsilon = 0.0001;

    public static void ScaleProperties(Dictionary<string, object> dict, double factor)
    {
      ScaleDictionary(ref dict, factor);
    }

    public static string CreateChildApplicationId(int childId, string parentApplicationId)
    {
      return string.Join("_", new[] { parentApplicationId, childId.ToString() });
    }

    public static string CreateChildApplicationId(string childId, string parentApplicationId)
    {
      return string.Join("_", new[] { parentApplicationId, childId });
    }

    public static string ExtractParentApplicationId(string appId)
    {
      return appId.Split(new[] { '_' }).First();
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
      if (IsPrimitive(o))
      {
        return true;
      }
      else
      {
        if (o is Dictionary<string, object> d)
        {
          ScaleDictionary(ref d, factor);
        }
        else if (o is IList && o.GetType().IsGenericType)
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

    private static bool IsPrimitive(object p)
    {
      return (p is string || p is bool || p is int || p is short || p is long || p is double || p is float || p is decimal);
    }
  }
}
