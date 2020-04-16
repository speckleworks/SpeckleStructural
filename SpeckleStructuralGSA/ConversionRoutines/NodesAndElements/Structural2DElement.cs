using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System.Text.RegularExpressions;
using SpeckleCoreGeometryClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { "NODE.2", "PROP_2D" }, "elements", true, false, new Type[] { typeof(GSANode), typeof(GSA2DProperty) }, new Type[] { typeof(GSA2DProperty) })]
  public class GSA2DElement : IGSASpeckleContainer
  {
    public string Member;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DElement();

    public void ParseGWACommand( List<GSANode> nodes, List<GSA2DProperty> props)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DElement();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var color = pieces[counter++].ParseGSAColor();

      var type = pieces[counter++];
      if (color != null)
        obj.Colors = Enumerable.Repeat(color.HexToArgbColor().Value, type.ParseElementNumNodes()).ToList();
      else
        obj.Colors = new List<int>();

      obj.ElementType = Structural2DElementType.Generic;
      var propertyGSAId = Convert.ToInt32(pieces[counter++]);
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA2DProperty).GetGSAKeyword(), propertyGSAId);
      counter++; // Group

      obj.Vertices = new List<double>();
      obj.Faces = new List<int>() { type.ParseElementNumNodes() - 3 };

      for (var i = 0; i < type.ParseElementNumNodes(); i++)
      {
        var key = pieces[counter++];
        var node = nodes.Where(n => n.GSAId.ToString() == key).FirstOrDefault();
        obj.Vertices.AddRange(node.Value.Value);
        obj.Faces.Add(i);
        this.SubGWACommand.Add(node.GWACommand);
      }

      counter++; // Orientation node

      var prop = props.Where(p => p.Value.ApplicationId == obj.PropertyRef).FirstOrDefault();
      obj.Axis = Helper.Parse2DAxis(obj.Vertices.ToArray(),
          Convert.ToDouble(pieces[counter++]),
          prop == null ? false : (prop as GSA2DProperty).IsAxisLocal);
      if (prop != null)
        this.SubGWACommand.Add(prop.GWACommand);

      if (pieces[counter++] != "NO_RLS")
      {
        var start = pieces[counter++];
        var end = pieces[counter++];

        counter += start.Split('K').Length - 1 + end.Split('K').Length - 1;
      }

      counter++; //Ofsset x-start
      counter++; //Ofsset x-end
      counter++; //Ofsset y

      Initialiser.Interface.GetGSATotal2DElementOffset(propertyGSAId, Convert.ToDouble(pieces[counter++]), out var offset, out var offsetRec);
      this.SubGWACommand.Add(offsetRec);

      obj.Offset = offset;

      counter++; // Dummy

      if (counter < pieces.Length)
        Member = pieces[counter++];

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
      { 
        return "";
        }

      var mesh = this.Value as Structural2DElement;

      var keyword = typeof(GSA2DElement).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA2DElement).GetGSAKeyword(), mesh.ApplicationId);

      var propKeyword = typeof(GSA2DProperty).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, mesh.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, mesh.PropertyRef);
      if (indexResult == null && mesh.ApplicationId != null)
      {
        if (mesh.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", mesh.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", mesh.ApplicationId + " referencing " + mesh.PropertyRef);
        }
      }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(mesh),
        index.ToString(),
        mesh.Name == null || mesh.Name == "" ? " " : mesh.Name,
        mesh.Colors == null || mesh.Colors.Count() < 1 ? "NO_RGB" : mesh.Colors[0].ArgbToHexColor().ToString(),
        mesh.Vertices.Count() / 3 == 3 ? "TRI3" : "QUAD4",
        propRef.ToString(),
        group.ToString() // Group
      };
      var numVertices = mesh.Faces[0] + 3;
      var coor = new List<double>();
      for (var i = 1; i < mesh.Faces.Count(); i++)
      {
        coor.AddRange(mesh.Vertices.Skip(mesh.Faces[i] * 3).Take(3));
        ls.Add(Helper.NodeAt(mesh.Vertices[mesh.Faces[i] * 3], mesh.Vertices[mesh.Faces[i] * 3 + 1], mesh.Vertices[mesh.Faces[i] * 3 + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString());
      }
      ls.Add("0"); //Orientation node
      try
      {
        ls.Add(Helper.Get2DAngle(coor.ToArray(), mesh.Axis).ToString());
      }
      catch { ls.Add("0"); }
      ls.Add("NO_RLS");

      ls.Add("0"); // Offset x-start
      ls.Add("0"); // Offset x-end
      ls.Add("0"); // Offset y
      ls.Add(mesh.Offset.ToString());

      //ls.Add("NORMAL"); // Action // TODO: EL.4 SUPPORT
      ls.Add((mesh.GSADummy.HasValue && mesh.GSADummy.Value) ? "DUMMY" : "");

      return (string.Join("\t", ls));
    }

  }

  [GSAObject("MEMB.7", new string[] { "NODE.2" }, "elements", false, true, new Type[] { typeof(GSANode), typeof(GSA2DProperty) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA2DMember : IGSASpeckleContainer
  {
    public int Group; // Keep for load targetting

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DElementMesh();

    public void ParseGWACommand( List<GSANode> nodes, List<GSA2DProperty> props)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DElementMesh();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var color = pieces[counter++].ParseGSAColor();

      var type = pieces[counter++];
      if (type == "SLAB")
        obj.ElementType = Structural2DElementType.Slab;
      else if (type == "WALL")
        obj.ElementType = Structural2DElementType.Wall;
      else
        obj.ElementType = Structural2DElementType.Generic;

      var propertyGSAId = Convert.ToInt32(pieces[counter++]);

      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA2DProperty).GetGSAKeyword(), propertyGSAId);
      this.Group = Convert.ToInt32(pieces[counter++]); // Keep group for load targetting

      var coordinates = new List<double>();
      var nodeRefsFull = pieces[counter++];

      //Remove the specification of internal nodes
      var nodeRefsWithoutInternalNodes = Regex.Replace(nodeRefsFull, @"P\([0-9]*(.*?)\)", "");

      var nodeRefs = nodeRefsWithoutInternalNodes.Trim().ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId.ToString() == nodeRefs[i]).FirstOrDefault();
        if (node == null) continue;

        coordinates.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var temp = new Structural2DElementMesh(
          coordinates.ToArray(),
          color.HexToArgbColor(),
          obj.ElementType, obj.PropertyRef,
          null,
          null);

      obj.Vertices = temp.Vertices;
      obj.Faces = temp.Faces;
      obj.Colors = temp.Colors;

      var numFaces = 0;
      for (var i = 0; i < obj.Faces.Count(); i++)
      {
        numFaces++;
        i += obj.Faces[i] + 3;
      }

      counter++; // Orientation node

      var prop = props.Where(p => p.Value.ApplicationId == obj.PropertyRef).FirstOrDefault();
      var axis = Helper.Parse2DAxis(coordinates.ToArray(),
          Convert.ToDouble(pieces[counter++]),
          prop == null ? false : (prop as GSA2DProperty).IsAxisLocal);
      obj.Axis = Enumerable.Repeat(axis, numFaces).ToList();
      if (prop != null)
        this.SubGWACommand.Add(prop.GWACommand);

      // Skip to offsets at second to last
      counter = pieces.Length - 2;

      Initialiser.Interface.GetGSATotal2DElementOffset(propertyGSAId, Convert.ToDouble(pieces[counter++]), out var offset, out var offsetRec);
      this.SubGWACommand.Add(offsetRec);

      obj.Offset = Enumerable.Repeat(offset, numFaces).ToList();

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var obj = (SpeckleObject)this.Value;
      var baseMesh = (SpeckleMesh)this.Value;
      var colour = (baseMesh.Colors == null || baseMesh.Colors.Count < 1) ? "NO_RGB" : baseMesh.Colors[0].ArgbToHexColor().ToString();

      StructuralAxis axis;
      Structural2DElementMesh mesh;
      bool? gsaDummy;
      string propRef;
      double? offset;
      string sid;
      Structural2DElementType structural2dElementType;
      double? gsaMeshSize;

      //This is to avoid relying on the fact that this.Value is dynamic - casting to one of the expected types first
      if (this.Value is Structural2DElement)
      {
        var el = (Structural2DElement)this.Value;
        structural2dElementType = el.ElementType;
        sid = Helper.GenerateSID(el);
        propRef = el.PropertyRef;
        mesh = new Structural2DElementMesh() { baseMesh = this.Value.baseMesh };
        gsaMeshSize = el.GSAMeshSize;
        gsaDummy = el.GSADummy;
        axis = el.Axis;
        offset = el.Offset;
      }
      else if (this.Value is Structural2DElementMesh)
      {
        var el = (Structural2DElementMesh)this.Value;
        structural2dElementType = el.ElementType;
        propRef = el.PropertyRef;
        sid = Helper.GenerateSID(el);
        mesh = this.Value;
        gsaMeshSize = el.GSAMeshSize;
        gsaDummy = el.GSADummy;
        axis = (el.Axis == null || el.Axis.Count == 0) ? null : el.Axis.First();
        offset = (el.Offset == null || el.Offset.Count == 0) ? null : (double ?) el.Offset.First();
      }
      else
      {
        return "";
      }

      var keyword = typeof(GSA2DMember).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA2DMember).GetGSAKeyword(), obj.ApplicationId);

      var propKeyword = typeof(GSA2DProperty).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, propRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propIndex = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, propRef);
      if (indexResult == null && obj.ApplicationId != null)
      {
        if (propRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", obj.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", obj.ApplicationId + " referencing " + propRef);
        }
      }

      var gwaCommands = new List<string>();

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + sid,
        index.ToString(),
        string.IsNullOrEmpty(obj.Name) ? " " : obj.Name,
        colour
      };
      if (structural2dElementType == Structural2DElementType.Slab)
        ls.Add("SLAB");
      else if (structural2dElementType == Structural2DElementType.Wall)
        ls.Add("WALL");
      else
        ls.Add("2D_GENERIC");
      ls.Add(propIndex.ToString());
      ls.Add(group != 0 ? group.ToString() : index.ToString()); // TODO: This allows for targeting of elements from members group
      var topo = "";
      var prevNodeIndex = -1;
      var connectivities = mesh.Edges();
      var coor = new List<double>();
      foreach (var c in connectivities[0])
      {
        coor.AddRange(baseMesh.Vertices.Skip(c * 3).Take(3));
        var currIndex = Helper.NodeAt(baseMesh.Vertices[c * 3], baseMesh.Vertices[c * 3 + 1], baseMesh.Vertices[c * 3 + 2], Initialiser.Settings.CoincidentNodeAllowance);
        if (prevNodeIndex != currIndex)
        {
          topo += currIndex.ToString() + " ";
        }
        prevNodeIndex = currIndex;
      }
      ls.Add(topo.Trim());
      ls.Add("0"); // Orientation node
      if (axis == null)
      {
        ls.Add("0");
      }
      else
      {
        try
        {
          ls.Add(Helper.Get2DAngle(coor.ToArray(), axis).ToString());
        }
        catch { ls.Add("0"); }
      }
      ls.Add(gsaMeshSize == 0 ? "1" : gsaMeshSize.ToString()); // Target mesh size
      ls.Add("MESH"); // TODO: What is this?
      ls.Add("LINEAR"); // Element type
      ls.Add("0"); // Fire
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // TODO: What is this?
      ls.Add((gsaDummy.HasValue && gsaDummy.Value) ? "DUMMY" : "ACTIVE");
      ls.Add("NO"); // Internal auto offset
      ls.Add(offset.HasValue ? offset.ToString() : "0"); // Offset z
      ls.Add("ALL"); // Exposure

      gwaCommands.Add(string.Join("\t", ls));

      return string.Join("\n", gwaCommands);
    }
  }
  
  public static partial class Conversions
  {
    public static string ToNative(this Structural2DElement mesh)
    {
      return (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) ? new GSA2DElement() { Value = mesh }.SetGWACommand() : new GSA2DMember() { Value = mesh }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DElement dummyObject)
    {
      var newElementLines = ToSpeckleBase<GSA2DElement>();
      var newMeshLines = ToSpeckleBase<GSA2DElementMesh>();
      var newLines = new List<Tuple<int, string>>();
      foreach (var k in newElementLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newElementLines[k]));
      }
      foreach (var k in newMeshLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newMeshLines[k]));
      }

      var elements = new List<GSA2DElement>();
      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();
      var props = Initialiser.GSASenderObjects[typeof(GSA2DProperty)].Cast<GSA2DProperty>().ToList();

      foreach (var p in newLines.Select(nl => nl.Item2))
      {
        var pPieces = p.ListSplit("\t");
        // Check if void or not an element
        if (pPieces[4] == "2D_VOID_CUTTER" || pPieces[4].Is1DMember() || pPieces[4].Is2DMember())
        {
          continue;
        }
        if (pPieces[4].ParseElementNumNodes() == 3 | pPieces[4].ParseElementNumNodes() == 4)
        {
          try
          {
            var element = new GSA2DElement() { GWACommand = p };
            element.ParseGWACommand(nodes, props);
            elements.Add(element);
          }
          catch { }
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA2DElement)].AddRange(elements);

      return (elements.Count() == 0) ? new SpeckleNull() :  new SpeckleObject();
    }

    public static SpeckleObject ToSpeckle(this GSA2DMember dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DMember>();

      var members = new List<GSA2DMember>();
      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();
      var props = Initialiser.GSASenderObjects[typeof(GSA2DProperty)].Cast<GSA2DProperty>().ToList();

      foreach (var p in newLines.Values)
      {
        var pPieces = p.ListSplit("\t");
        if (pPieces[4].Is2DMember())
        {
          // Check if dummy
          if (pPieces[pPieces.Length - 4] == "ACTIVE")
          {
            try
            {
              var member = new GSA2DMember() { GWACommand = p };
              member.ParseGWACommand(nodes, props);
              members.Add(member);
            }
            catch { }
          }
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA2DMember)].AddRange(members);

      return (members.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
