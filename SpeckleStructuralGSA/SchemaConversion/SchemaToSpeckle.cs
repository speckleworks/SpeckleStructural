using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA;
using SpeckleStructuralGSA.Schema;
using SQLitePCL;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class SchemaConversions
  {
    public static SpeckleObject ToSpeckle(this Gsa0dLoad dummyObject)
    {
      var typeName = dummyObject.GetType().Name;
      var gsaLoads = Helper.GetNewFromCache<GSA0DLoad, Gsa0dLoad>();
      var gsaNodes = Initialiser.GSASenderObjects.Get<GSANode>();
      var keyword = GsaRecord.Keyword<Gsa0dLoad>();

      var structural0DLoads = new List<Structural0DLoad>();

      Add0dLoadsWithoutAppId(keyword, gsaLoads, gsaNodes, ref structural0DLoads);
      Add0dLoadsWithAppId(keyword, gsaLoads, gsaNodes, ref structural0DLoads);

      var loads = structural0DLoads.Select(sl => new GSA0DLoad() { Value = sl }).ToList();

      Initialiser.GSASenderObjects.AddRange(loads);
      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    #region 0dLoad_methods
    private struct LoadingData
    {
      public int? LoadCaseIndex;
      public int? NodeIndex;
      public int d0LoadIndex;
    }

    //D0__ because class names can't start with numbers ..
    private class D0LoadingSummary
    {
      public int Index;
      public int? LoadCaseIndex;
      public int? NodeIndex;
      public int? UniqueLoadingIndex;

      public D0LoadingSummary(int index, int? loadCaseIndex, int? nodeIndex, int? uniqueLoadingIndex)
      {
        Index = index;
        LoadCaseIndex = loadCaseIndex;
        NodeIndex = nodeIndex;
        UniqueLoadingIndex = uniqueLoadingIndex;
      }
    }

    private static bool Add0dLoadsWithoutAppId(string keyword, List<Gsa0dLoad> gsaLoads, List<GSANode> gsaNodes, ref List<Structural0DLoad> structural0DLoads)
    {
      var summaries = new List<D0LoadingSummary>();
      var uniqueLoadings = new List<StructuralVectorSix>();
      var nodeKeyword = GsaRecord.Keyword<GsaNode>();
      var loadCaseKeyword = GsaRecord.Keyword<GsaLoadCase>();

      foreach (var gl in gsaLoads.Where(l => l.Index.HasValue))
      {
        var relevantGsaNodes = gsaNodes.Where(n => gl.NodeIndices.Any(ni => ni == n.GSAId)).ToList();
        foreach (var n in relevantGsaNodes)
        {
          var loading = Helper.GsaLoadToLoading(gl.LoadDirection, gl.Value.Value);
          int? uniqueLoadingIndex = null;
          if (!gl.GlobalAxis)
          {
            var loadAxis = (StructuralAxis)SpeckleStructuralGSA.Helper.Parse0DAxis(gl.AxisIndex.Value, out string _, n.Value.Value.ToArray());
            if (!Helper.IsZeroAxis(loadAxis))
            {
              //Converts from loads on an axis to their global equivalent
              loading.TransformOntoAxis(loadAxis);
            }
          }
          if (loading != null)
          {
            var matching = uniqueLoadings.Where(k => k.Value.SequenceEqual(loading.Value));
            if (matching.Count() > 0)
            {
              uniqueLoadingIndex = uniqueLoadings.IndexOf(matching.First());
            }
            else
            {
              uniqueLoadingIndex = uniqueLoadings.Count();
              uniqueLoadings.Add(loading);
            }
          }

          summaries.Add(new D0LoadingSummary(gl.Index.Value, gl.LoadCaseIndex, n.GSAId, uniqueLoadingIndex));
        }
      }

      var groups = summaries.GroupBy(s => new { s.LoadCaseIndex, s.UniqueLoadingIndex });

      foreach (var group in groups)
      {
        var indices = group.Select(i => i.Index).ToList();
        var nodeIndices = group.Select(i => i.NodeIndex).Where(i => i != null).Distinct().ToList();
        var nodeRefs = nodeIndices.Select(ni => SpeckleStructuralGSA.Helper.GetApplicationId(nodeKeyword, ni.Value)).ToList();
        var loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(loadCaseKeyword, group.First().LoadCaseIndex.Value);
        var applicationId = SpeckleStructuralGSA.Helper.FormatApplicationId(keyword, indices);
        var loading = uniqueLoadings[group.First().UniqueLoadingIndex.Value];
        var obj = new Structural0DLoad() { Loading = loading, NodeRefs = nodeRefs, LoadCaseRef = loadCaseRef, ApplicationId = applicationId };
        structural0DLoads.Add(obj);
      }

      /*
      var gsaLoadsWithoutAppId = gsaLoads.Where(l 
        => l.AxisIndex.HasValue && l.AxisIndex.Value > 0 
        && string.IsNullOrEmpty(l.ApplicationId)).ToList();

      //For each GSA 0D load object, transform the load into the global axis
      var uniqueLoadings = new List<StructuralVectorSix>();
      var globalLoadDict = new Dictionary<int, List<LoadingData>>();  //int is the index of the unique loadings list
      foreach (var gsaLoad in gsaLoadsWithoutAppId)
      {
        var relevantGsaNodes = gsaNodes.Where(n => gsaLoad.NodeIndices.Any(ni => ni == n.GSAId)).ToList();
        foreach (var ln in relevantGsaNodes)
        {
          var loadAxis = SpeckleStructuralGSA.Helper.Parse0DAxis(gsaLoad.AxisIndex.Value, out string _, ln.Value.Value.ToArray());
          var loading = Helper.GsaLoadToLoading(gsaLoad.LoadDirection, gsaLoad.Value.Value);

          if (loading == null || loadAxis == null)
          {
            continue;
          }

          //Converts from loads on an axis to their global equivalent
          loading.TransformOntoAxis(loadAxis);

          var uniqueLoadingIndex = 0;
          if (!uniqueLoadings.Any(k => k.Value.SequenceEqual(loading.Value)))
          {
            uniqueLoadingIndex = uniqueLoadings.Count();
            uniqueLoadings.Add(loading);
          }
          if (!globalLoadDict.ContainsKey(uniqueLoadingIndex))
          {
            globalLoadDict.Add(uniqueLoadingIndex, new List<LoadingData>());
          }
          globalLoadDict[uniqueLoadingIndex].Add(new LoadingData { NodeIndex = ln.GSAId, d0LoadIndex = gsaLoad.Index.Value, LoadCaseIndex = gsaLoad.LoadCaseIndex });
        }
      }

      
      foreach (var k in globalLoadDict.Keys)
      {
        var groupedByLoadCase = globalLoadDict[k].GroupBy(d => d.LoadCaseIndex).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var loadCaseIndex in groupedByLoadCase.Keys)
        {
          var nodeRefs = (loadCaseIndex.HasValue) 
            ? groupedByLoadCase[loadCaseIndex].Where(d => d.NodeIndex.ValidNonZero()).Distinct()
              .Select(d => SpeckleStructuralGSA.Helper.GetApplicationId(nodeKeyword, d.NodeIndex.Value)).ToList()
            : null;
          var loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(loadCaseKeyword, loadCaseIndex.Value);
          var applicationId = SpeckleStructuralGSA.Helper.FormatApplicationId(keyword, groupedByLoadCase[loadCaseIndex].Select(d => d.d0LoadIndex).Distinct());
          var obj = new Structural0DLoad() { Loading = uniqueLoadings[k], NodeRefs = nodeRefs, LoadCaseRef = loadCaseRef, ApplicationId = applicationId };

          structural0DLoads.Add(obj);
        }
      }
      */

      return true;
    }

    private static bool Add0dLoadsWithAppId(string keyword, List<Gsa0dLoad> gsaLoads, List<GSANode> gsaNodes, ref List<Structural0DLoad> structural0DLoads)
    {
      var gsaLoadsWithAppId = gsaLoads.Where(l => !string.IsNullOrEmpty(l.ApplicationId) && l.GlobalAxis).ToList();
      foreach (var gsaLoad in gsaLoadsWithAppId)
      {

      }
      return true;
    }
    #endregion
  }
}
