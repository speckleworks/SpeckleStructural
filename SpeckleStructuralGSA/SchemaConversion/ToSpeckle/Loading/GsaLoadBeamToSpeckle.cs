using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class GsaLoadBeamToSpeckle
  {
    public static SpeckleObject ToSpeckle(this GsaLoadBeam dummyObject)
    {
      var newLines = Initialiser.AppResources.Cache.GetGwaToSerialise(dummyObject.Keyword);
      //This will return ALL load beams, which (in the future) may not be UDLs
      //Also in the future, the cache will return Gsa schema objects, not just GWA that needs to be converted into Gsa schema objects
      var allGsaLoadBeams = GwaToGsaLoadBeams(newLines);
      allGsaLoadBeams = allGsaLoadBeams.Where(l => l.Index.ValidNonZero()).ToList();
      var gsaLoadsByType = allGsaLoadBeams.GroupBy(l => l.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      var keyword = GsaRecord.GetKeyword<GsaLoadBeamUdl>();
      var axisKeyword = GsaRecord.GetKeyword<GsaAxis>();
      var loadCaseKeyword = GsaRecord.GetKeyword<GsaLoadCase>();
      var entityKeyword = (Initialiser.AppResources.Settings.TargetLayer == SpeckleGSAInterfaces.GSATargetLayer.Design) ? GsaRecord.GetKeyword<GsaMemb>() : GsaRecord.GetKeyword<GsaEl>();

      //Just to UDL for now
      var gsaLoads = gsaLoadsByType[typeof(GsaLoadBeamUdl)].Cast<GsaLoadBeamUdl>().ToList();

      var structural1DLoads = new List<Structural1DLoad>();


      //The 1D loads are split into two groups:
      //1. Those which are grouped by Application ID - n:1 ratio (where n <= 6) between GSA objects and Speckle objects
      //2. Those which are sent out individually - 1:1 ratio between GSA objects and Speckle objects
      //To avoid complication regarding merging with existing objects: if a 1D load was previously received from Speckle (i.e. it has an application ID)
      //and it was manually changed from GLOBAL to referencing an axis, then ignore the application ID when sending out (i.e. lump it with group #2)
      var group1 = gsaLoads.Where(l => !string.IsNullOrEmpty(l.ApplicationId) && l.AxisRefType == LoadBeamAxisRefType.Global);
      var group2 = gsaLoads.Except(group1);

      Add1dLoadsWithAppId(entityKeyword, loadCaseKeyword, group1, ref structural1DLoads);
      Add1dLoadsWithoutAppId(keyword, axisKeyword, entityKeyword, loadCaseKeyword, group2, ref structural1DLoads);

      var loads = structural1DLoads.Select(sl => new GSA1DLoad() { Value = sl }).Cast<GSA1DLoad>().ToList();

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);
      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    private static bool Add1dLoadsWithAppId(string entityKeyword, string loadCaseKeyword, IEnumerable<GsaLoadBeamUdl> gsaLoads, ref List<Structural1DLoad> structural1DLoads)
    {
      var gsaGroups = gsaLoads.GroupBy(gl => new { ApplicationId = gl.ApplicationId.Substring(0, gl.ApplicationId.IndexOf("_")), gl.LoadCaseIndex });
      foreach (var group in gsaGroups)
      {
        var gList = group.ToList();

        var applicationId = gList[0].ApplicationId.Substring(0, gList[0].ApplicationId.IndexOf("_"));
        var name = (gList.Any(gl => !string.IsNullOrEmpty(gl.Name))) ? gList.FirstOrDefault(gl => !string.IsNullOrEmpty(gl.Name)).Name : "";
        
        //Assume the element refs are the same for those with application IDs - so just take the indices of the first record and resolve them to application IDs for entities
        var elementRefs = gList[0].Entities.Select(ei => Initialiser.AppResources.Cache.GetApplicationId(entityKeyword, ei)).Where(aid => !string.IsNullOrEmpty(aid)).ToList();
        var loadCaseRef = Initialiser.AppResources.Cache.GetApplicationId(loadCaseKeyword, gList[0].LoadCaseIndex.Value);

        var loadings = gList.Select(gl => Helper.GsaLoadToLoading(gl.LoadDirection, gl.Load.Value)).ToList();
        var combinedLoading = new StructuralVectorSix(Enumerable.Range(0, 6).Select(i => loadings.Sum(l => l.Value[i])));

        structural1DLoads.Add(new Structural1DLoad()
        {
          ApplicationId = applicationId,
          Name = name,
          ElementRefs = elementRefs,
          LoadCaseRef = loadCaseRef,
          Loading = combinedLoading
        });
      }
      return true; 
    }

    private static bool Add1dLoadsWithoutAppId(string keyword, string axisKeyword, string entityKeyword, string loadCaseKeyword, IEnumerable<GsaLoadBeamUdl> gsaLoads, ref List<Structural1DLoad> structural1DLoads)
    {
      var gsaGroups = gsaLoads.GroupBy(gl => gl.LoadCaseIndex).Select(g => g.OfType<GsaLoadBeamUdl>().ToList()).ToList();
      foreach (var gsaGroup in gsaGroups)
      {
        //This is the group which might have axes, so if this is true, then they need to be transformed
        var transformedGsaLoads = new List<StructuralVectorSix>();
        var uniqueLoadings = new List<StructuralVectorSix>();
        var uniqueToLoadingsList = new Dictionary<int, List<int>>();

        var glByIndex = gsaGroup.Where(gl => gl.Index.HasValue).ToDictionary(gl => gl.Index.Value, gl => gl);

        foreach (var gl in gsaGroup)
        {
          if (!gl.Index.HasValue || gl.Index == 0) continue;

          var loading = Helper.GsaLoadToLoading(gl.LoadDirection, gl.Load.Value);
          if (gl.AxisRefType == LoadBeamAxisRefType.Reference && gl.AxisIndex.HasValue && gl.AxisIndex.Value > 0)
          {

            var axisGwa = Initialiser.AppResources.Cache.GetGwa(axisKeyword, gl.AxisIndex.Value);
            if (axisGwa != null && axisGwa.Count() > 0 && string.IsNullOrEmpty(axisGwa.First()))
            {
              return false;
            }
            var gsaAxis = new GsaAxis();
            gsaAxis.FromGwa(axisGwa.First());
            var loadAxis = (StructuralAxis)gsaAxis.ToSpeckle();
            //Converts from loads on an axis to their global equivalent
            loading.TransformOntoAxis(loadAxis);
          }

          int uniqueLoadingIndex;
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
            if (!uniqueToLoadingsList.ContainsKey(uniqueLoadingIndex))
            {
              uniqueToLoadingsList.Add(uniqueLoadingIndex, new List<int>());
            }
            uniqueToLoadingsList[uniqueLoadingIndex].Add(gl.Index.Value);
          }
        }

        foreach (var ul in uniqueToLoadingsList.Keys)
        {
          var entityIndices = uniqueToLoadingsList[ul].SelectMany(ei => glByIndex[ei].Entities).Distinct().OrderBy(n => n).ToList();
          var elementRefs = entityIndices.Select(ei => Initialiser.AppResources.Cache.GetApplicationId(entityKeyword, ei)).Where(aid => !string.IsNullOrEmpty(aid)).ToList();
          var loadCaseRef = (gsaGroup.First().LoadCaseIndex.HasValue) ? Initialiser.AppResources.Cache.GetApplicationId(loadCaseKeyword, gsaGroup.First().LoadCaseIndex.Value) : null;
          structural1DLoads.Add(new Structural1DLoad()
          {
            ApplicationId = SpeckleStructuralGSA.Helper.FormatApplicationId(keyword, uniqueToLoadingsList[ul]),
            Name = gsaGroup.First().Name,
            ElementRefs = elementRefs,
            LoadCaseRef = loadCaseRef,
            Loading = uniqueLoadings[ul]
          });
        }
      }

      return true;
    }

    private static List<GsaLoadBeam> GwaToGsaLoadBeams(Dictionary<int, string> newLines)
    {
      var instanceInfo = new Dictionary<GwaKeyword, Type>() {
        { GwaKeyword.LOAD_BEAM_UDL, typeof(GsaLoadBeamUdl) },
        { GwaKeyword.LOAD_BEAM_LINE, typeof(GsaLoadBeamLine) },
        { GwaKeyword.LOAD_BEAM_POINT, typeof(GsaLoadBeamPoint) },
        { GwaKeyword.LOAD_BEAM_PATCH, typeof(GsaLoadBeamPatch) },
        { GwaKeyword.LOAD_BEAM_TRILIN, typeof(GsaLoadBeamPatchTrilin) }
      };

      var schemaObjs = new List<GsaLoadBeam>();

      foreach (var index in newLines.Keys)
      {
        GsaLoadBeam obj = null;
        foreach (var kw in instanceInfo.Keys)
        {
          if (newLines[index].Contains(kw.ToString()))
          {
            obj = (GsaLoadBeam)Activator.CreateInstance(instanceInfo[kw]);
            break;
          }
        }
        if (obj != null)
        {
          obj.Index = index;

          if (obj.FromGwa(newLines[index]))
          {
            schemaObjs.Add(obj);
          }
        }
      }
      return schemaObjs;
    }

    private class D1LoadingSummary
    {
      public int Index;
      public int? LoadCaseIndex;
      public List<int> EntityIndices;
      /*
      public int? UniqueLoadingIndex;
      public string Name;
      public string ApplicationId;
      */

      public D1LoadingSummary(int index, int? loadCaseIndex, List<int> entityIndices)
      {
        this.Index = index;
        this.LoadCaseIndex = loadCaseIndex;
        this.EntityIndices = entityIndices;
      }
    }
  }
}
