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
  [GSAObject("INF_NODE.1", new string[] { }, "model", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSANodalInfluenceEffect : GSABase<StructuralNodalInfluenceEffect>
  {
    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralNodalInfluenceEffect();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

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
        obj.Axis = Helper.Parse0DAxis(0, out var temp);
      }
      else if (axis == "LOCAL")
      {
        obj.Axis = targetNode.Value.Axis;
      }
      else
      {
        obj.Axis = Helper.Parse0DAxis(Convert.ToInt32(axis), out string rec, targetNode.Value.Value.ToArray());
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

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSANodalInfluenceEffect).GetGSAKeyword(), infl.ApplicationId);

      var nodeRef = Initialiser.AppResources.Cache.LookupIndex(typeof(GSANode).GetGSAKeyword(), infl.NodeRef);

      if (!nodeRef.HasValue)
        return "";

      var gwaCommands = new List<string>();

      Helper.SetAxis(infl.Axis, out var axisIndex, out var axisGwa, infl.Name);
      if (axisGwa.Length > 0)
      {
        gwaCommands.Add(axisGwa);
      }

      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      //This will cause multiple GWA lines to have the same application Id - might need a review
      var sid = Helper.GenerateSID(infl);
      for (var i = 0; i < infl.Directions.Value.Count(); i++)
      {
        var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
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
        gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
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
      var typeName = dummyObject.GetType().Name;
      var inflsLock = new object();
      var infls = new SortedDictionary<int, GSANodalInfluenceEffect>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var p = newLines[k];
          var infl = new GSANodalInfluenceEffect() { GWACommand = p };
          infl.ParseGWACommand(nodes);
          lock (inflsLock)
          {
            infls.Add(k, infl);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(infls.Values.ToList());

      return (infls.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
