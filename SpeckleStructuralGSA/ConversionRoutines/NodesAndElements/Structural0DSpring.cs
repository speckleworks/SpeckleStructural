using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("EL.4", new string[] { "NODE.2" }, "elements", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA0DSpring : IGSASpeckleContainer
  {
    public string Member;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DSpring();

    //Sending
    public void ParseGWACommand(List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DSpring();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSASpringProperty).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      counter++; // Group

      obj.Value = new List<double>();

      var key = pieces[counter++];
      if (int.TryParse(key, out int nodeIndex))
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

      //counter++; // Action // TODO: EL.4 SUPPORT
      counter++; // Dummy

      if (counter < pieces.Length)
      {
        Member = pieces[counter++];
      }
      this.Value = obj;
    }

    public string SetGWACommand(IGSAProxy GSA, int group = 0)
    {
      if (this.Value == null)
        return "";

      var spring = this.Value as Structural0DSpring;

      var keyword = typeof(GSA0DSpring).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, spring.ApplicationId);

      var propKeyword = typeof(GSASpringProperty).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(propKeyword, spring.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.Cache.ResolveIndex(propKeyword, spring.PropertyRef);
      if (indexResult == null && spring.ApplicationId != null)
      {
        if (spring.PropertyRef == null)
        {
          Helper.SafeDisplay("Blank property references found for these Application IDs:", spring.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Property references not found:", spring.ApplicationId + " referencing " + spring.PropertyRef);
        }
      }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(spring),
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
        ls.Add(Helper.NodeAt(spring.Value[i], spring.Value[i + 1], spring.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString());
      }

      ls.Add("0"); // Orientation Node
      ls.Add("0"); //Angle
      ls.Add("NO_RLS"); //is_rls

      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      //ls.Add("NORMAL"); // Action // TODO: EL.4 SUPPORT
      ls.Add((spring.Dummy.HasValue && spring.Dummy.Value) ? "DUMMY" : "");

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural0DSpring spring)
    {
      var group = Initialiser.Cache.ResolveIndex(typeof(GSA0DSpring).GetGSAKeyword(), spring.ApplicationId);
      return new GSA0DSpring() { Value = spring }.SetGWACommand(Initialiser.Interface, group);
    }

    //Sending to Speckle, search through a
    public static SpeckleObject ToSpeckle(this GSA0DSpring dummyObject)
    {
      var newSpringLines = ToSpeckleBase<GSA0DSpring>();
      var newNodeLines = ToSpeckleBase<GSANode>();
      var newLines = new List<Tuple<int, string>>();
      foreach (var k in newSpringLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newSpringLines[k]));
      }
      foreach (var k in newNodeLines.Keys)
      {
        newLines.Add(new Tuple<int, string>(k, newNodeLines[k]));
      }

      var springs = new List<GSA0DSpring>();

      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      foreach (var p in newLines.Select(nl => nl.Item2))
      {
        var pPieces = p.ListSplit("\t");
        if (pPieces[4] == "GRD_SPRING")
        {
          var spring = new GSA0DSpring() { GWACommand = p };
          spring.ParseGWACommand(nodes);
          springs.Add(spring);
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA0DSpring)].AddRange(springs);

      return (springs.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
