using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL_STAGE.3", new string[] { "LIST.1" }, "misc", true, true, new Type[] { typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) }, new Type[] { typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) })]
  public class GSAConstructionStage : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralConstructionStage();

    public void ParseGWACommand(List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralConstructionStage();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      counter++; //Skip colour

      var elementList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        var elementId = Initialiser.Interface.ConvertGSAList(elementList, SpeckleGSAInterfaces.GSAEntity.ELEMENT);
        foreach (var id in elementId)
        {
          IGSASpeckleContainer elem = e1Ds.Where(e => e.GSAId == id).FirstOrDefault();

          if (elem == null)
            elem = e2Ds.Where(e => e.GSAId == id).FirstOrDefault();

          if (elem == null)
            continue;

          obj.ElementRefs.Add((elem.Value as SpeckleObject).ApplicationId);
          this.SubGWACommand.Add(elem.GWACommand);
        }
      }
      else
      {
        var groupIds = Helper.GetGroupsFromGSAList(elementList).ToList();
        foreach (var id in groupIds)
        {
          var memb1Ds = m1Ds.Where(m => m.Group == id);
          var memb2Ds = m2Ds.Where(m => m.Group == id);

          obj.ElementRefs.AddRange(memb1Ds.Select(m => (string)m.Value.ApplicationId));
          obj.ElementRefs.AddRange(memb2Ds.Select(m => (string)m.Value.ApplicationId));
          this.SubGWACommand.AddRange(memb1Ds.Select(m => m.GWACommand));
          this.SubGWACommand.AddRange(memb2Ds.Select(m => m.GWACommand));
        }
      }

      counter++; //Skip creep coefficient
      obj.StageDays = Convert.ToInt32(pieces[counter++]);

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var stageDef = this.Value as StructuralConstructionStage;

      var sourceType = stageDef.Type;

      var keyword = typeof(GSAConstructionStage).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSAConstructionStage).GetGSAKeyword(), stageDef.ApplicationId);
      
      var targetString = " ";

      if (stageDef.ElementRefs != null && stageDef.ElementRefs.Count() > 0)
      {
        if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          var e1DIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e1DPolyIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(e1DIndices);
          indices.AddRange(e1DPolyIndices);
          indices.AddRange(e2DIndices);
          indices.AddRange(e2DMeshIndices);
          indices = indices.Distinct().ToList();

          targetString = string.Join(" ", indices.Select(x => x.ToString()));
        }
        else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
        {
          var m1DIndices = Initialiser.Cache.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m2DIndices = Initialiser.Cache.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(m1DIndices);
          indices.AddRange(m2DIndices);
          indices = indices.Distinct().ToList();

          targetString = string.Join(" ", indices.Select(i => "G" + i.ToString()));
        }
      }

      var stageName = string.IsNullOrEmpty(stageDef.Name) ? " " : stageDef.Name;

      var ls = new List<string>
        {
          // Set ANAL_STAGE
          "SET",
          keyword + ":" + Helper.GenerateSID(stageDef),
          index.ToString(),
          stageName, // Name
          "NO_RGB", // Colour
          targetString, //Elements by group name
          "0", //Creep factor
          stageDef.StageDays.ToString() // Stage
        };

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralConstructionStage stage)
    {
      return (new GSAConstructionStage() { Value = stage }).SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAConstructionStage dummyObject)
    {
      var newLines = ToSpeckleBase<GSAConstructionStage>();

      var stageDefs = new List<GSAConstructionStage>();
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
          var stageDef = new GSAConstructionStage() { GWACommand = p };
          stageDef.ParseGWACommand(e1Ds, e2Ds, m1Ds, m2Ds);
          stageDefs.Add(stageDef);
        }
        catch { }
      }

      Initialiser.GSASenderObjects.AddRange(stageDefs);

      return (stageDefs.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
