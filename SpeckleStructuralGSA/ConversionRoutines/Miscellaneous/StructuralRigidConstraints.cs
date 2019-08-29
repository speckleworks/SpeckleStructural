using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAConversion("RIGID.3", new string[] { }, "misc", true, true, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) }, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) })]
  public class GSARigidConstraints : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralRigidConstraints();

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSANode> nodes, List<GSAConstructionStage> stages)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralRigidConstraints();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      var masterNodeRef = Convert.ToInt32(pieces[counter++]);
      var masterNode = nodes.Where(n => n.GSAId == masterNodeRef);
      if (masterNode.Count() > 0)
      {
        this.SubGWACommand.Add(masterNode.First().GWACommand);
        obj.MasterNodeRef = masterNode.First().Value.ApplicationId;
      }

      var constraint = new bool[6];

      var linkage = pieces[counter++];

      switch(linkage)
      {
        case "ALL":
        case "PIN":
          constraint = new bool[] { true, true, true, true, true, true };
          break;
        case "XY_PLANE":
        case "XY_PLANE_PIN":
          constraint = new bool[] { true, true, false, false, false, true };
          break;
        case "YZ_PLANE":
        case "YZ_PLANE_PIN":
          constraint = new bool[] { false, true, true, true, false, false };
          break;
        case "ZX_PLANE":
        case "ZX_PLANE_PIN":
          constraint = new bool[] { true, false, true, false, true, false };
          break;
        case "XY_PLATE":
        case "XY_PLATE_PIN":
          constraint = new bool[] { false, false, true, true, true, false };
          break;
        case "YZ_PLATE":
        case "YZ_PLATE_PIN":
          constraint = new bool[] { true, false, false, false, true, true };
          break;
        case "ZX_PLATE":
        case "ZX_PLATE_PIN":
          constraint = new bool[] { false, true, false, true, false, true };
          break;
        default:
          // Ignore non-diagonal terms of coupled directionsl.
          // Assume if rotational direction is locked, it is locked for all slave directions.
          constraint[0] = linkage.Contains("X:X");
          constraint[1] = linkage.Contains("Y:Y");
          constraint[2] = linkage.Contains("Z:Z");
          constraint[3] = linkage.Contains("XX:XX");
          constraint[4] = linkage.Contains("YY:YY");
          constraint[5] = linkage.Contains("ZZ:ZZ");
          break;
      }

      obj.Constraint = new StructuralVectorBoolSix(constraint);

      int[] targetNodeRefs = GSA.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.NODE);

      if (nodes != null)
      {
        List<GSANode> targetNodes = nodes
            .Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (GSANode n in targetNodes)
          n.ForceSend = true;
      }

      var gwaStageDefGsaIds = pieces[counter++].ListSplit(" ");
      obj.ConstructionStageRefs = stages.Where(sd => gwaStageDefGsaIds.Any(gwaSDId => gwaSDId == sd.GSAId.ToString())).Select(x => (string)x.Value.ApplicationId).ToList();

      counter++; // Parent member

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      var constraint = this.Value as StructuralRigidConstraints;

      var keyword = typeof(GSARigidConstraints).GetGSAKeyword();

      var index = GSA.Indexer.ResolveIndex(keyword, constraint.ApplicationId);
      
      var slaveNodeIndices = GSA.Indexer.LookupIndices(typeof(GSANode).GetGSAKeyword(), constraint.NodeRefs).Where(x => x.HasValue).Select(x => x.Value.ToString()).ToList();
      var slaveNodeIndicesSummary = slaveNodeIndices.Count > 0 ? string.Join(" ", slaveNodeIndices) : "none";
      var masterNodeIndex = GSA.Indexer.LookupIndex(typeof(GSANode).GetGSAKeyword(), constraint.MasterNodeRef);
      var stageDefRefs = GSA.Indexer.LookupIndices(typeof(GSAConstructionStage).GetGSAKeyword(), constraint.ConstructionStageRefs).Where(x => x.HasValue).Select(x => x.Value.ToString()).ToList();

      var subLs = new List<string>();
      if (constraint.Constraint.Value[0])
      {
        var x = "X:X";
        if (constraint.Constraint.Value[4])
          x += "YY";
        if (constraint.Constraint.Value[5]) 
          x += "ZZ";
        subLs.Add(x);
      }

      if (constraint.Constraint.Value[1])
      {
        var y = "Y:Y";
        if (constraint.Constraint.Value[3])
          y += "XX";
        if (constraint.Constraint.Value[5])
          y += "ZZ";
        subLs.Add(y);
      }

      if (constraint.Constraint.Value[2])
      {
        var z = "Z:Z";
        if (constraint.Constraint.Value[3])
          z += "XX";
        if (constraint.Constraint.Value[4])
          z += "YY";
        subLs.Add(z);
      }

      if (constraint.Constraint.Value[3])
        subLs.Add("XX:XX");

      if (constraint.Constraint.Value[4])
        subLs.Add("YY:YY");

      if (constraint.Constraint.Value[5])
        subLs.Add("ZZ:ZZ");

      var ls = new List<string>
      {
        "SET_AT",
        index.ToString(),
        keyword + ":" + HelperClass.GenerateSID(constraint),
        constraint.Name == null || constraint.Name == "" ? " " : constraint.Name,
        (masterNodeIndex.HasValue) ? masterNodeIndex.Value.ToString() : "0", // Master node
        string.Join("-", subLs),
        string.Join(" ", slaveNodeIndices),
        string.Join(" ", stageDefRefs),
        "0" // Parent member
      };

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralRigidConstraints constraint)
    {
      new GSARigidConstraints() { Value = constraint }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSARigidConstraints dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSARigidConstraints)))
        Initialiser.GSASenderObjects[typeof(GSARigidConstraints)] = new List<object>();

      List<GSARigidConstraints> constraints = new List<GSARigidConstraints>();
      List<GSANode> nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();
      List<GSAConstructionStage> stages = Initialiser.GSASenderObjects[typeof(GSAConstructionStage)].Cast<GSAConstructionStage>().ToList();

      string keyword = typeof(GSARigidConstraints).GetGSAKeyword();
      string[] subKeywords = typeof(GSARigidConstraints).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSARigidConstraints)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSARigidConstraints)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          GSARigidConstraints constraint = new GSARigidConstraints() { GWACommand = p };
          constraint.ParseGWACommand(Initialiser.Interface, nodes, stages);
          constraints.Add(constraint);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSARigidConstraints)].AddRange(constraints);

      if (constraints.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
