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
  [GSAObject("EL.4", new string[] { "NODE.2" }, "elements", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSASpringProperty) })]
  public class GSA1DSpring : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DSpring();

    public void ParseGWACommand(GSAInterfacer GSA, List<GSANode> nodes)
    {
      if (this.GWACommand == null)
        return;

      Structural1DSpring obj = new Structural1DSpring();

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

      if (orientationNodeRef != "0")
      {
        GSANode node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(),
            rotationAngle, node.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
        obj.ZAxis = GSA.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;
      
      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA, int group = 0)
    {
      if (this.Value == null)
        return;

      Structural1DSpring spring = this.Value as Structural1DSpring;

      string keyword = typeof(GSA1DSpring).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA1DSpring), spring);
      int propRef = 0;
      try
      {
        propRef = GSA.Indexer.LookupIndex(typeof(GSASpringProperty), spring.PropertyRef).Value;
      }
      catch { }

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(spring));
      ls.Add(index.ToString());
      ls.Add(spring.Name == null || spring.Name == "" ? " " : spring.Name);
      ls.Add("NO_RGB");
      ls.Add("SPRING"); // Type
      ls.Add(propRef.ToString());
      ls.Add(group.ToString());
      for (int i = 0; i < spring.Value.Count(); i += 3)
        ls.Add(GSA.NodeAt(spring.Value[i], spring.Value[i + 1], spring.Value[i + 2], Conversions.GSACoincidentNodeAllowance).ToString());
      ls.Add("0"); // Orientation Node
      try
      {
        ls.Add(GSA.Get1DAngle(spring.Value.ToArray(), spring.ZAxis).ToString());
      }
      catch { ls.Add("0"); }
      ls.Add("NO_RLS");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      //ls.Add("NORMAL"); // Action // TODO: EL.4 SUPPORT
      ls.Add(spring.GSADummy ? "DUMMY" : "");

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private static bool ParseEndRelease(char code, string[] pieces, ref int counter)
    {
      switch (code)
      {
        case 'F':
          return false;
        case 'R':
          return true;
        default:
          // TODO
          counter++;
          return true;
      }
    }
  }
  
  public static partial class Conversions
  {
    public static bool ToNative(this Structural1DSpring spring)
    {
      new GSA1DSpring() { Value = spring }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DSpring dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DSpring)))
        GSASenderObjects[typeof(GSA1DSpring)] = new List<object>();

      List<GSA1DSpring> springs = new List<GSA1DSpring>();
      List<GSANode> nodes = GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();

      string keyword = typeof(GSA1DSpring).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DSpring).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA1DSpring)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA1DSpring)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        string[] pPieces = p.ListSplit("\t");
        if ( pPieces[4] == "SPRING" )
        {
          try
          {
            GSA1DSpring spring = new GSA1DSpring() { GWACommand = p };
            spring.ParseGWACommand(GSA, nodes);
            springs.Add(spring);
          }
          catch { }
        }
      }

      GSASenderObjects[typeof(GSA1DSpring)].AddRange(springs);

      if (springs.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
