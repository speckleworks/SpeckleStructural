using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("GEN_REST.2", new string[] { }, "misc", true, true, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) }, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) })]
  public class GSAStructuralStagedNodalRestraints : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralStagedNodalRestraints();

    public void ParseGWACommand(List<GSANode> nodes, List<GSAConstructionStage> stages)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralStagedNodalRestraints();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      
      obj.Name = pieces[counter++];
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      //Restraints
      var restraints = new bool[6];
      for (var i = 0; i < 6; i++)
        restraints[i] = pieces[counter++] == "1";
      obj.Restraint = new StructuralVectorBoolSix(restraints);
      
      var targetNodeRefs = Initialiser.Interface.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.NODE);

      if (nodes != null)
      {
        var targetNodes = nodes
            .Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (var n in targetNodes)
          n.ForceSend = true;
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

      var destinationType = typeof(GSAStructuralStagedNodalRestraints);

			var keyword = destinationType.GetGSAKeyword();
      var subkeywords = destinationType.GetSubGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, obj.ApplicationId);

      var nodesStr = "none"; //default value
      if (obj.NodeRefs != null && obj.NodeRefs.Count() >= 1)
      {
        var nodeIndices = Initialiser.Cache.LookupIndices(typeof(GSANode).GetGSAKeyword(), obj.NodeRefs);
        nodesStr = string.Join(" ", nodeIndices);
      }

      var stageDefStr = "all"; //default value
      if (obj.ConstructionStageRefs != null && obj.ConstructionStageRefs.Count() >= 1)
      {
        var stageDefIndices = Initialiser.Cache.LookupIndices(typeof(GSAConstructionStage).GetGSAKeyword(), obj.ConstructionStageRefs);
        stageDefStr = string.Join(" ", stageDefIndices);
      }

      var ls = new List<string>
      {
        "SET_AT",
        index.ToString(),
        keyword + ":" + Helper.GenerateSID(obj),
				string.IsNullOrEmpty(obj.Name) ? " " : obj.Name
			};

			ls.AddRange(obj.Restraint.Value.Select(v => v ? "1" : "0"));
			ls.Add(nodesStr);
			ls.Add(stageDefStr);

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralStagedNodalRestraints restraint)
    {
      return new GSAStructuralStagedNodalRestraints() { Value = restraint }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAStructuralStagedNodalRestraints dummyObject)
    {
      var newLines = ToSpeckleBase< GSAStructuralStagedNodalRestraints>();

      var nodalRestraintsLock = new object();
      var genNodalRestraints = new List<GSAStructuralStagedNodalRestraints>();

      var stageDefs = Initialiser.GSASenderObjects.Get<GSAConstructionStage>();
      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var genNodalRestraint = new GSAStructuralStagedNodalRestraints() { GSAId = k, GWACommand = newLines[k] };
          genNodalRestraint.ParseGWACommand(nodes, stageDefs);
          lock (nodalRestraintsLock)
          {
            genNodalRestraints.Add(genNodalRestraint);
          }
        }
        catch { }
      });

      Initialiser.GSASenderObjects.AddRange(genNodalRestraints);

      return (genNodalRestraints.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
