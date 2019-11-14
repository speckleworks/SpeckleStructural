using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    private static Dictionary<int, string> ToSpeckleBase<T>()
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword();

      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(T)))
      {
        Initialiser.GSASenderObjects[objType] = new List<object>();
      }

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      return Initialiser.Cache.GetGwaToSerialise(keyword);
    }

    private static void ToSpeckleBase<T,S>(out Dictionary<int, string> newLines)
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword();
      var subKeywords = objType.GetSubGSAKeyword();

      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(T)))
      {
        Initialiser.GSASenderObjects[objType] = new List<object>();
      }

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      newLines = Initialiser.Cache.GetGwaToSerialise(keyword);
    }

    public static string ToSpeckleTypeName(this Type GSAType)
    {
      return ((SpeckleObject) ((IGSASpeckleContainer)Activator.CreateInstance(GSAType)).Value).Type;
    }
  }
}
