/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  /// <summary>
  /// Static class responsible of tracking and assigning indices for GSA records.
  /// </summary>
  public class Indexer
  {
    private static Dictionary<string, int> indexMap = new Dictionary<string, int>();
    private static Dictionary<string, int> counter = new Dictionary<string, int>();
    private static Dictionary<string, List<int>> indexUsed = new Dictionary<string, List<int>>();
    private static Dictionary<string, List<int>> baseLine = new Dictionary<string, List<int>>();

    /// <summary>
    /// Reset indexer.
    /// </summary>
    public void Clear()
    {
      indexMap.Clear();
      counter.Clear();
      indexUsed.Clear();
      baseLine.Clear();
    }

    #region Indexer
    /// <summary>
    /// Return the next available index for the associated GSA keyword.
    /// </summary>
    /// <param name="keywordGSA">GSA keyword</param>
    /// <returns>Index</returns>
    private int NextIndex(string keywordGSA)
    {
      if (!counter.ContainsKey(keywordGSA))
        counter[keywordGSA] = 1;

      if (indexUsed.ContainsKey(keywordGSA))
        while (indexUsed[keywordGSA].Contains(counter[keywordGSA]))
          counter[keywordGSA]++;

      return counter[keywordGSA]++;
    }

    /// <summary>
    /// Resolve the next index for the given Type.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <returns>Index</returns>
    public int ResolveIndex(Type type)
    {
      return ResolveIndex(type.GetGSAKeyword(), string.Empty, type.Name);
    }

    /// <summary>
    /// Resolve the index for the given IStructural object.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="obj">IStructural object</param>
    /// <returns>Index</returns>
    public int ResolveIndex(Type type, dynamic obj)
    {
      return ResolveIndex(type.GetGSAKeyword(), obj.ApplicationId, type.Name);
    }

    /// <summary>
    /// Resolve the index for the given application ID.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="applicationId">application ID</param>
    /// <returns>Index</returns>
    public int ResolveIndex(Type type, string applicationId)
    {
      return ResolveIndex(type.GetGSAKeyword(), applicationId, type.Name);
    }

    /// <summary>
    /// Resolve the next index for the given keyword.
    /// </summary>
    /// <param name="keyword">GSA keyword</param>
    /// <returns>Index</returns>
    public int ResolveIndex(string keyword)
    {
      return ResolveIndex(keyword, string.Empty, string.Empty);
    }

    /// <summary>
    /// Resolve the index for the given IStructural object.
    /// </summary>
    /// <param name="keyword">GSA keyword</param>
    /// <param name="obj">IStructural object</param>
    /// <returns>Index</returns>
    public int ResolveIndex(string keyword, dynamic obj)
    {
      return ResolveIndex(keyword, obj.ApplicationId, string.Empty);
    }

    /// <summary>
    /// Resolve the index for the given application ID.
    /// </summary>
    /// <param name="keyword">GSA keyword</param>
    /// <param name="applicationId">application ID</param>
    /// <returns>Index</returns>
    public int ResolveIndex(string keyword, string applicationId)
    {
      return ResolveIndex(keyword, applicationId, string.Empty);
    }

    /// <summary>
    /// Resolve the index for the given GSA keyword, application ID, and type.
    /// </summary>
    /// <param name="keywordGSA">GSA keyword</param>
    /// <param name="applicationId">application ID</param>
    /// <param name="type">Type name</param>
    /// <returns>Index</returns>
    private int ResolveIndex(string keywordGSA, string applicationId, string type = "")
    {
      // If no ID set, return next one but do not store.
      if (applicationId == null || applicationId == string.Empty)
        return NextIndex(keywordGSA);

      string key = keywordGSA + ":" + type + ":" + applicationId;

      if (!indexMap.ContainsKey(key))
        indexMap[key] = NextIndex(keywordGSA);

      return indexMap[key];
    }

    /// <summary>
    /// Resolve the index for the given IStructural objects.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="objects">List of IStructural object</param>
    /// <returns>List of indices</returns>
    public List<int> ResolveIndices(Type type, List<IStructural> objects)
    {
      return objects.Select(o => ResolveIndex(type, o)).ToList();
    }

    /// <summary>
    /// Resolve the index for the given list of application IDs.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="applicationId">List of application ID</param>
    /// <returns>List of indices</returns>
    public List<int> ResolveIndices(Type type, List<string> applicationId)
    {
      return applicationId.Select(s => ResolveIndex(type, s)).ToList();
    }

    /// <summary>
    /// Find the index associated with the IStructural object.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="obj">IStructural object</param>
    /// <returns>Index</returns>
    public int? LookupIndex(Type type, dynamic obj)
    {
      return LookupIndex(type.GetGSAKeyword(), obj.applicationId, type.Name);
    }

    /// <summary>
    /// Find the index associated with the application ID.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="applicationId">application ID</param>
    /// <returns>Index</returns>
    public int? LookupIndex(Type type, string applicationId)
    {
      return LookupIndex(type.GetGSAKeyword(), applicationId, type.Name);
    }

    /// <summary>
    /// Find the index associated with the GSA keyword, application ID, and type.
    /// </summary>
    /// <param name="keywordGSA">GSA keyword</param>
    /// <param name="applicationId">application ID</param>
    /// <param name="type">Type name</param>
    /// <returns>Index</returns>
    private int? LookupIndex(string keywordGSA, string applicationId, string type = "")
    {
      if (applicationId == null || applicationId == string.Empty)
        return null;

      string key = keywordGSA + ":" + type + ":" + applicationId;

      if (!indexMap.ContainsKey(key))
        return null;

      return indexMap[key];
    }

    /// <summary>
    /// Find the indices associated with the IStructural objects.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="objects">List of IStructural objects</param>
    /// <returns>List of indices</returns>
    public List<int?> LookupIndices(Type type, List<IStructural> objects)
    {
      return objects.Select(o => LookupIndex(type, o)).ToList();
    }

    /// <summary>
    /// Find the indices associated with the list of application IDs.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="applicationId">List of application IDs</param>
    /// <returns>List of indices</returns>
    public List<int?> LookupIndices(Type type, List<string> applicationId)
    {
      return applicationId.Select(s => LookupIndex(type, s)).ToList();
    }

    /// <summary>
    /// Reserve the indices for the given type.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="refs">List of indices</param>
    public void ReserveIndices(Type type, List<int> refs)
    {
      ReserveIndices(type.GetGSAKeyword(), refs);
    }

    /// <summary>
    /// Reserve the indices for the given keyword.
    /// </summary>
    /// <param name="keywordGSA">GSA keyword</param>
    /// <param name="refs">List of indices</param>
    public void ReserveIndices(string keywordGSA, List<int> refs)
    {
      if (!indexUsed.ContainsKey(keywordGSA))
        indexUsed[keywordGSA] = refs;
      else
        indexUsed[keywordGSA].AddRange(refs);

      indexUsed[keywordGSA] = indexUsed[keywordGSA].Distinct().ToList();
    }

    /// <summary>
    /// Reserve the indices for the given type and adds a mapping.
    /// </summary>
    /// <param name="type">GSAObject type</param>
    /// <param name="refs">List of indices</param>
    /// <param name="applicationId">List of application IDs</param>
    public void ReserveIndicesAndMap(Type type, List<int> refs, List<string> applicationId)
    {
      string keywordGSA = type.GetGSAKeyword();

      for (int i = 0; i < applicationId.Count(); i++)
      {
        string key = keywordGSA + ":" + type.Name + ":" + applicationId[i];
        indexMap[key] = refs[i];
      }

      ReserveIndices(type, refs);
    }
    #endregion

    #region Base Line
    /// <summary>
    /// Set the current indices reserved as a base line.
    /// </summary>
    public void SetBaseline()
    {
      baseLine.Clear();
      foreach (KeyValuePair<string, List<int>> kvp in indexUsed)
        baseLine[kvp.Key] = new List<int>(kvp.Value);
    }

    /// <summary>
    /// Reset the indexer to the baseline.
    /// </summary>
    public void ResetToBaseline()
    {
      indexUsed.Clear();
      indexMap.Clear();
      counter.Clear();
      foreach (KeyValuePair<string, List<int>> kvp in baseLine)
        indexUsed[kvp.Key] = new List<int>(kvp.Value);
    }

    /// <summary>
    /// Check if the index is in the base line.
    /// </summary>
    /// <param name="keywordGSA">GSA keyword</param>
    /// <param name="index">Index</param>
    /// <returns>True if the index is in the baseline</returns>
    public bool InBaseline(string keywordGSA, int index)
    {
      if (baseLine.ContainsKey(keywordGSA))
        if (baseLine[keywordGSA].Contains(index))
          return true;

      return false;
    }
    #endregion
  }
}
*/
