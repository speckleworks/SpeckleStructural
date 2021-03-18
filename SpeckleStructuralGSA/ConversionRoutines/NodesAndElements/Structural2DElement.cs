using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System.Text.RegularExpressions;
using SpeckleCoreGeometryClasses;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace SpeckleStructuralGSA
{
  //Elements can have parent members and the application IDs should be based on that of their parents, so they need to be read first, hence the inclusion of that as a read prerequisite
  [GSAObject("EL.4", new string[] { "NODE.3", "PROP_2D.7" }, "model", true, false, new Type[] { typeof(GSANode), typeof(GSA2DProperty), typeof(GSA2DMember) }, new Type[] { typeof(GSANode), typeof(GSA2DProperty) })]
  public class GSA2DElement : GSABase<Structural2DElement>
  {
    public int Member;

    public void ParseGWACommand(List<GSANode> nodes, List<GSA2DProperty> props)
    {
      // GWA command from 10.1 docs
      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle |
      // is_rls { | rls { | k } }
      // off_x1 | off_x2 | off_y | off_z | parent_member | dummy

      if (this.GWACommand == null)
      {
        return;
      }
      var keyword = this.GetGSAKeyword();

      var obj = new Structural2DElement();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(keyword, this.GSAId);
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
      try
      {
        obj.Axis = Helper.Parse2DAxis(obj.Vertices.ToArray(),
            Convert.ToDouble(pieces[counter++]),
            prop != null && prop.IsAxisLocal);
      }
      catch
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Generating axis from coordinates for 2D element", obj.ApplicationId);
      }

      if (prop != null)
      {
        this.SubGWACommand.Add(prop.GWACommand);
      }

      if (pieces[counter++] != "NO_RLS")
      {
        var start = pieces[counter++];
        var end = pieces[counter++];

        counter += start.Split('K').Length - 1 + end.Split('K').Length - 1;
      }

      counter++; //Ofsset x-start
      counter++; //Ofsset x-end
      counter++; //Ofsset y

      Initialiser.AppResources.Proxy.GetGSATotal2DElementOffset(propertyGSAId, Convert.ToDouble(pieces[counter++]), out var offset, out var offsetRec);
      this.SubGWACommand.Add(offsetRec);

      obj.Offset = offset;

      counter++; //Debug flag

      //Process the parent member ID, if present
      if (counter < pieces.Count() && !string.IsNullOrEmpty(pieces[counter]))
      {
        var memberStr = pieces[counter++]; // no references to this piece of data, why do we store it rather than just skipping over?
        if (int.TryParse(memberStr, out Member))
        {
          obj.ApplicationId = SpeckleStructuralClasses.Helper.CreateChildApplicationId(this.GSAId, Helper.GetApplicationId(typeof(GSA2DMember).GetGSAKeyword(), Member));
        }
      }
      counter++; // Dummy

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());
      if (this.Member > 0)
      {
        ((Dictionary<string, object>)obj.Properties["structural"]).Add("GsaParentId", this.Member.ToString());
      }

      this.Value = obj;

      Initialiser.AppResources.Cache.SetApplicationId(keyword, this.GSAId, obj.ApplicationId);
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
      { 
        return "";
      }

      var mesh = this.Value as Structural2DElement;
      if (mesh.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSA2DElement).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA2DElement).GetGSAKeyword(), mesh.ApplicationId);

      var propKeyword = typeof(GSA2DProperty).GetGSAKeyword();
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(propKeyword, mesh.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(propKeyword, mesh.PropertyRef);
      if (indexResult == null && mesh.ApplicationId != null)
      {
        if (mesh.PropertyRef == null)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Blank property references found for these Application IDs:",
            mesh.ApplicationId);
        }
        else
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "2D property references not found:",
            mesh.ApplicationId + " referencing " + mesh.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(mesh);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
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
        ls.Add(Initialiser.AppResources.Proxy.NodeAt(mesh.Vertices[mesh.Faces[i] * 3], mesh.Vertices[mesh.Faces[i] * 3 + 1], mesh.Vertices[mesh.Faces[i] * 3 + 2], Initialiser.AppResources.Settings.CoincidentNodeAllowance).ToString());
      }
      ls.Add("0"); //Orientation node
      if (mesh.Axis == null)
      {
        ls.Add("0");
      }
      else
      {
        try
        {
          ls.Add(Helper.Get2DAngle(coor.ToArray(), mesh.Axis).ToString());
        }
        catch { ls.Add("0"); }
      }
      ls.Add("NO_RLS");

      ls.Add("0"); // Offset x-start
      ls.Add("0"); // Offset x-end
      ls.Add("0"); // Offset y
      ls.Add(mesh.Offset.ToString());

      ls.Add(""); // parent_member

      ls.Add((mesh.GSADummy.HasValue && mesh.GSADummy.Value) ? "DUMMY" : "");

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }

  }

  [GSAObject("MEMB.8", new string[] { "NODE.3" }, "model", false, true, new Type[] { typeof(GSANode), typeof(GSA2DProperty) }, new Type[] { typeof(GSANode), typeof(GSA2DProperty) })]
  public class GSA2DMember : GSABase<SpeckleMesh>
  {
    public int Group; // Keep for load targetting

    public void ParseGWACommand(List<GSANode> nodes, List<GSA2DProperty> props)
    {
      // MEMB.8 | num | name | colour | type (2D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | time[4] | dummy | off_auto_internal | off_z | reinforcement2d |

      if (this.GWACommand == null)
        return;

      var obj = new Structural2DElementMesh();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      var color = pieces[counter++].ParseGSAColor(); // colour

      var type = pieces[counter++];
      obj.ElementType = (type == "SLAB") ? Structural2DElementType.Slab : (type == "WALL") ? Structural2DElementType.Wall : Structural2DElementType.Generic;

      counter++; // exposure - fire property

      var propertyGSAId = Convert.ToInt32(pieces[counter++]);

      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA2DProperty).GetGSAKeyword(), propertyGSAId);
      this.Group = Convert.ToInt32(pieces[counter++]); // Keep group for load targetting

      // topology
      var coordinates = new List<double>();
      var nodeRefsFull = pieces[counter++];

      //Remove the specification of internal nodes
      var nodeRefsWithoutInternalNodes = Regex.Replace(nodeRefsFull, @"P\([0-9]*(.*?)\)", "");
      nodeRefsWithoutInternalNodes = Regex.Replace(nodeRefsWithoutInternalNodes, @"L\([0-9]*(.*?)\)", "");
      nodeRefsWithoutInternalNodes = Regex.Replace(nodeRefsWithoutInternalNodes, @"V\([0-9]*(.*?)\)", "");

      var nodeRefs = nodeRefsWithoutInternalNodes.Trim().ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId.ToString() == nodeRefs[i]).FirstOrDefault();
        if (node == null)
        {
          //TO DO: review how this is possible and prevent it
          continue;
        }

        coordinates.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var temp = new Structural2DElementMesh(
          coordinates.Essential(),
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
      StructuralAxis axis = null;
      try
      {
        axis = Helper.Parse2DAxis(coordinates.ToArray(),
            Convert.ToDouble(pieces[counter++]),
            prop == null ? false : (prop as GSA2DProperty).IsAxisLocal);
      }
      catch
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Generating axis from coordinates for 2D member", obj.ApplicationId);
      }

      if (axis != null)
      {
        obj.Axis = Enumerable.Repeat(axis, numFaces).ToList();
        if (prop != null)
        {
          this.SubGWACommand.Add(prop.GWACommand);
        }
      }

      //Since this is a GSA-specific property, only set if not default
      var meshSize = Convert.ToDouble(pieces[counter++]);
      if (meshSize > 0)
      {
        obj.GSAMeshSize = meshSize; // mesh_size
      }

      counter++; // intersector
      counter++; // analysis type

      counter = counter+=6; // skip fire bits to get to dummy status
      //Since this is a GSA-specific property, only set if true;
      var dummy = (pieces[counter++] == "DUMMY");
      if (dummy)
      {
        obj.GSADummy = dummy;
      }

      Initialiser.AppResources.Proxy.GetGSATotal2DElementOffset(propertyGSAId, Convert.ToDouble(pieces[counter++]), out var offset, out var offsetRec);
      this.SubGWACommand.Add(offsetRec);

      obj.Offset = Enumerable.Repeat(offset, numFaces).ToList();

      // skip remaining commands

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var obj = (SpeckleObject)this.Value;
      var baseMesh = (SpeckleMesh)this.Value;

      if (baseMesh.Vertices == null || baseMesh.Vertices.Count() == 0)
        return "";

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
        mesh = new Structural2DElementMesh() { baseMesh = ((Structural2DElement) this.Value).baseMesh };
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
        mesh = ((Structural2DElementMesh) this.Value);
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

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA2DMember).GetGSAKeyword(), obj.ApplicationId);

      var propKeyword = typeof(GSA2DProperty).GetGSAKeyword();
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(propKeyword, propRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propIndex = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(propKeyword, propRef);
      if (indexResult == null && obj.ApplicationId != null)
      {
        if (propRef == null)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Blank property references found for these Application IDs:",
            obj.ApplicationId);
        }
        else
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "2D property references not found:",
            obj.ApplicationId + " referencing " + propRef);
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
      ls.Add(ElementTypeToString(structural2dElementType));
      ls.Add("ALL");
      ls.Add(propIndex.ToString());
      ls.Add(group != 0 ? group.ToString() : index.ToString()); // TODO: This allows for targeting of elements from members group
      
      mesh.Consolidate();
      var connectivities = mesh.Edges();

      if (connectivities == null || connectivities.Count() == 0)
      {
        //This is likely if the vertices are duplicated or other strange states
        return "";
      }

      var coor = new List<double>();
      foreach (var c in connectivities[0])
      {
        coor.AddRange(mesh.Vertices.Skip(c * 3).Take(3));
      }
      coor = coor.Essential().ToList();
      var coorPts = Enumerable.Range(0, coor.Count() / 3).Select(i => new double[] { coor[i * 3], coor[i * 3 + 1], coor[i * 3 + 2] }).ToList();

      //Use these reduced coordinates to call NodeAt and create a topo string
      // topo
      var topo = "";
      var prevNodeIndex = -1;
      foreach (var coorPt in coorPts)
      {
        var currIndex = Initialiser.AppResources.Proxy.NodeAt(coorPt[0], coorPt[1], coorPt[2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
        if (prevNodeIndex != currIndex)
        {
          topo += currIndex.ToString() + " ";
        }
        prevNodeIndex = currIndex;
      }

      ls.Add(topo.Trim());
      
      ls.Add("0"); // Orientation node
      
      // angle
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
      ls.Add("YES"); // intersector
      ls.Add("LINEAR"); // Element type
      ls.Add("0"); // Fire resistance (mins)
      ls.Add("0"); // Fire limiting temp
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // Time 4
      ls.Add((gsaDummy.HasValue && gsaDummy.Value) ? "DUMMY" : "ACTIVE");
      ls.Add(offset.HasValue ? offset.ToString() : "0"); // Offset z
      ls.Add("NO"); // Internal auto offset
      //These are default values - filled in here to avoid instances of GWA comparisons (when upserting into the cache) showing change where there isn't
      ls.Add("REBAR_2D.1");
      ls.Add("0.03");
      ls.Add("0.03");
      ls.Add("0");
      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      return string.Join("\n", gwaCommands);
    }

    private string ElementTypeToString(Structural2DElementType t)
    {
      if (t == Structural2DElementType.Slab)
      {
        return "SLAB";
      }
      if (t == Structural2DElementType.Wall)
      {
        return "WALL";
      }
      return "2D_GENERIC";
    }
  }
  
  public static partial class Conversions
  {
    public static string ToNative(this Structural2DElement el)
    {
      var layer = Initialiser.AppResources.Settings.TargetLayer;

      if (el == null || el.Vertices == null || el.ApplicationId == null)
      {
        return "";
      }

      if (layer == GSATargetLayer.Design)
      {
        return new GSA2DMember() { Value = el }.SetGWACommand();
      }
      if (layer == GSATargetLayer.Analysis && (el.NumFaces() == 1))
      {
        return new GSA2DElement() { Value = el }.SetGWACommand();
      }

      //Reaching this point means it should be treated as a full analytical mesh - where each face creates a new 2D element
      el.Consolidate();

      var elMesh = new Structural2DElementMesh(el.Vertices, el.Faces, el.Colors, el.ElementType, el.PropertyRef,
        new[] { el.Axis }, new[] { el.Offset ?? 0 }, el.ApplicationId, el.GSAMeshSize ?? 0, el.Properties);

      return (new GSA2DElementMesh() { Value = elMesh }).SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DElement dummyObject)
    {
      var typeName = dummyObject.GetType().Name;
      var newElementLines = ToSpeckleBase<GSA2DElement>();
      var newMeshLines = ToSpeckleBase<GSA2DElementMesh>();
      var newLinesTuples = new List<Tuple<int, string>>();
      var keyword = dummyObject.GetGSAKeyword();

      foreach (var k in newElementLines.Keys)
      {
        newLinesTuples.Add(new Tuple<int, string>(k, newElementLines[k]));
      }
      foreach (var k in newMeshLines.Keys)
      {
        newLinesTuples.Add(new Tuple<int, string>(k, newMeshLines[k]));
      }

      var elementsLock = new object();
      var elements = new List<GSA2DElement>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var props = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DProperty>();

      var newLines = newLinesTuples.Select(nl => nl.Item2);
      Parallel.ForEach(newLines, p =>
      {
        var pPieces = p.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        // Check if void or not an element
        if (!(pPieces[4] == "2D_VOID_CUTTER" || pPieces[4].Is1DMember() || pPieces[4].Is2DMember())
          && (pPieces[4].ParseElementNumNodes() == 3 | pPieces[4].ParseElementNumNodes() == 4))
        {
          var gsaId = pPieces[1];
          try
          {
            var element = new GSA2DElement() { GWACommand = p };
            element.ParseGWACommand(nodes, props);
            lock (elementsLock)
            {
              elements.Add(element);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
              "Keyword=" + keyword, "Index=" + gsaId);
          }
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(elements);

      return (elements.Count() == 0) ? new SpeckleNull() :  new SpeckleObject();
    }

    public static SpeckleObject ToSpeckle(this GSA2DMember dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DMember>();
      var typeName = dummyObject.GetType().Name;
      var membersLock = new object();
      var members = new SortedDictionary<int, GSA2DMember>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var props = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DProperty>();
      var keyword = dummyObject.GetGSAKeyword();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        if (pPieces[4].Is2DMember())
        {
          // Check if dummy
          if (pPieces[20] == "ACTIVE")
          {
            var gsaId = pPieces[1];
            try
            {
              var member = new GSA2DMember() { GWACommand = newLines[k] };
              member.ParseGWACommand(nodes, props);
              lock (membersLock)
              {
                members.Add(k, member);
              }
            }
            catch (Exception ex)
            {
              Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
                "Keyword=" + keyword, "Index=" + k);
            }
          }
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GsaKit.GSASenderObjects.AddRange(members.Values.ToList());

      return (members.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
