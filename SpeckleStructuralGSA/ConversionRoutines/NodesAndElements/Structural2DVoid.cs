using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("MEMB.7", new string[] { "NODE.2" }, "elements", false, true, new Type[] { typeof(GSANode) }, new Type[] { })]
  public class GSA2DVoid : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DVoid();

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural2DVoid obj = new Structural2DVoid();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Interface.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var color = pieces[counter++].ParseGSAColor();

      counter++; // Type
      counter++; // Property
      counter++; // Group

      List<double> coordinates = new List<double>();
      string[] nodeRefs = pieces[counter++].ListSplit(" ");
      for (int i = 0; i < nodeRefs.Length; i++)
      {
        GSANode node = nodes.Where(n => n.GSAId.ToString() == nodeRefs[i]).FirstOrDefault();
        coordinates.AddRange(node.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      Structural2DVoid temp = new Structural2DVoid(
          coordinates.ToArray(),
          color.HexToArgbColor());

      obj.Vertices = temp.Vertices;
      obj.Faces = temp.Faces;
      obj.Colors = temp.Colors;

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural2DVoid v = this.Value as Structural2DVoid;

      string keyword = typeof(GSA2DVoid).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(keyword, typeof(GSA2DVoid).Name, v.ApplicationId);

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + HelperClass.GenerateSID(v));
      ls.Add(index.ToString());
      ls.Add(v.Name == null || v.Name == "" ? " " : v.Name);
      ls.Add(v.Colors == null || v.Colors.Count() < 1 ? "NO_RGB" : v.Colors[0].ArgbToHexColor().ToString());
      ls.Add("2D_VOID_CUTTER");
      ls.Add("1"); // Property reference
      ls.Add("0"); // Group
      string topo = "";
      int prevNodeIndex = -1;
      List<int[]> connectivities = v.Edges();
      List<double> coor = new List<double>();

      if (connectivities.Count == 0)
        return;
      
      foreach (int c in connectivities[0])
      {
        coor.AddRange(v.Vertices.Skip(c * 3).Take(3));
        var currIndex = HelperClass.NodeAt(GSA, v.Vertices[c * 3], v.Vertices[c * 3 + 1], v.Vertices[c * 3 + 2], Initialiser.Settings.CoincidentNodeAllowance);
        if (prevNodeIndex != currIndex)
          topo += currIndex.ToString() + " ";
        prevNodeIndex = currIndex;
      }

      ls.Add(topo);
      ls.Add("0"); // Orientation node
      ls.Add("0"); // Angles
      ls.Add("1"); // Target mesh size
      ls.Add("MESH"); // TODO: What is this?
      ls.Add("LINEAR"); // Element type
      ls.Add("0"); // Fire
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // TODO: What is this?
      ls.Add("ACTIVE"); // Dummy
      ls.Add("NO"); // Internal auto offset
      ls.Add("0"); // Offset z
      ls.Add("ALL"); // Exposure

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural2DVoid v)
    {
      new GSA2DVoid() { Value = v }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA2DVoid dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA2DVoid)))
        Initialiser.GSASenderObjects[typeof(GSA2DVoid)] = new List<object>();

      List<GSA2DVoid> voids = new List<GSA2DVoid>();
      List<GSANode> nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA2DVoid).GetGSAKeyword();
      string[] subKeywords = typeof(GSA2DVoid).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSA2DVoid)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSA2DVoid)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit("\t");
        if (pPieces[4].MemberIs2D())
        {
          // Check if void
          if (pPieces[4] == "2D_VOID_CUTTER")
          {
            try
            {
              GSA2DVoid v = new GSA2DVoid() { GWACommand = p };
              v.ParseGWACommand(Initialiser.Interface, nodes);
              voids.Add(v);
            }
            catch { }
          }
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA2DVoid)].AddRange(voids);

      if (voids.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
