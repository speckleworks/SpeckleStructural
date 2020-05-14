using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public class GSASenderDictionary : IGSASenderDictionary
  {
    private readonly object syncLock = new object();
    private readonly Dictionary<Type, List<object>> dict = new Dictionary<Type, List<object>>();

    public void Clear() => ExecuteWithLock(() => dict.Clear());

    public int Count<T>() => ExecuteWithLock(() => (dict.ContainsKey(typeof(T))) ? dict[typeof(T)].Count() : 0);

    public List<T> Get<T>() => ExecuteWithLock(() => (dict.ContainsKey(typeof(T))) ? dict[typeof(T)].Cast<T>().ToList() : new List<T>());

    public Dictionary<Type, List<object>> GetAll() => ExecuteWithLock(() =>
      {
        var retDict = new Dictionary<Type, List<object>>();
        foreach (var t in dict.Keys)
        {
          if (dict[t] != null && dict[t].Count() > 0)
          {
            retDict.Add(t, new List<object>());
            retDict[t].AddRange(dict[t]);
          }
        }
        return retDict;
      });

    public bool Add<T>(T o) => ExecuteWithLock(() =>
      {
        var t = typeof(T);
        if (dict.ContainsKey(t))
        {
          dict[t].Add(o);
        }
        else
        {
          dict.Add(t, new List<object>() { o });
        }
        return true;
      });

    public bool AddRange<T>(List<T> os) => ExecuteWithLock(() =>
      {
        var t = typeof(T);
        var objList = new List<object>();
        foreach (var o in os)
        {
          objList.Add((object)o);
        }
        if (dict.ContainsKey(t))
        {
          dict[t].AddRange(objList);
        }
        else
        {
          dict.Add(t, objList);
        }
        return true;
      });

    public void RemoveAll<T>(List<T> os)
    {
      ExecuteWithLock(() =>
      {
        var t = typeof(T);
        foreach(var o in os)
        {
          if (dict[t].Contains(o))
          {
            dict[t].Remove(o);
          }
        }
      });
    }

    private T ExecuteWithLock<T>(Func<T> f)
    {
      lock (syncLock)
      {
        return f();
      }
    }

    private void ExecuteWithLock(Action a)
    {
      lock (syncLock)
      {
        a();
      }
    }
  }
}
