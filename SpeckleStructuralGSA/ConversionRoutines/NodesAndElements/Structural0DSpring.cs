using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAConversion("EL.4", new string[] { "NODE.2" }, "elements", true, false, new Type[] { typeof(GSA0DSpring) }, new Type[] { typeof(GSA1DProperty) })]
  public class GSA0DSpring : IGSASpeckleContainer
  {
    public string Member;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural0DSpring();

    //Sending
    public void ParseGWACommand(IGSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural0DSpring();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      obj.PropertyRef = GSA.GetSID(typeof(GSASpringProperty).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));
      counter++; // Group

      obj.Value = new List<double>();
      for (int i = 0; i < 2; i++)
      {
        string key = pieces[counter++];
        GSANode node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      string orientationNodeRef = pieces[counter++];
      double rotationAngle = Convert.ToDouble(pieces[counter++]);

      //counter++; // Action // TODO: EL.4 SUPPORT
      counter++; // Dummy

      if (counter < pieces.Length)
        Member = pieces[counter++];

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA, int group = 0)
    {
      if (this.Value == null)
        return;

      var spring = this.Value as Structural0DSpring;

      string keyword = typeof(GSA0DSpring).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(keyword, spring.ApplicationId);
      int propRef = 0;
      try
      {
        propRef = GSA.Indexer.LookupIndex(typeof(GSASpringProperty).GetGSAKeyword(), spring.PropertyRef).Value;
      }
      catch { }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + HelperClass.GenerateSID(spring),
        index.ToString(),
        spring.Name == null || spring.Name == "" ? " " : spring.Name,
        "NO_RGB",
        "GRD_SPRING", //type
        propRef.ToString(), //Property
        group.ToString(), //Group
        //"1", //Group
      };

      //Topology
      for (int i = 0; i < spring.Value.Count(); i += 3)
      {
        ls.Add(HelperClass.NodeAt(GSA, spring.Value[i], spring.Value[i + 1], spring.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance).ToString());
      }

      ls.Add("0"); // Orientation Node
      ls.Add("0"); //Angle
      ls.Add("NO_RLS"); //is_rls

      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      //ls.Add("NORMAL"); // Action // TODO: EL.4 SUPPORT
      ls.Add(spring.Dummy ? "DUMMY" : "");

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }



  public static partial class Conversions
  {
    public static bool ToNative(this Structural0DSpring spring)
    {
      int group = Initialiser.Interface.Indexer.ResolveIndex(typeof(GSA0DSpring).GetGSAKeyword(), spring.ApplicationId);
      new GSA0DSpring() { Value = spring }.SetGWACommand(Initialiser.Interface, group);

      return true;
    }

    //Sending to Speckle, search through a
    public static SpeckleObject ToSpeckle(this GSA0DSpring dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA0DSpring)))
        Initialiser.GSASenderObjects[typeof(GSA0DSpring)] = new List<object>();

      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSANode)))
        Initialiser.GSASenderObjects[typeof(GSANode)] = new List<object>();

      List<GSA0DSpring> springs = new List<GSA0DSpring>();
      List<GSANode> nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA0DSpring).GetGSAKeyword();
      string[] subKeywords = typeof(GSA0DSpring).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSA0DSpring)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSA0DSpring)]
        .Select(l => (l as IGSASpeckleContainer).GWACommand)
        .Concat(Initialiser.GSASenderObjects[typeof(GSANode)].SelectMany(l => (l as IGSASpeckleContainer).SubGWACommand))
        .ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit("\t");
        if (pPieces[4] == "GRD_SPRING")
        {
          GSA0DSpring spring = new GSA0DSpring() { GWACommand = p };
          spring.ParseGWACommand(Initialiser.Interface, nodes);
          springs.Add(spring);
        }
      }

      Initialiser.GSASenderObjects[typeof(GSA0DSpring)].AddRange(springs);

      if (springs.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
