using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { "NODE.3" }, "model", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode), typeof(GSA1DProperty) })]
  public class GSA0DSpring : GSABase<Structural0DSpring>
  {
    public int Member;

    //Sending
    public void ParseGWACommand(List<GSANode> nodes)
    {
      // GWA command from 10.1 docs
      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle |
      // is_rls { | rls { | k } }
      // off_x1 | off_x2 | off_y | off_z | parent_member | dummy

      if (this.GWACommand == null)
        return;

      var obj = new Structural0DSpring();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]); // num
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      counter++; // Colour
      counter++; // Type
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSASpringProperty).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      counter++; // Group

      obj.Value = new List<double>();

      var key = pieces[counter++];
      if (int.TryParse(key, out var nodeIndex))
      {
        if (nodeIndex > 0)
        {
          var node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
          obj.Value.AddRange(node.Value.Value);
          this.SubGWACommand.Add(node.GWACommand);
        }
      }
      var orientationNodeRef = pieces[counter++];
      var rotationAngle = Convert.ToDouble(pieces[counter++]);

      var memberStr = pieces[counter++];
      if (string.IsNullOrEmpty(memberStr) == false)
      {
        int.TryParse(memberStr, out Member);
      }

      counter++; // Dummy

      this.Value = obj;
    }

    public string SetGWACommand(IGSAProxy GSA, int group = 0)
    {
      if (this.Value == null)
        return "";

      var spring = this.Value as Structural0DSpring;
      if (spring.Value == null || spring.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA0DSpring).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, spring.ApplicationId);

      var propKeyword = typeof(GSASpringProperty).GetGSAKeyword();
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(propKeyword, spring.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(propKeyword, spring.PropertyRef);
      if (indexResult == null && spring.ApplicationId != null)
      {
        if (spring.PropertyRef == null)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Blank property references found for these Application IDs:",
            spring.ApplicationId);
        }
        else
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Spring property references not found:",
            spring.ApplicationId + " referencing " + spring.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(spring);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        spring.Name == null || spring.Name == "" ? " " : spring.Name,
        "NO_RGB",
        "GRD_SPRING", //type
        propRef.ToString(), //Property
        group.ToString(), //Group
        //"1", //Group
      };

      //Topology
      for (var i = 0; i < spring.Value.Count(); i += 3)
      {
        ls.Add(Initialiser.AppResources.Proxy.NodeAt(spring.Value[i], spring.Value[i + 1], spring.Value[i + 2], Initialiser.AppResources.Settings.CoincidentNodeAllowance).ToString());
      }

      ls.Add("0"); // Orientation Node
      ls.Add("0"); //Angle
      ls.Add("NO_RLS"); //is_rls

      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      ls.Add(""); // parent_member

      ls.Add((spring.Dummy.HasValue && spring.Dummy.Value) ? "DUMMY" : "");

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DSpring spring)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(spring, () =>
      {
        var group = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA0DSpring).GetGSAKeyword(), spring.ApplicationId);
        return new GSA0DSpring() { Value = spring }.SetGWACommand(Initialiser.AppResources.Proxy, group);
      });
    }

    //Sending to Speckle, search through a
    public static SpeckleObject ToSpeckle(this GSA0DSpring dummyObject)
    {
      var typeName = dummyObject.GetType().Name;
      var newSpringLines = ToSpeckleBase<GSA0DSpring>();
      var newNodeLines = ToSpeckleBase<GSANode>();
      var newLines = new List<Tuple<int, string>>();
      var keyword = dummyObject.GetGSAKeyword();

      foreach (var k in newSpringLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newSpringLines[k]));
      }
      foreach (var k in newNodeLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newNodeLines[k]));
      }

      var springsLock = new object();
      var springs = new List<GSA0DSpring>();

      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      foreach (var p in newLines.Select(nl => nl.Item2))
      {
        var pPieces = p.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        if (pPieces[4] == "GRD_SPRING")
        {
          var gsaId = pPieces[1];
          var spring = new GSA0DSpring() { GWACommand = p };
          try
          {
            spring.ParseGWACommand(nodes);
            lock (springsLock)
            {
              springs.Add(spring);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + pPieces[1]);
          }
        }
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(springs);

      return (springs.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
