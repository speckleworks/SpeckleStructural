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

    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DVoid();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = HelperClass.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var color = pieces[counter++].ParseGSAColor();

      counter++; // Type
      counter++; // Property
      counter++; // Group

      var coordinates = new List<double>();
      var nodeRefs = pieces[counter++].ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId.ToString() == nodeRefs[i]).FirstOrDefault();
        var speckleNodeObj = node.Value;
        if (speckleNodeObj != null)
        {
          coordinates.AddRange(speckleNodeObj.Value);
          this.SubGWACommand.Add(node.GWACommand);
        }
      }

      var temp = new Structural2DVoid(
          coordinates.ToArray(),
          color.HexToArgbColor());

      obj.Vertices = temp.Vertices;
      obj.Faces = temp.Faces;
      obj.Colors = temp.Colors;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var v = this.Value as Structural2DVoid;

      var keyword = typeof(GSA2DVoid).GetGSAKeyword();

      var index = Initialiser.Indexer.ResolveIndex(keyword, typeof(GSA2DVoid).ToSpeckleTypeName(), v.ApplicationId);

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + HelperClass.GenerateSID(v),
        index.ToString(),
        v.Name == null || v.Name == "" ? " " : v.Name,
        v.Colors == null || v.Colors.Count() < 1 ? "NO_RGB" : v.Colors[0].ArgbToHexColor().ToString(),
        "2D_VOID_CUTTER",
        "1", // Property reference
        "0" // Group
      };
      var topo = "";
      var prevNodeIndex = -1;
      var connectivities = v.Edges();
      var coor = new List<double>();

      if (connectivities.Count == 0)
        return "";
      
      foreach (var c in connectivities[0])
      {
        coor.AddRange(v.Vertices.Skip(c * 3).Take(3));
        var currIndex = HelperClass.NodeAt(v.Vertices[c * 3], v.Vertices[c * 3 + 1], v.Vertices[c * 3 + 2], Initialiser.Settings.CoincidentNodeAllowance);
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

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DVoid v)
    {
      return new GSA2DVoid() { Value = v }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DVoid dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DVoid>();

      var voids = new List<GSA2DVoid>();
      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      foreach (var p in newLines.Values)
      {
        var pPieces = p.ListSplit("\t");
        if (!pPieces[4].MemberIs2D())
        {
          // Check if void
          if (pPieces[4] == "2D_VOID_CUTTER")
          {
            try
            {
              var v = new GSA2DVoid() { GWACommand = p };
              v.ParseGWACommand(nodes);
              voids.Add(v);
            }
            catch { }
          }
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA2DVoid)].AddRange(voids);

      return (voids.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
