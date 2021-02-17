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
  [GSAObject("RIGID.3", new string[] { }, "model", true, true, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) }, new Type[] { typeof(GSANode), typeof(GSAConstructionStage) })]
  public class GSARigidConstraints : GSABase<StructuralRigidConstraints>
  {
    public void ParseGWACommand(List<GSANode> nodes, List<GSAConstructionStage> stages)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralRigidConstraints();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
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

      var targetNodeRefs = Initialiser.AppResources.Proxy.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.NODE);

      if (nodes != null)
      {
        var targetNodes = nodes
            .Where(n => targetNodeRefs.Contains(n.GSAId)).ToList();

        obj.NodeRefs = targetNodes.Select(n => (string)n.Value.ApplicationId).OrderBy(i => i).ToList();
        this.SubGWACommand.AddRange(targetNodes.Select(n => n.GWACommand));

        foreach (var n in targetNodes)
          n.ForceSend = true;
      }

      var gwaStageDefGsaIds = pieces[counter++].ListSplit(" ");
      obj.ConstructionStageRefs = stages.Where(sd => gwaStageDefGsaIds.Any(gwaSDId => gwaSDId == sd.GSAId.ToString())).Select(x => (string)x.Value.ApplicationId).ToList();

      counter++; // Parent member

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var constraint = this.Value as StructuralRigidConstraints;
      if (constraint.Constraint == null)
        return "";

      var keyword = typeof(GSARigidConstraints).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, constraint.ApplicationId);
      
      var slaveNodeIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSANode).GetGSAKeyword(), constraint.NodeRefs).Where(x => x.HasValue)
        .Distinct().OrderBy(i => i).Select(x => x.Value.ToString()).ToList();
      var slaveNodeIndicesSummary = slaveNodeIndices.Count > 0 ? string.Join(" ", slaveNodeIndices) : "none";
      var masterNodeIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSANode).GetGSAKeyword(), constraint.MasterNodeRef);
      var stageDefRefs = Initialiser.AppResources.Cache.LookupIndices(typeof(GSAConstructionStage).GetGSAKeyword(), constraint.ConstructionStageRefs)
        .Where(x => x.HasValue).Distinct().Select(x => x.Value.ToString()).ToList();

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
        keyword + ":" + Helper.GenerateSID(constraint),
        constraint.Name == null || constraint.Name == "" ? " " : constraint.Name,
        (masterNodeIndex.HasValue) ? masterNodeIndex.Value.ToString() : "0", // Master node
        string.Join("-", subLs),
        string.Join(" ", slaveNodeIndices),
        string.Join(" ", stageDefRefs),
        "0" // Parent member
      };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralRigidConstraints constraint)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(constraint, () => new GSARigidConstraints() { Value = constraint }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSARigidConstraints dummyObject)
    {
      var newLines = ToSpeckleBase<GSARigidConstraints>();
      var typeName = dummyObject.GetType().Name;
      var constraintsLock = new object();
      var constraints = new SortedDictionary<int, GSARigidConstraints>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var stages = Initialiser.GsaKit.GSASenderObjects.Get<GSAConstructionStage>();
      var keyword = dummyObject.GetGSAKeyword();

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var constraint = new GSARigidConstraints() { GSAId = k, GWACommand = newLines[k] };
          constraint.ParseGWACommand(nodes, stages);
          lock (constraintsLock)
          {
            constraints.Add(k, constraint);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(constraints.Values.ToList());

      return (constraints.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
