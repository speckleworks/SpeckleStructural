using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("NODE.2", new string[] { "AXIS" }, "nodes", true, true, new Type[] { }, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember), typeof(GSA2DElement), typeof(GSA2DMember) })]
  public class GSANode : IGSASpeckleContainer
  {
    public bool ForceSend; // This is to filter only "important" nodes

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNode();

    public void ParseGWACommand(GSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      StructuralNode obj = new StructuralNode();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
      obj.Value = new List<double>();
      obj.Value.Add(Convert.ToDouble(pieces[counter++]));
      obj.Value.Add(Convert.ToDouble(pieces[counter++]));
      obj.Value.Add(Convert.ToDouble(pieces[counter++]));

      //counter += 3; // TODO: Skip unknown fields in NODE.3

      while (counter < pieces.Length)
      {
        string s = pieces[counter++];

        switch (s)
        {
          case "NO_GRID":
          case "NO_REST":
          case "NO_MESH":
            continue;
          case "GRID":
            counter++; // Grid place
            counter++; // Datum
            counter++; // Grid line A
            counter++; // Grid line B
            break;
          case "REST":
            obj.Restraint = new StructuralVectorBoolSix(new bool[6]);
            for (int i = 0; i < 6; i++)
              obj.Restraint.Value[i] = pieces[counter++] == "0" ? false : true;
            this.ForceSend = true;
            break;
          case "STIFF":
            obj.Stiffness = new StructuralVectorSix(new double[6]);
            for (int i = 0; i < 6; i++)
              obj.Stiffness.Value[i] = Convert.ToDouble(pieces[counter++]);
            this.ForceSend = true;
            break;
          case "MESH":
            obj.GSALocalMeshSize = pieces[counter++].ToDouble();
            counter++; // Edge length
            counter++; // Radius
            counter++; // Tie to mesh
            counter++; // Column rigidity
            counter++; // Column prop
            counter++; // Column node
            counter++; // Column angle
            counter++; // Column factor
            counter++; // Column slab factor
            break;
          default: // Axis
            string gwaRec = null;
            obj.Axis = GSA.Parse0DAxis(Convert.ToInt32(s), out gwaRec, obj.Value.ToArray());
            if (gwaRec != null)
              this.SubGWACommand.Add(gwaRec);
            break;
        }
      }

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralNode node = this.Value as StructuralNode;

      string keyword = typeof(GSANode).GetGSAKeyword();

      int index = GSA.NodeAt(node.Value[0], node.Value[1], node.Value[2], Conversions.GSACoincidentNodeAllowance, node.ApplicationId);

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(node));
      ls.Add(index.ToString());
      ls.Add(node.Name == null || node.Name == "" ? " " : node.Name);
      ls.Add("NO_RGB");
      ls.Add(string.Join("\t", node.Value.ToArray()));

      //ls.Add("0"); // TODO: Skip unknown fields in NODE.3
      //ls.Add("0"); // TODO: Skip unknown fields in NODE.3
      //ls.Add("0"); // TODO: Skip unknown fields in NODE.3

      ls.Add("NO_GRID");

      try
      {
        ls.Add(GSA.SetAxis(node.Axis).ToString());
      }
      catch { ls.Add("0"); }

      try
      {
        List<string> subLs = new List<string>();

        if (node.Restraint == null || !node.Restraint.Value.Any(x => x))
          subLs.Add("NO_REST");
        else
        {
          subLs.Add("REST");
          subLs.Add(node.Restraint.Value[0] ? "1" : "0");
          subLs.Add(node.Restraint.Value[1] ? "1" : "0");
          subLs.Add(node.Restraint.Value[2] ? "1" : "0");
          subLs.Add(node.Restraint.Value[3] ? "1" : "0");
          subLs.Add(node.Restraint.Value[4] ? "1" : "0");
          subLs.Add(node.Restraint.Value[5] ? "1" : "0");
        }

        ls.AddRange(subLs);

      }
      catch { ls.Add("NO_REST"); }

      try
      {
        List<string> subLs = new List<string>();

        if (node.Stiffness == null || !node.Stiffness.Value.Any(x => x == 0))
          subLs.Add("NO_STIFF");
        else
        {
          subLs.Add("STIFF");
          subLs.Add(node.Stiffness.Value[0].ToString());
          subLs.Add(node.Stiffness.Value[1].ToString());
          subLs.Add(node.Stiffness.Value[2].ToString());
          subLs.Add(node.Stiffness.Value[3].ToString());
          subLs.Add(node.Stiffness.Value[4].ToString());
          subLs.Add(node.Stiffness.Value[5].ToString());
        }

        ls.AddRange(subLs);
      }
      catch { ls.Add("NO_STIFF"); }

      try
      {
        List<string> subLs = new List<string>();

        if (node.GSALocalMeshSize == 0)
        {
          ls.Add("NO_MESH");
        }
        else
        {
          subLs.Add("MESH");
          subLs.Add(node.GSALocalMeshSize.ToString());
          subLs.Add("0"); // Radius
          subLs.Add("NO"); // Tie to mesh
          subLs.Add("NO"); // column rigidity will be generated
          subLs.Add("0"); // Column property number
          subLs.Add("0"); //Column orientation node
          subLs.Add("0"); //Column orientation angle
          subLs.Add("1"); //Column dimension factor
          subLs.Add("0"); //Column slab thickness factor
        }

        ls.AddRange(subLs);
      }
      catch (Exception)
      {
        ls.Add("NO_MESH");
      }

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  [GSAObject("EL.3", new string[] { "PROP_MASS.2" }, "elements", true, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DElement : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNode();

    public void ParseGWACommand(GSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      StructuralNode obj = new StructuralNode();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      counter++; // Reference
      counter++; // Name
      counter++; // Color
      counter++; // Type
      var mass = GetGSAMass(GSA, Convert.ToInt32(pieces[counter++]));
      obj.Mass = mass;
      counter++; // group
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      // Rest is unimportant for 0D element

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralNode node = this.Value as StructuralNode;

      if (node.Mass == 0)
        return;

      string keyword = typeof(GSA0DElement).GetGSAKeyword();
      int index = GSA.Indexer.ResolveIndex(typeof(GSA0DElement), node);
      int propIndex = GSA.Indexer.ResolveIndex("PROP_MASS.2", node);
      int nodeRef = GSA.Indexer.ResolveIndex(typeof(GSANode), node);

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(node));
      ls.Add(index.ToString());
      ls.Add(node.Name == null || node.Name == "" ? " " : node.Name);
      ls.Add("NO_RGB");
      ls.Add("MASS");
      ls.Add(propIndex.ToString());
      ls.Add("0"); // Group
      ls.Add(nodeRef.ToString());
      ls.Add("0"); // Orient Node
      ls.Add("0"); // Beta
      ls.Add("NO_RLS"); // Release
      ls.Add("0"); // Offset x-start
      ls.Add("0"); // Offset y-start
      ls.Add("0"); // Offset y
      ls.Add("0"); // Offset z
      ls.Add(""); //Dummy

      GSA.RunGWACommand(string.Join("\t", ls));

      ls.Clear();
      ls.Add("SET");
      ls.Add("PROP_MASS.2" + ":" + GSA.GenerateSID(node));
      ls.Add(propIndex.ToString());
      ls.Add("");
      ls.Add("NO_RGB");
      ls.Add("GLOBAL");
      ls.Add(node.Mass.ToString());
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      ls.Add("MOD");
      ls.Add("100%");
      ls.Add("100%");
      ls.Add("100%");

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private double GetGSAMass(GSAInterfacer GSA, int propertyRef)
    {
      string res = GSA.GetGWARecords("GET\tPROP_MASS.2\t" + propertyRef.ToString()).FirstOrDefault();
      string[] pieces = res.ListSplit("\t");

      this.SubGWACommand.Add(res);

      return Convert.ToDouble(pieces[5]);
    }
  }
  
  public static partial class Conversions
  {
    public static bool ToNative(this SpecklePoint inputObject)
    {
      StructuralNode convertedObject = new StructuralNode();

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static bool ToNative(this StructuralNode node)
    {
      new GSANode() { Value = node }.SetGWACommand(GSA);
      new GSA0DElement() { Value = node }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSANode dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSANode)))
        GSASenderObjects[typeof(GSANode)] = new List<object>();

      List<GSANode> nodes = new List<GSANode>();

      string keyword = typeof(GSANode).GetGSAKeyword();
      string[] subKeywords = typeof(GSANode).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSANode)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSANode)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSANode node = new GSANode { GWACommand = p };
        node.ParseGWACommand(GSA);
        nodes.Add(node);
      }

      GSASenderObjects[typeof(GSANode)].AddRange(nodes);

      if (nodes.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA0DElement dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSANode)))
        return new SpeckleNull();

      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA0DElement).GetGSAKeyword();
      string[] subKeywords = typeof(GSA0DElement).GetSubGSAKeyword();

      // Read lines here
      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      bool changed = false;

      // Remove deleted lines
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        foreach (IGSASpeckleContainer o in kvp.Value.Where(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x))))
        {
          o.Value.Mass = 0;
          o.SubGWACommand.RemoveAll(s => lines.Contains(s));
          o.SubGWACommand.RemoveAll(s => deletedLines.Contains(s));

          changed = true;
        }

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSANode)].SelectMany(l => (l as IGSASpeckleContainer).SubGWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit("\t");
        if (pPieces[4].ParseElementNumNodes() == 1)
        {
          try
          {
            GSA0DElement massNode = new GSA0DElement() { GWACommand = p };
            massNode.ParseGWACommand(GSA);

            GSANode match = nodes
                .Where(n => n.Value.ApplicationId == massNode.Value.ApplicationId)
                .First();

            if (match != null)
            {
              match.Value.Mass = massNode.Value.Mass;
              match.SubGWACommand.AddRange(massNode.SubGWACommand.Concat(new string[] { p }));

              match.ForceSend = true;

              changed = true;
            }
          }
          catch { }
        }
      }

      if (changed) return new SpeckleObject();
      return new SpeckleNull();
    }
  }
}
