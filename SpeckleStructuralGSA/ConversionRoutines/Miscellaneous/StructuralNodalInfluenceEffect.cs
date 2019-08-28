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

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSANode> nodes)
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
        obj.Axis = HelperClass.Parse0DAxis(0, Initialiser.Interface, out string temp);
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

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralNodalInfluenceEffect infl = this.Value as StructuralNodalInfluenceEffect;
      
      string keyword = typeof(GSANodalInfluenceEffect).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSANodalInfluenceEffect).GetGSAKeyword(), infl.ApplicationId);

      int? nodeRef = GSA.Indexer.LookupIndex(typeof(GSANode).GetGSAKeyword(), infl.NodeRef);

      if (!nodeRef.HasValue)
        return;

      int axisRef = HelperClass.SetAxis(infl.Axis, infl.Name);

      string[] direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (int i = 0; i < infl.Directions.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + HelperClass.GenerateSID(infl));
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
        Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
      }
    }
  }
  
  public static partial class Conversions
  {
    public static bool ToNative(this StructuralNodalInfluenceEffect infl)
    {
      new GSANodalInfluenceEffect() { Value = infl }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSANodalInfluenceEffect dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSANodalInfluenceEffect)))
        Initialiser.GSASenderObjects[typeof(GSANodalInfluenceEffect)] = new List<object>();

      List<GSANodalInfluenceEffect> infls = new List<GSANodalInfluenceEffect>();
      List<GSANode> nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSANodalInfluenceEffect).GetGSAKeyword();
      string[] subKeywords = typeof(GSANodalInfluenceEffect).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSANodalInfluenceEffect)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSANodalInfluenceEffect)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          GSANodalInfluenceEffect infl = new GSANodalInfluenceEffect() { GWACommand = p };
          infl.ParseGWACommand(Initialiser.Interface, nodes);
          infls.Add(infl);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSANodalInfluenceEffect)].AddRange(infls);

      if (infls.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
