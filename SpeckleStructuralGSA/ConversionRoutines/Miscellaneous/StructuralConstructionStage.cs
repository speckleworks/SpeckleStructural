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

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      StructuralConstructionStage obj = new StructuralConstructionStage();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      counter++; //Skip colour

      var elementList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        var elementId = GSA.ConvertGSAList(elementList, SpeckleGSAInterfaces.GSAEntity.ELEMENT);
        foreach (int id in elementId)
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
        var groupIds = HelperClass.GetGroupsFromGSAList(elementList).ToList();
        foreach (int id in groupIds)
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

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralConstructionStage stageDef = this.Value as StructuralConstructionStage;

      string keyword = typeof(GSAConstructionStage).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSAConstructionStage).GetGSAKeyword(), stageDef.ApplicationId);
      
      var targetString = " ";

      if (stageDef.ElementRefs != null && stageDef.ElementRefs.Count() > 0)
      {
        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        {
          var e1DIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e1DPolyIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DIndices = GSA.Indexer.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = GSA.Indexer.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          targetString = string.Join(" ",
            e1DIndices.Select(x => x.ToString())
            .Concat(e1DPolyIndices.Select(x => "G" + x.ToString()))
            .Concat(e2DIndices.Select(x => x.ToString()))
            .Concat(e2DMeshIndices.Select(x => "G" + x.ToString()))
          );
        }
        else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        {
          var m1DIndices = GSA.Indexer.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m1DPolyIndices = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m2DIndices = GSA.Indexer.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          targetString = string.Join(" ",
            m1DIndices.Select(x => "G" + x.ToString())
            .Concat(m1DPolyIndices.Select(x => "G" + x.ToString()))
            .Concat(m2DIndices.Select(x => "G" + x.ToString()))
          );
        }
      }

      var stageName = string.IsNullOrEmpty(stageDef.Name) ? " " : stageDef.Name;

      List<string> ls = new List<string>
        {
          // Set ANAL_STAGE
          "SET",
          keyword + ":" + HelperClass.GenerateSID(stageDef),
          index.ToString(),
          stageName, // Name
          "NO_RGB", // Colour
          targetString, //Elements by group name
          "0", //Creep factor
          stageDef.StageDays.ToString() // Stage
        };

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralConstructionStage stage)
    {
      var gsaStageDefinition = new GSAConstructionStage() { Value = stage };

      gsaStageDefinition.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSAConstructionStage dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSAConstructionStage)))
        GSASenderObjects[typeof(GSAConstructionStage)] = new List<object>();

      List<GSAConstructionStage> stageDefs = new List<GSAConstructionStage>();
      var e1Ds = new List<GSA1DElement>();
      var e2Ds = new List<GSA2DElement>();
      var m1Ds = new List<GSA1DMember>();
      var m2Ds = new List<GSA2DMember>();

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        e1Ds = GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList();
        e2Ds = GSASenderObjects[typeof(GSA2DElement)].Cast<GSA2DElement>().ToList();
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        m1Ds = GSASenderObjects[typeof(GSA1DMember)].Cast<GSA1DMember>().ToList();
        m2Ds = GSASenderObjects[typeof(GSA2DMember)].Cast<GSA2DMember>().ToList();
      }

      string keyword = typeof(GSAConstructionStage).GetGSAKeyword();
      string[] subKeywords = typeof(GSAConstructionStage).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSAConstructionStage)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSAConstructionStage)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSAConstructionStage stageDef = new GSAConstructionStage() { GWACommand = p };
        stageDef.ParseGWACommand(GSA, e1Ds, e2Ds, m1Ds, m2Ds);
        stageDefs.Add(stageDef);
      }

      GSASenderObjects[typeof(GSAConstructionStage)].AddRange(stageDefs);

      if (stageDefs.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
