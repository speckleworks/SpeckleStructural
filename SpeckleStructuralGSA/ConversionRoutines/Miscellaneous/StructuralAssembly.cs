using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ASSEMBLY.3", new string[] { }, "misc", true, true, new Type[] { typeof(GSANode), typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) }, new Type[] { typeof(GSANode), typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) })]
  public class GSAAssembly : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralAssembly();

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSANode> nodes, List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralAssembly();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Interface.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      var targetEntity = pieces[counter++];

      var targetList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = GSA.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.MEMBER);
          var match1D = e1Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          var match2D = e2Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          obj.ElementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
        else if (targetEntity == "ELEMENT")
        {
          var elementList = GSA.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.ELEMENT);
          var match1D = e1Ds.Where(e => elementList.Contains(e.GSAId));
          var match2D = e2Ds.Where(e => elementList.Contains(e.GSAId));
          obj.ElementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = GSA.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.MEMBER);
          var match1D = m1Ds.Where(e => memberList.Contains(e.GSAId));
          var match2D = m2Ds.Where(e => memberList.Contains(e.GSAId));
          obj.ElementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
        else if (targetEntity == "ELEMENT")
          return;
      }

      obj.Value = new List<double>();
      for (int i = 0; i < 2; i++)
      {
        string key = pieces[counter++];
        GSANode node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }
      var orientationNodeId = Convert.ToInt32(pieces[counter++]);
      GSANode orientationNode = nodes.Where(n => n.GSAId == orientationNodeId).FirstOrDefault();
      this.SubGWACommand.Add(orientationNode.GWACommand);
      obj.OrientationPoint = new SpecklePoint(orientationNode.Value.Value[0], orientationNode.Value.Value[1], orientationNode.Value.Value[2]);

      counter++; // Internal topology
      obj.Width = (Convert.ToDouble(pieces[counter++]) + Convert.ToDouble(pieces[counter++])) / 2;

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Type destType = typeof(GSAAssembly);

      StructuralAssembly assembly = this.Value as StructuralAssembly;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(keyword, assembly.ApplicationId);

      var targetString = " ";

      if (assembly.ElementRefs != null && assembly.ElementRefs.Count() > 0)
      {
        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        {
          var e1DIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e1DPolyIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DIndices = GSA.Indexer.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = GSA.Indexer.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          targetString = string.Join(" ",
            e1DIndices.Select(x => x.ToString())
            .Concat(e1DPolyIndices.Select(x => "G" + x.ToString()))
            .Concat(e2DIndices.Select(x => x.ToString()))
            .Concat(e2DMeshIndices.Select(x => "G" + x.ToString()))
          );
        }
        else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        {
          var m1DIndices = GSA.Indexer.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m1DPolyIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m2DIndices = GSA.Indexer.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          // TODO: Once assemblies can properly target members, this should target members explicitly
          targetString = string.Join(" ",
            m1DIndices.Select(x => "G" + x.ToString())
            .Concat(m1DPolyIndices.Select(x => "G" + x.ToString()))
            .Concat(m2DIndices.Select(x => "G" + x.ToString()))
          );
        }
      }

      List<int> nodeIndices = new List<int>();
      for (int i = 0; i < assembly.Value.Count(); i += 3)
        nodeIndices.Add(HelperClass.NodeAt(GSA, assembly.Value[i], assembly.Value[i + 1], assembly.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance));

      var numPoints = (assembly.NumPoints == 0) ? 10 : assembly.NumPoints;

      //The width parameter is intentionally not being used here as the meaning doesn't map to the y coordinate parameter of the ASSEMBLY keyword
      //It is therefore to be ignored here for GSA purposes.

      List<string> ls = new List<string>
        {
          "SET",
          keyword + ":" + HelperClass.GenerateSID(assembly),
          index.ToString(),
          string.IsNullOrEmpty(assembly.Name) ? "" : assembly.Name,
          // TODO: Once assemblies can properly target members, this should target members explicitly
          //Conversions.GSATargetLayer == GSATargetLayer.Analysis ? "ELEMENT" : "MEMBER",
          "ELEMENT",
          targetString,
          nodeIndices[0].ToString(),
          nodeIndices[1].ToString(),
          HelperClass.NodeAt(GSA, assembly.OrientationPoint.Value[0], assembly.OrientationPoint.Value[1], assembly.OrientationPoint.Value[2], Initialiser.Settings.CoincidentNodeAllowance).ToString(),
          "", //Empty list for int_topo as it assumed that the line is never curved
          assembly.Width.ToString(), //Y
          "0", //Z
          "LAGRANGE",
          "0", //Curve order - reserved for future use according to the documentation
          "POINTS",
          numPoints.ToString() //Number of points
        };

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralAssembly assembly)
    {
      new GSAAssembly() { Value = assembly }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSAAssembly dummyObject)
    {
      Type objType = dummyObject.GetType();

      if (!Initialiser.GSASenderObjects.ContainsKey(objType))
        Initialiser.GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var assemblies = new List<GSAAssembly>();
      var nodes = Initialiser.GSASenderObjects[typeof(GSANode)].Cast<GSANode>().ToList();
      var e1Ds = new List<GSA1DElement>();
      var e2Ds = new List<GSA2DElement>();
      var m1Ds = new List<GSA1DMember>();
      var m2Ds = new List<GSA2DMember>();

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        e1Ds = Initialiser.GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList();
        e2Ds = Initialiser.GSASenderObjects[typeof(GSA2DElement)].Cast<GSA2DElement>().ToList();
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        m1Ds = Initialiser.GSASenderObjects[typeof(GSA1DMember)].Cast<GSA1DMember>().ToList();
        m2Ds = Initialiser.GSASenderObjects[typeof(GSA2DMember)].Cast<GSA2DMember>().ToList();
      }

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          GSAAssembly assembly = new GSAAssembly() { GWACommand = p };
          //Pass in ALL the nodes and members - the Parse_ method will search through them
          assembly.ParseGWACommand(Initialiser.Interface, nodes, e1Ds, e2Ds, m1Ds, m2Ds);
          assemblies.Add(assembly);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[objType].AddRange(assemblies);

      if (assemblies.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
