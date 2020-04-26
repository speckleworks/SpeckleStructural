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

    public void ParseGWACommand(List<GSANode> nodes, List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralAssembly();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      var targetEntity = pieces[counter++];

      var targetList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = Initialiser.Interface.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.MEMBER);
          var match1D = e1Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          var match2D = e2Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          var elementRefs = obj.ElementRefs;
          elementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
        else if (targetEntity == "ELEMENT")
        {
          var elementList = Initialiser.Interface.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.ELEMENT);
          var match1D = e1Ds.Where(e => elementList.Contains(e.GSAId));
          var match2D = e2Ds.Where(e => elementList.Contains(e.GSAId));
          var elementRefs = obj.ElementRefs;
          elementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = Initialiser.Interface.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.MEMBER);
          var match1D = m1Ds.Where(e => memberList.Contains(e.GSAId));
          var match2D = m2Ds.Where(e => memberList.Contains(e.GSAId));
          var elementRefs = obj.ElementRefs;
          elementRefs.AddRange(match1D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => (e.Value as SpeckleObject).ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => (e as IGSASpeckleContainer).GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => (e as IGSASpeckleContainer).GWACommand));
        }
        else if (targetEntity == "ELEMENT")
        {
          //Unlike all other classes, the layer relevant to sending is only determined by looking at a GWA parameter rather than a class attribute.
          //Once this condition has been met, assign to null so it won't form part of the sender objects list
          Value = null;
          return;
        }
      }

      obj.Value = new List<double>();
      for (var i = 0; i < 2; i++)
      {
        var key = pieces[counter++];
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }
      var orientationNodeId = Convert.ToInt32(pieces[counter++]);
      var orientationNode = nodes.Where(n => n.GSAId == orientationNodeId).FirstOrDefault();
      this.SubGWACommand.Add(orientationNode.GWACommand);
      obj.OrientationPoint = new SpecklePoint(orientationNode.Value.Value[0], orientationNode.Value.Value[1], orientationNode.Value.Value[2]);

      counter++; // Internal topology
      obj.Width = (Convert.ToDouble(pieces[counter++]) + Convert.ToDouble(pieces[counter++])) / 2;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSAAssembly);

      var assembly = this.Value as StructuralAssembly;

      if (assembly.Value == null || assembly.Value.Count() == 0)
        return "";

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, assembly.ApplicationId);

      var targetString = " ";

      if (assembly.ElementRefs != null && assembly.ElementRefs.Count() > 0)
      {
        var polylineIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          var e1DIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(e1DIndices);
          indices.AddRange(e2DIndices);
          indices.AddRange(e2DMeshIndices);
          indices = indices.Distinct().ToList();

          targetString = string.Join(" ", indices.Select(x => x.ToString()));
        }
        else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
        {
          var m1DIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m2DIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), assembly.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(m1DIndices);
          indices.AddRange(m2DIndices);
          indices = indices.Distinct().ToList();

          // TODO: Once assemblies can properly target members, this should target members explicitly
          targetString = string.Join(" ", indices.Select(i => "G" + i.ToString()));
        }
      }

      var nodeIndices = new List<int>();
      for (var i = 0; i < assembly.Value.Count(); i += 3)
      {
        nodeIndices.Add(Helper.NodeAt(assembly.Value[i], assembly.Value[i + 1], assembly.Value[i + 2], Initialiser.Settings.CoincidentNodeAllowance));
      }

      //The width parameter is intentionally not being used here as the meaning doesn't map to the y coordinate parameter of the ASSEMBLY keyword
      //It is therefore to be ignored here for GSA purposes.
      var orientationPoint = (assembly.OrientationPoint == null || assembly.OrientationPoint.Value == null || assembly.OrientationPoint.Value.Count < 3)
        ? new SpecklePoint(0, 0, 0)
        : assembly.OrientationPoint;

      var ls = new List<string>
        {
          "SET",
          keyword + ":" + Helper.GenerateSID(assembly),
          index.ToString(),
          string.IsNullOrEmpty(assembly.Name) ? "" : assembly.Name,
          // TODO: Once assemblies can properly target members, this should target members explicitly
          //Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis ? "ELEMENT" : "MEMBER",
          "ELEMENT",
          targetString,
          nodeIndices[0].ToString(),
          nodeIndices[1].ToString(),
          Helper.NodeAt(orientationPoint.Value[0], orientationPoint.Value[1], orientationPoint.Value[2], Initialiser.Settings.CoincidentNodeAllowance).ToString(),
          "", //Empty list for int_topo as it assumed that the line is never curved
          assembly.Width.ToString(), //Y
          "0", //Z
          "LAGRANGE",
          "0" //Curve order - reserved for future use according to the documentation
      };

      if (assembly.NumPoints.HasValue)
      {
        var numPoints = (assembly.NumPoints == 0) ? 10 : assembly.NumPoints;
        ls.AddRange(new[] { "POINTS",
          numPoints.ToString() //Number of points
        });
      }
      else if (assembly.PointDistances != null && assembly.PointDistances.Count() > 0)
      {
        ls.AddRange(new[] { 
          "EXPLICIT",
          string.Join(" ", assembly.PointDistances)
        });
      }

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralAssembly assembly)
    {
      return new GSAAssembly() { Value = assembly }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAAssembly dummyObject)
    {
      var newLines = ToSpeckleBase<GSAAssembly>();

      //Get all relevant GSA entities in this entire model
      var assemblies = new List<GSAAssembly>();
      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();
      var e1Ds = new List<GSA1DElement>();
      var e2Ds = new List<GSA2DElement>();
      var m1Ds = new List<GSA1DMember>();
      var m2Ds = new List<GSA2DMember>();

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        e1Ds = Initialiser.GSASenderObjects.Get<GSA1DElement>();
        e2Ds = Initialiser.GSASenderObjects.Get<GSA2DElement>();
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        m1Ds = Initialiser.GSASenderObjects.Get<GSA1DMember>();
        m2Ds = Initialiser.GSASenderObjects.Get<GSA2DMember>();
      }

      foreach (var p in newLines.Values)
      {
        try
        {
          var assembly = new GSAAssembly() { GWACommand = p };
          //Pass in ALL the nodes and members - the Parse_ method will search through them
          assembly.ParseGWACommand(nodes, e1Ds, e2Ds, m1Ds, m2Ds);

          //This ties into the note further above:
          //Unlike all other classes, the layer relevant to sending is only determined by looking at a GWA parameter rather than a class attribute.
          //Once this condition has been met, assign to null so it won't form part of the sender objects list
          if (assembly.Value != null)
          {
            assemblies.Add(assembly);
          }
        }
        catch { }
      }

      Initialiser.GSASenderObjects.AddRange(assemblies);

      return (assemblies.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
