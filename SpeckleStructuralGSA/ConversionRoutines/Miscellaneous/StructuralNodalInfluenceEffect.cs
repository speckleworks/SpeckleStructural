using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;
using SQLite;

namespace SpeckleStructuralGSA
{
  [GSAObject("INF_NODE.1", new string[] { }, "misc", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSANodalInfluenceEffect : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNodalInfluenceEffect();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      StructuralNodalInfluenceEffect obj = new StructuralNodalInfluenceEffect();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
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
        obj.Axis = GSA.Parse0DAxis(0, out string temp);
      }
      else if (axis == "LOCAL")
      {
        obj.Axis = targetNode.Value.Axis;
      }
      else
      {
        obj.Axis = GSA.Parse0DAxis(Convert.ToInt32(axis), out string rec, targetNode.Value.Value.ToArray());
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

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralNodalInfluenceEffect infl = this.Value as StructuralNodalInfluenceEffect;
      
      string keyword = typeof(GSANodalInfluenceEffect).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSANodalInfluenceEffect), infl);

      int? nodeRef = GSA.Indexer.LookupIndex(typeof(GSANode), infl.NodeRef);

      if (!nodeRef.HasValue)
        return;

      int axisRef = GSA.SetAxis(infl.Axis);

      string[] direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (int i = 0; i < infl.Directions.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + GSA.GenerateSID(infl));
        ls.Add(infl.Name == null || infl.Name == "" ? " " : infl.Name);
        ls.Add(infl.GSAEffectGroup.ToString());
        ls.Add(nodeRef.Value.ToString());
        ls.Add(infl.Factor.ToString());
        switch (infl.EffectType)
        {
          case StructuralInfluenceEffectType.Force:
            ls.Add("FORCE");
            break;
          case StructuralInfluenceEffectType.Displacement:
            ls.Add("DISP");
            break;
          default:
            return;
        }
        ls.Add(axisRef.ToString());
        ls.Add(direction[i]);
        GSA.RunGWACommand(string.Join("\t", ls));
      }
    }
  }
  
  public static partial class Conversions
  {
    public static bool ToNative(this StructuralNodalInfluenceEffect infl)
    {
      new GSANodalInfluenceEffect() { Value = infl }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSANodalInfluenceEffect dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSANodalInfluenceEffect)))
        GSASenderObjects[typeof(GSANodalInfluenceEffect)] = new List<object>();

      List<GSANodalInfluenceEffect> infls = new List<GSANodalInfluenceEffect>();
      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSANodalInfluenceEffect).GetGSAKeyword();
      string[] subKeywords = typeof(GSANodalInfluenceEffect).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSANodalInfluenceEffect)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSANodalInfluenceEffect)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSANodalInfluenceEffect infl = new GSANodalInfluenceEffect() { GWACommand = p };
        infl.ParseGWACommand(GSA, nodes);
        infls.Add(infl);
      }

      GSASenderObjects[typeof(GSANodalInfluenceEffect)].AddRange(infls);

      if (infls.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
