using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaLoadNodeToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaLoadNode dummyObject)
    {
      var typeName = dummyObject.GetType().Name;
      var gsaLoads = Helper.GetNewFromCache<GSA0DLoad, GsaLoadNode>();
      if (gsaLoads.Count() == 0)
      {
        return new SpeckleObject();
      }
      gsaLoads = gsaLoads.Where(l => l.Index.ValidNonZero()).ToList();

      var keyword = GsaRecord.GetKeyword<GsaLoadNode>();
      var nodeKeyword = GsaRecord.GetKeyword<GsaNode>();
      var loadCaseKeyword = GsaRecord.GetKeyword<GsaLoadCase>();

      //The 0D loads are split into two groups:
      //1. Those which are grouped by Application ID - n:1 ratio (where n <= 6) between GSA objects and Speckle objects
      //2. Those which are sent out individually - 1:1 ratio between GSA objects and Speckle objects
      //To avoid complication regarding merging with existing objects: if a 0D load was previously received from Speckle (i.e. it has an application ID)
      //and it was manually changed from GLOBAL to referencing an axis, then ignore the application ID when sending out (i.e. lump it with group #2)
      var group1 = gsaLoads.Where(l => !string.IsNullOrEmpty(l.ApplicationId) && l.GlobalAxis);
      var group2 = gsaLoads.Except(group1);
      var nodeIndicesReferenced = new List<int>();

      var structural0DLoads = new List<Structural0DLoad>();
      var gsaNodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      Add0dLoadsWithAppId(nodeKeyword, loadCaseKeyword, group1, gsaNodes, ref structural0DLoads, ref nodeIndicesReferenced);
      Add0dLoadsWithoutAppId(keyword, nodeKeyword, loadCaseKeyword, group2, gsaNodes, ref structural0DLoads, ref nodeIndicesReferenced);

      var forceSendNodes = gsaNodes.Where(n => nodeIndicesReferenced.Any(nir => nir == n.GSAId)).ToList();
      foreach (var fsn in forceSendNodes)
      {
        fsn.ForceSend = true;
      }

      var loads = structural0DLoads.Select(sl => new GSA0DLoad() { Value = sl }).ToList();

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);
      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    #region 0dLoad_methods

    private static bool Add0dLoadsWithoutAppId(string keyword, string nodeKeyword, string loadCaseKeyword, IEnumerable<GsaLoadNode> gsaLoads,
      List<GSANode> gsaNodes, ref List<Structural0DLoad> structural0DLoads, ref List<int> nodeIndicesReferenced)
    {
      var summaries = new List<D0LoadingSummary>();
      var uniqueLoadings = new List<StructuralVectorSix>();

      foreach (var gl in gsaLoads.Where(l => l.Value != null))
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

          summaries.Add(new D0LoadingSummary(gl.Index.Value, gl.LoadCaseIndex, n.GSAId) { UniqueLoadingIndex = uniqueLoadingIndex, Name = gl.Name });
        }
      }

      var groups = summaries.GroupBy(s => new { s.LoadCaseIndex, s.UniqueLoadingIndex });
      
      var allNodeIndices = nodeIndicesReferenced.ToList();

      foreach (var group in groups)
      {
        var indices = group.Select(i => i.Index).Distinct().ToList();
        var nodeIndices = group.Select(i => i.NodeIndex).Where(i => i != null).Distinct().ToList();
        allNodeIndices.AddRange(nodeIndices.Where(ni => ni.HasValue && !allNodeIndices.Contains(ni.Value)).Select(ni => ni.Value));
        var nodeRefs = nodeIndices.Select(ni => SpeckleStructuralGSA.Helper.GetApplicationId(nodeKeyword, ni.Value)).OrderBy(r => r).ToList();
        var loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(loadCaseKeyword, group.First().LoadCaseIndex.Value);
        var applicationId = SpeckleStructuralGSA.Helper.FormatApplicationId(keyword, indices);
        var name = string.Join("-", group.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name));
        var loading = uniqueLoadings[group.First().UniqueLoadingIndex.Value];
        var obj = new Structural0DLoad() { ApplicationId = applicationId, Loading = loading, NodeRefs = nodeRefs, LoadCaseRef = loadCaseRef };
        if (!string.IsNullOrEmpty(name))
        {
          obj.Name = name;
        }
        Helper.AddCustomStructuralProperty(obj, indices.Count() == 1 ? "NativeId" : "NativeIds", 
          indices.Count() == 1 ? (object)indices.First().ToString() : indices.Select(i => i.ToString()).ToList());
        structural0DLoads.Add(obj);
      }

      nodeIndicesReferenced = allNodeIndices;
      return true;
    }

    private static bool Add0dLoadsWithAppId(string nodeKeyword, string loadCaseKeyword, IEnumerable<GsaLoadNode> gsaLoads,
      List<GSANode> gsaNodes, ref List<Structural0DLoad> structural0DLoads, ref List<int> nodeIndicesReferenced)
    {
      var summaries = new List<D0LoadingSummary>();

      //All loads here have an application ID and axis is set to GLOBAL
      foreach (var gl in gsaLoads)
      {
        //Assume an underscore is the end of the original Application ID of the Speckle object that created up to 6 rows of 0D loads
        var appId = gl.ApplicationId.Substring(0, gl.ApplicationId.IndexOf("_"));
        var relevantGsaNodes = gsaNodes.Where(n => gl.NodeIndices.Any(ni => ni == n.GSAId)).ToList();
        foreach (var n in relevantGsaNodes)
        {
          summaries.Add(new D0LoadingSummary(gl.Index.Value, gl.LoadCaseIndex, n.GSAId) { ApplicationId = appId });
        }
      }

      var groups = summaries.GroupBy(s => new { s.LoadCaseIndex, s.ApplicationId });
      
      var allNodeIndices = nodeIndicesReferenced.ToList();

      foreach (var group in groups)
      {
        var indices = group.Select(i => i.Index).Distinct().ToList();
        var nodeIndices = group.Select(i => i.NodeIndex).Where(i => i != null).Distinct().ToList();
        allNodeIndices.AddRange(nodeIndices.Where(ni => ni.HasValue && !allNodeIndices.Contains(ni.Value)).Select(ni => ni.Value));
        var nodeRefs = nodeIndices.Select(ni => SpeckleStructuralGSA.Helper.GetApplicationId(nodeKeyword, ni.Value)).OrderBy(r => r).ToList();
        var loadCaseRef = SpeckleStructuralGSA.Helper.GetApplicationId(loadCaseKeyword, group.First().LoadCaseIndex.Value);
        var applicationId = group.First().ApplicationId;
        var name = string.Join("-", group.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name));
        var loadings = indices.Select(i => gsaLoads.First(l => l.Index == i)).Select(gl => Helper.GsaLoadToLoading(gl.LoadDirection, gl.Value.Value)).ToList();
        var combinedLoading = new StructuralVectorSix(Enumerable.Range(0, 6).Select(i => loadings.Sum(l => l.Value[i])));
        var obj = new Structural0DLoad() { ApplicationId = applicationId, Loading = combinedLoading, NodeRefs = nodeRefs, LoadCaseRef = loadCaseRef };
        if (!string.IsNullOrEmpty(name))
        {
          obj.Name = name;
        }
        Helper.AddCustomStructuralProperty(obj, indices.Count() == 1 ? "NativeId" : "NativeIds",
          indices.Count() == 1 ? (object)indices.First().ToString() : indices.Select(i => i.ToString()).ToList());
        structural0DLoads.Add(obj);
      }

      nodeIndicesReferenced = allNodeIndices;

      return true;
    }

    //This is a class used as a container type used in queries to group data into groups which will map to Gsa0dLoad objects
    //D0__ because class names can't start with numbers ..
    private class D0LoadingSummary
    {
      public int Index;
      public int? LoadCaseIndex;
      public int? NodeIndex;
      public int? UniqueLoadingIndex;
      public string Name;
      public string ApplicationId;

      public D0LoadingSummary(int index, int? loadCaseIndex, int? nodeIndex)
      {
        Index = index;
        LoadCaseIndex = loadCaseIndex;
        NodeIndex = nodeIndex;
      }
    }
    #endregion
  }
}
