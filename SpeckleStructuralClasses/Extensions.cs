using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public static class Extensions
  {
    public static List<T> ValueAsTypedList<T> (this Dictionary<string, object> d, string key)
    {
      if (!d.ContainsKey(key)) return null;

      try
      {
        if (d[key] is List<T>)
        {
          return (List<T>)d[key];
        }
        else if (d[key] is List<object>)
        {
          //Note: this will return a new list instance, so calling Add() or AddRange() on the return value will not alter
          //the structural properties dictionary.
          return (List<T>)(((List<object>)d[key]).Cast<T>().ToList());
        }
      }
      catch { }
      return null;
    }

    public static List<double> ValueAsDoubleList(this Dictionary<string, object> d, string key)
    {
      if (!d.ContainsKey(key)) return null;

      try
      {
        if (d[key] is List<double>)
        {
          return (List<double>)d[key];
        }
        else if (d[key] is List<object>)
        {
          //Note: this will return a new list instance, so calling Add() or AddRange() on the return value will not alter
          //the structural properties dictionary.

          var retList = new List<double>();
          foreach (var dv in (List<object>)d[key])
          {
            try
            {
              var dblVal = Convert.ToDouble(dv);
              retList.Add(dblVal);
            }
            catch
            {
              retList.Add(0);
            }
          }

          return retList;
        }
      }
      catch { }
      return null;
    }
  }
}
