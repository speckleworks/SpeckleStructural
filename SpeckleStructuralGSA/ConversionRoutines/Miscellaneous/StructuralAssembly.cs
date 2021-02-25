using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ASSEMBLY.3", new string[] { }, "model", true, true, new Type[] { typeof(GSANode), typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) }, new Type[] { typeof(GSANode), typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) })]
  public class GSAAssembly : GSABase<StructuralAssembly>
  {
    public void ParseGWACommand(List<GSANode> nodes, List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralAssembly();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      var targetEntity = pieces[counter++];

      var targetList = pieces[counter++];

      var elementRefs = obj.ElementRefs == null ? new List<string>() : obj.ElementRefs;

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = Initialiser.AppResources.Proxy.ConvertGSAList(targetList, GSAEntity.MEMBER);
          var match1D = e1Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          var match2D = e2Ds.Where(e => memberList.Contains(Convert.ToInt32(e.Member)));
          elementRefs.AddRange(match1D.Select(e => e.Value.ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => e.Value.ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => e.GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => e.GWACommand));
        }
        else if (targetEntity == "ELEMENT")
        {
          var elementList = Initialiser.AppResources.Proxy.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.ELEMENT);
          var match1D = e1Ds.Where(e => elementList.Contains(e.GSAId));
          var match2D = e2Ds.Where(e => elementList.Contains(e.GSAId));
          elementRefs.AddRange(match1D.Select(e => (e.Value).ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => (e.Value).ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => e.GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => e.GWACommand));
        }
      }
      else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
      {
        if (targetEntity == "MEMBER")
        {
          var memberList = Initialiser.AppResources.Proxy.ConvertGSAList(targetList, SpeckleGSAInterfaces.GSAEntity.MEMBER);
          var match1D = m1Ds.Where(e => memberList.Contains(e.GSAId));
          var match2D = m2Ds.Where(e => memberList.Contains(e.GSAId));
          elementRefs.AddRange(match1D.Select(e => ((Structural1DElement)e.Value).ApplicationId.ToString()));
          elementRefs.AddRange(match2D.Select(e => ((Structural2DElement)e.Value).ApplicationId.ToString()));
          obj.ElementRefs = elementRefs;
          this.SubGWACommand.AddRange(match1D.Select(e => e.GWACommand));
          this.SubGWACommand.AddRange(match2D.Select(e => e.GWACommand));
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
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSAAssembly dummyObject)
    {
      var newLines = ToSpeckleBase<GSAAssembly>();
      var typeName = dummyObject.GetType().Name;
      var assembliesLock = new object();
      var keyword = dummyObject.GetGSAKeyword();

      //Get all relevant GSA entities in this entire model
      var assemblies = new SortedDictionary<int, GSAAssembly>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var e1Ds = new List<GSA1DElement>();
      var e2Ds = new List<GSA2DElement>();
      var m1Ds = new List<GSA1DMember>();
      var m2Ds = new List<GSA2DMember>();

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        e1Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>();
        e2Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>();
      }
      else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
      {
        m1Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DMember>();
        m2Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DMember>();
      }

      Parallel.ForEach(newLines.Keys, k =>
      {
        try
        {
          var assembly = new GSAAssembly() { GWACommand = newLines[k], GSAId = k };
          //Pass in ALL the nodes and members - the Parse_ method will search through them
          assembly.ParseGWACommand(nodes, e1Ds, e2Ds, m1Ds, m2Ds);

          //This ties into the note further above:
          //Unlike all other classes, the layer relevant to sending is only determined by looking at a GWA parameter rather than a class attribute.
          //Once this condition has been met, assign to null so it won't form part of the sender objects list
          if (assembly.Value != null)
          {
            lock (assembliesLock)
            {
              assemblies.Add(k, assembly);
            }
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      }
      );

      Initialiser.GsaKit.GSASenderObjects.AddRange(assemblies.Values.ToList());

      return (assemblies.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
