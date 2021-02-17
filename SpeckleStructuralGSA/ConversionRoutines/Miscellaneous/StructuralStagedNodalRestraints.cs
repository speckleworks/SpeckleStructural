using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GEN_REST.2", new string[] { }, "model", true, true, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) }, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) })]
  public class GSAStagedNodalRestraints : GSABase<StructuralStagedNodalRestraints>
  {
    public void ParseGWACommand(List<GSANode> nodes, List<GSAConstructionStage> stages)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralStagedNodalRestraints();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      
      obj.Name = pieces[counter++];
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      //Restraints
      var restraints = new bool[6];
      for (var i = 0; i < 6; i++)
      {
        restraints[i] = pieces[counter++] == "1";
      }
      obj.Restraint = new StructuralVectorBoolSix(restraints);
      
      var targetNodeRefs = Initialiser.AppResources.Proxy.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.NODE);

      if (nodes != null)
      {
        var targetNodes = nodes.Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).Distinct().OrderBy(i => i).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (var n in targetNodes)
        {
          n.ForceSend = true;
        }
      }
      
      var gwaStageDefGsaIds = pieces[counter++].ListSplit(" ");
      obj.ConstructionStageRefs = stages.Where(sd => gwaStageDefGsaIds.Any(gwaSDId => gwaSDId == sd.GSAId.ToString())).Select(x => (string)x.Value.ApplicationId).ToList();

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var obj = this.Value as StructuralStagedNodalRestraints;
      if (obj.Restraint == null)
        return "";

      var destinationType = typeof(GSAStagedNodalRestraints);

			var keyword = destinationType.GetGSAKeyword();
      var subkeywords = destinationType.GetSubGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, obj.ApplicationId);

      var nodesStr = "none"; //default value
      if (obj.NodeRefs != null && obj.NodeRefs.Count() >= 1)
      {
        var nodeIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSANode).GetGSAKeyword(), obj.NodeRefs).Distinct().OrderBy(i => i);
        nodesStr = string.Join(" ", nodeIndices);
      }

      var stageDefStr = "all"; //default value
      if (obj.ConstructionStageRefs != null && obj.ConstructionStageRefs.Count() >= 1)
      {
        var stageDefIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSAConstructionStage).GetGSAKeyword(), obj.ConstructionStageRefs).Distinct().OrderBy(i => i);
        stageDefStr = string.Join(" ", stageDefIndices);
      }

      var sid = Helper.GenerateSID(obj);
      var ls = new List<string>
      {
        "SET_AT",
        index.ToString(),
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
				string.IsNullOrEmpty(obj.Name) ? " " : obj.Name
			};

			ls.AddRange(obj.Restraint.Value.Select(v => v ? "1" : "0"));
			ls.Add(nodesStr);
			ls.Add(stageDefStr);

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralStagedNodalRestraints restraint)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(restraint, () => new GSAStagedNodalRestraints() { Value = restraint }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSAStagedNodalRestraints dummyObject)
    {
      var newLines = ToSpeckleBase< GSAStagedNodalRestraints>();
      var typeName = dummyObject.GetType().Name;
      var nodalRestraintsLock = new object();
      var genNodalRestraints = new SortedDictionary<int, GSAStagedNodalRestraints>();
      var keyword = dummyObject.GetGSAKeyword();

      var stageDefs = Initialiser.GsaKit.GSASenderObjects.Get<GSAConstructionStage>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var genNodalRestraint = new GSAStagedNodalRestraints() { GSAId = k, GWACommand = newLines[k] };
          genNodalRestraint.ParseGWACommand(nodes, stageDefs);
          lock (nodalRestraintsLock)
          {
            genNodalRestraints.Add(k, genNodalRestraint);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(genNodalRestraints.Values.ToList());

      return (genNodalRestraints.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
