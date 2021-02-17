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
  [GSAObject("ANAL_STAGE.3", new string[] { "LIST.1" }, "model", true, true, new Type[] { typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) }, new Type[] { typeof(GSA1DElement), typeof(GSA2DElement), typeof(GSA1DMember), typeof(GSA2DMember) })]
  public class GSAConstructionStage : GSABase<StructuralConstructionStage>
  {
    public void ParseGWACommand(List<GSA1DElement> e1Ds, List<GSA2DElement> e2Ds, List<GSA1DMember> m1Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralConstructionStage();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      counter++; //Skip colour

      var elementList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        var elementId = Initialiser.AppResources.Proxy.ConvertGSAList(elementList, GSAEntity.ELEMENT);
        foreach (var id in elementId)
        {
          string elementApplicationId = null;
          string elementGwaCommand = "";
          var elem1d = (IGSAContainer<Structural1DElement>)e1Ds.Where(e => e.GSAId == id).FirstOrDefault();

          if (elem1d == null)
          {
            var elem2d = (IGSAContainer<Structural2DElement>)e2Ds.Where(e => e.GSAId == id).FirstOrDefault();
            if (elem2d != null)
            {
              elementApplicationId = elem2d.Value.ApplicationId;
              elementGwaCommand = elem2d.GWACommand;
            }
          }
          else
          {
            elementApplicationId = elem1d.Value.ApplicationId;
            elementGwaCommand = elem1d.GWACommand;
          }
          if (elementApplicationId != null)
          {
            obj.ElementRefs.Add(elementApplicationId);
            this.SubGWACommand.Add(elementGwaCommand);
          }
        }
      }
      else
      {
        var groupIds = Helper.GetGroupsFromGSAList(elementList).ToList();
        foreach (var id in groupIds)
        {
          var memb1Ds = m1Ds.Where(m => m.Group == id);
          var memb2Ds = m2Ds.Where(m => m.Group == id);

          obj.ElementRefs.AddRange(memb1Ds.Select(m => m.Value.ApplicationId));
          obj.ElementRefs.AddRange(memb2Ds.Select(m => m.Value.ApplicationId));
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
      if (string.IsNullOrEmpty(stageDef.ApplicationId))
      {
        return "";
      }

      var sourceType = stageDef.Type;

      var keyword = typeof(GSAConstructionStage).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSAConstructionStage).GetGSAKeyword(), stageDef.ApplicationId);
      
      var targetString = " ";

      if (stageDef.ElementRefs != null && stageDef.ElementRefs.Count() > 0)
      {
        if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          var e1DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e1DPolyIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(e1DIndices);
          indices.AddRange(e1DPolyIndices);
          indices.AddRange(e2DIndices);
          indices.AddRange(e2DMeshIndices);
          indices = indices.Distinct().OrderBy(i => i).ToList();

          targetString = string.Join(" ", indices.Select(x => x.ToString()));
        }
        else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
        {
          var m1DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var m2DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), stageDef.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();

          var indices = new List<int>(m1DIndices);
          indices.AddRange(m2DIndices);
          indices = indices.Distinct().OrderBy(i => i).ToList();

          targetString = string.Join(" ", indices.Select(i => "G" + i.ToString()));
        }
      }

      var stageName = string.IsNullOrEmpty(stageDef.Name) ? " " : stageDef.Name;

      var sid = Helper.GenerateSID(stageDef);
      var ls = new List<string>
        {
          // Set ANAL_STAGE
          "SET",
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          index.ToString(),
          stageName, // Name
          "NO_RGB", // Colour
          targetString, //Elements by group name
          "0", //Creep factor
          stageDef.StageDays.ToString(), // Stage
          "none"
        };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
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
      var typeName = dummyObject.GetType().Name;
      var stageDefsLock = new object();
      var stageDefs = new SortedDictionary<int, GSAConstructionStage>();
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
          var stageDef = new GSAConstructionStage() { GWACommand = newLines[k] };
          stageDef.ParseGWACommand(e1Ds, e2Ds, m1Ds, m2Ds);
          lock (stageDefsLock)
          {
            stageDefs.Add(k, stageDef);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString());
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(stageDefs.Values.ToList());

      return (stageDefs.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
