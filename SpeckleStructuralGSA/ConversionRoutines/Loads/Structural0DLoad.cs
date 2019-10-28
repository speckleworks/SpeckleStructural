using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_NODE.2", new string[] { "NODE.2", "AXIS" }, "loads", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DLoad : IGSASpeckleContainer
  {
    public int Axis; // Store this temporarily to generate other loads

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DLoad();

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural0DLoad obj = new Structural0DLoad();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

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

      //obj.LoadCaseRef = Initialiser.Indexer.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      obj.LoadCaseRef = Initialiser.Indexer.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      string axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : Convert.ToInt32(axis);

      obj.Loading = new StructuralVectorSix(new double[6]);
      string direction = pieces[counter++].ToLower();
      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Y":
          obj.Loading.Value[1] = Convert.ToDouble(pieces[counter++]);
          break;
        case "Z":
          obj.Loading.Value[2] = Convert.ToDouble(pieces[counter++]);
          break;
        case "XX":
          obj.Loading.Value[3] = Convert.ToDouble(pieces[counter++]);
          break;
        case "YY":
          obj.Loading.Value[4] = Convert.ToDouble(pieces[counter++]);
          break;
        case "ZZ":
          obj.Loading.Value[5] = Convert.ToDouble(pieces[counter++]);
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }

    public void SetGWACommand()
    {
      if (this.Value == null)
        return;

      Structural0DLoad load = this.Value as Structural0DLoad;

      if (load.Loading == null)
        return;

      string keyword = typeof(GSA0DLoad).GetGSAKeyword();

      var nodeRefs = Initialiser.Indexer.LookupIndices(typeof(GSANode).GetGSAKeyword(), typeof(GSANode).Name, load.NodeRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      int loadCaseRef = 0;
      try
      {
        loadCaseRef = Initialiser.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).Name, load.LoadCaseRef).Value;
      }
      catch {
        loadCaseRef = Initialiser.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).Name, load.LoadCaseRef);
      }

      string[] direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (int i = 0; i < load.Loading.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Indexer.ResolveIndex(typeof(GSA0DLoad).GetGSAKeyword(), typeof(GSA0DLoad).Name);

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        ls.Add(string.Join(" ", nodeRefs));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL"); // Axis
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural0DLoad load)
    {
      new GSA0DLoad() { Value = load }.SetGWACommand();

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA0DLoad dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA0DLoad)))
        Initialiser.GSASenderObjects[typeof(GSA0DLoad)] = new List<object>();

      List<GSA0DLoad> loads = new List<GSA0DLoad>();

      List<GSANode> nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA0DLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSA0DLoad).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSA0DLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSA0DLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        List<GSA0DLoad> loadSubList = new List<GSA0DLoad>();

        // Placeholder load object to get list of nodes and load values
        // Need to transform to axis so one load definition may be transformed to many
        GSA0DLoad initLoad = new GSA0DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(nodes);

        // Raise node flag to make sure it gets sent
        foreach (GSANode n in nodes.Where(n => initLoad.Value.NodeRefs.Contains(n.Value.ApplicationId)))
          n.ForceSend = true;

        // Create load for each node applied
        foreach (string nRef in initLoad.Value.NodeRefs)
        {
          GSA0DLoad load = new GSA0DLoad();
          load.GWACommand = initLoad.GWACommand;
          load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
          load.Value.Name = initLoad.Value.Name;
          load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

          // Transform load to defined axis
          GSANode node = nodes.Where(n => (n.Value.ApplicationId == nRef)).First();
          string gwaRecord = null;
          StructuralAxis loadAxis = HelperClass.Parse0DAxis(initLoad.Axis, Initialiser.Interface, out gwaRecord, node.Value.Value.ToArray());
          load.Value.Loading = initLoad.Value.Loading;
          load.Value.Loading.TransformOntoAxis(loadAxis);

          // If the loading already exists, add node ref to list
          GSA0DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
          if (match != null)
          {
            match.Value.NodeRefs.Add(nRef);
            if (gwaRecord != null)
              match.SubGWACommand.Add(gwaRecord);
          }
          else
          {
            load.Value.NodeRefs = new List<string>() { nRef };
            if (gwaRecord != null)
              load.SubGWACommand.Add(gwaRecord);
            loadSubList.Add(load);
          }
        }

        loads.AddRange(loadSubList);
      }

      Initialiser.GSASenderObjects[typeof(GSA0DLoad)].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
