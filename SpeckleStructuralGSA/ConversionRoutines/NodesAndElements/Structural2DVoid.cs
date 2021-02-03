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
  [GSAObject("MEMB.8", new string[] { "NODE.3" }, "model", false, true, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA2DVoid : GSABase<Structural2DVoid>
  {
    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DVoid();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      var color = pieces[counter++].ParseGSAColor();

      counter++; // Type
      counter++; // Exposure
      counter++; // Property
      counter++; // Group

      var coordinates = new List<double>();
      var nodeRefs = pieces[counter++].ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId.ToString() == nodeRefs[i]).FirstOrDefault();
        if (node == null)
        {
          //TO DO: review how this is possible and prevent it
          continue;
        }

        var speckleNodeObj = node.Value;
        if (speckleNodeObj != null)
        {
          coordinates.AddRange(speckleNodeObj.Value);
          this.SubGWACommand.Add(node.GWACommand);
        }
      }

      var temp = new Structural2DVoid(
          coordinates.Essential(),
          color.HexToArgbColor());

      obj.Vertices = temp.Vertices;
      obj.Faces = temp.Faces;
      obj.Colors = temp.Colors;

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var v = this.Value as Structural2DVoid;

      if (v.Vertices == null || v.Vertices.Count() == 0)
        return "";

      var keyword = typeof(GSA2DVoid).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, v.ApplicationId);

      var sid = Helper.GenerateSID(v);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        v.Name == null || v.Name == "" ? " " : v.Name,
        v.Colors == null || v.Colors.Count() < 1 ? "NO_RGB" : v.Colors[0].ArgbToHexColor().ToString(),
        "2D_VOID_CUTTER",
        "ALL", // exposure
        "1", // Property reference
        "0" // Group
      };
      
      // topo
      var topo = "";
      var prevNodeIndex = -1;
      //var coor = new List<double>();
      
      if (v.Faces != null && v.Faces.Count() > 0)
      {
        var connectivities = v.Edges();
        foreach (var c in connectivities[0])
        {
          //coor.AddRange(v.Vertices.Skip(c * 3).Take(3));
          var currIndex = Initialiser.AppResources.Proxy.NodeAt(v.Vertices[c * 3], v.Vertices[c * 3 + 1], v.Vertices[c * 3 + 2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
          if (prevNodeIndex != currIndex)
            topo += currIndex.ToString() + " ";
          prevNodeIndex = currIndex;
        }
      }
      else
      {
        //Even if something has gone wrong in the generation of the faces (and therefore edges) of the mesh, if there are exactly 3 or 4 points, 
        //assume that it is planar and create a void.
        //TO DO: review why the generation of faces results in an empty list for wall voids in the ToSpeckle() code.  Once that has been fixed,
        //the block below can be removed
        if (v.Vertices != null && (v.Vertices.Count() == 9 || v.Vertices.Count() == 12))
        {
          int numVertices = v.Vertices.Count() / 3;
          var indices = new List<int>();
          for (var i = 0; i < numVertices; i++)
          {
            var currIndex = Initialiser.AppResources.Proxy.NodeAt(v.Vertices[i * 3], v.Vertices[i * 3 + 1], v.Vertices[i * 3 + 2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);
            if (prevNodeIndex != currIndex)
            {
              topo += currIndex.ToString() + " ";
            }
            prevNodeIndex = currIndex;
          }
        }
        else
        {
          return "";
        }
      }
      
      ls.Add(topo);
      
      ls.Add("0"); // Orientation node
      ls.Add("0"); // Angles
      ls.Add("1"); // Target mesh size
      ls.Add("YES"); // intersector
      ls.Add("LINEAR"); // Element type
      ls.Add("0"); // Fire resistance (mins)
      ls.Add("0"); // Fire limiting temp
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // Time 4
      ls.Add("ACTIVE"); // Dummy
      ls.Add("0"); // Offset z
      ls.Add("NO"); // Internal auto offset
      //These are default values - filled in here to avoid instances of GWA comparisons (when upserting into the cache) showing change where there isn't
      ls.Add("REBAR_2D.1");
      ls.Add("0.03");
      ls.Add("0.03");
      ls.Add("0");
      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
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
      var typeName = dummyObject.GetType().Name;
      var voidsLock = new object();
      var voids = new SortedDictionary<int, GSA2DVoid>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        if (!pPieces[4].Is2DMember())
        {
          // Check if void
          if (pPieces[4] == "2D_VOID_CUTTER")
          {
            var gsaId = pPieces[1];
            try
            {
              var v = new GSA2DVoid() { GWACommand = newLines[k] };
              v.ParseGWACommand(nodes);
              lock (voidsLock)
              {
                voids.Add(k, v);
              }
            }
            catch (Exception ex)
            {
              Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, gsaId);
              Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, gsaId);
            }
          }
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(voids.Values.ToList());

      return (voids.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
