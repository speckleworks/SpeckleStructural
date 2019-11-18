using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("INF_NODE.1", new string[] { }, "misc", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSANodalInfluenceEffect : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNodalInfluenceEffect();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralNodalInfluenceEffect();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      obj.GSAEffectGroup = Convert.ToInt32(pieces[counter++]);

      var targetNodeRef = pieces[counter++];

      GSANode targetNode;

      if (nodes != null)
      {
        targetNode = nodes.Where(n => targetNodeRef == n.GSAId.ToString()).FirstOrDefault();

        obj.NodeRef = targetNode.Value.ApplicationId;

        this.SubGWACommand.Add(targetNode.GWACommand);

        targetNode.ForceSend = true;
      }
      else
        return;

      obj.Factor = Convert.ToDouble(pieces[counter++]);
      var effectType = pieces[counter++];
      switch(effectType)
      {
        case "DISP":
          obj.EffectType = StructuralInfluenceEffectType.Displacement;
          break;
        case "FORCE":
          obj.EffectType = StructuralInfluenceEffectType.Force;
          break;
        default:
          return;
      }

      var axis = pieces[counter++];
      if (axis == "GLOBAL")
      {
        obj.Axis = HelperClass.Parse0DAxis(0, Initialiser.Interface, out var temp);
      }
      else if (axis == "LOCAL")
      {
        obj.Axis = targetNode.Value.Axis;
      }
      else
      {
        obj.Axis = HelperClass.Parse0DAxis(Convert.ToInt32(axis), Initialiser.Interface, out string rec, targetNode.Value.Value.ToArray());
        if (rec != null)
          this.SubGWACommand.Add(rec);
      }

      var dir = pieces[counter++];
      obj.Directions = new StructuralVectorBoolSix(new bool[6]);
      switch(dir.ToLower())
      {
        case "x":
          obj.Directions.Value[0] = true;
          break;
        case "y":
          obj.Directions.Value[1] = true;
          break;
        case "z":
          obj.Directions.Value[2] = true;
          break;
        case "xx":
          obj.Directions.Value[3] = true;
          break;
        case "yy":
          obj.Directions.Value[4] = true;
          break;
        case "zz":
          obj.Directions.Value[5] = true;
          break;
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var infl = this.Value as StructuralNodalInfluenceEffect;
      
      var keyword = typeof(GSANodalInfluenceEffect).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSANodalInfluenceEffect).GetGSAKeyword(), infl.ApplicationId);

      var nodeRef = Initialiser.Cache.LookupIndex(typeof(GSANode).GetGSAKeyword(), infl.NodeRef);

      if (!nodeRef.HasValue)
        return "";

      var gwaCommands = new List<string>();

      HelperClass.SetAxis(infl.Axis, out var axisIndex, out var axisGwa, infl.Name);
      if (axisGwa.Length > 0)
      {
        gwaCommands.Add(axisGwa);
      }

      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (var i = 0; i < infl.Directions.Value.Count(); i++)
      {
        var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + ":" + HelperClass.GenerateSID(infl),
          infl.Name == null || infl.Name == "" ? " " : infl.Name,
          infl.GSAEffectGroup.ToString(),
          nodeRef.Value.ToString(),
          infl.Factor.ToString()
        };
        switch (infl.EffectType)
        {
          case StructuralInfluenceEffectType.Force:
            ls.Add("FORCE");
            break;
          case StructuralInfluenceEffectType.Displacement:
            ls.Add("DISP");
            break;
          default:
            return "";
        }
        ls.Add(axisIndex.ToString());
        ls.Add(direction[i]);
        gwaCommands.Add(string.Join("\t", ls));
      }
      return string.Join("\n", gwaCommands);
    }
  }
  
  public static partial class Conversions
  {
    public static string ToNative(this StructuralNodalInfluenceEffect infl)
    {
      return new GSANodalInfluenceEffect() { Value = infl }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSANodalInfluenceEffect dummyObject)
    {
      var newLines = ToSpeckleBase<GSANodalInfluenceEffect>();

      var infls = new List<GSANodalInfluenceEffect>();
      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      foreach (var p in newLines.Values)
      {
        try
        {
          var infl = new GSANodalInfluenceEffect() { GWACommand = p };
          infl.ParseGWACommand(nodes);
          infls.Add(infl);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSANodalInfluenceEffect)].AddRange(infls);

      return (infls.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
