using System;
using System.Collections.Generic;
using SpeckleCore;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    private static List<string> ToSpeckleBase<T>()
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword();

      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(T)))
      {
        Initialiser.GSASenderObjects[objType] = new List<object>();
      }

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      return Initialiser.Indexer.GetGwaToSerialise(keyword);
    }

    private static void ToSpeckleBase<T,S>(out List<string> newLines)
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
      newLines = Initialiser.Indexer.GetGwaToSerialise(keyword);
    }

    public static string ToSpeckleTypeName(this Type GSAType)
    {
      return ((SpeckleObject) ((IGSASpeckleContainer)Activator.CreateInstance(GSAType)).Value).Type;
    }
  }
}
