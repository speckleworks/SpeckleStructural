using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_2D_THERMAL.2", new string[] { }, "model", true, true, new Type[] { typeof(GSA2DElement), typeof(GSA2DMember), typeof(GSALoadCase) }, new Type[] { typeof(GSA2DElement), typeof(GSA2DMember), typeof(GSALoadCase) })]
  public class GSA2DThermalLoading : GSABase<Structural2DThermalLoad>
  {
    public void ParseGWACommand(List<GSA2DElement> e2Ds, List<GSA2DMember> m2Ds)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DThermalLoad();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      
      obj.Name = pieces[counter++];
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);

      var elementList = pieces[counter++];

      obj.ElementRefs = new List<string>();

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        var elementId = Initialiser.AppResources.Proxy.ConvertGSAList(elementList, GSAEntity.ELEMENT);
        foreach (var id in elementId)
        {
          var elem = e2Ds.Where(e => e.GSAId == id).Select(e => (IGSAContainer<Structural2DElement>)e).FirstOrDefault();

          if (elem == null)
            continue;

          obj.ElementRefs.Add(((SpeckleObject)elem.Value).ApplicationId);
          this.SubGWACommand.Add(elem.GWACommand);
        }
      }
      else
      {
        var groupIds = Helper.GetGroupsFromGSAList(elementList).ToList();
        foreach (var id in groupIds)
        {
          var memb2Ds = m2Ds.Where(m => m.Group == id);

          obj.ElementRefs.AddRange(memb2Ds.Select(m => (m.Value).ApplicationId));
          this.SubGWACommand.AddRange(memb2Ds.Select(m => m.GWACommand));
        }
      }

      obj.ElementRefs.Sort();

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var loadingType = pieces[counter++];

      switch (loadingType)
      {
        case "CONS":
          obj.TopTemperature = Convert.ToDouble(pieces[counter++]);
          obj.BottomTemperature = obj.TopTemperature;
          break;
        case "DZ":
          obj.TopTemperature = Convert.ToDouble(pieces[counter++]);
          obj.BottomTemperature = Convert.ToDouble(pieces[counter++]);
          break;
        case "GEN":
          // GENERALIZE THIS TO AN AVERAGE
          for (var i = 0; i < 3; i++)
          { 
            obj.TopTemperature += Convert.ToDouble(pieces[counter++]);
            obj.BottomTemperature += Convert.ToDouble(pieces[counter++]);
          }
          obj.TopTemperature /= 4;
          obj.BottomTemperature /= 4;
          break;
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural2DThermalLoad;

      if (load.ApplicationId == null)
      {
        return "";
      }

      var keyword = typeof(GSA2DThermalLoading).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA2DThermalLoading).GetGSAKeyword(), load.ApplicationId);

      var targetString = " ";

      if (load.ElementRefs != null && load.ElementRefs.Count() > 0)
      {
        if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          var e2DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          var e2DMeshIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          targetString = string.Join(" ",
            e2DIndices.Select(x => x.ToString())
            .Concat(e2DMeshIndices.Select(x => "G" + x.ToString())).OrderBy(i => i));
        }
        else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
        {
          var m2DIndices = Initialiser.AppResources.Cache.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
          targetString = string.Join(" ",
            m2DIndices.Select(x => "G" + x.ToString()).OrderBy(i => i));
        }
      }

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

      if (indexResult == null && load.ApplicationId != null)
      {
        if (load.LoadCaseRef == null)
        {
          Helper.SafeDisplay("Blank load case references found for these Application IDs:", load.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Load case references not found:", load.ApplicationId + " referencing " + load.LoadCaseRef);
        }
      }

      var loadingName = string.IsNullOrEmpty(load.Name) ? " " : load.Name;

      var sid = Helper.GenerateSID(load);
      var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          loadingName, // Name
          targetString, //Elements
					loadCaseRef.ToString(),
        };

      if (load.TopTemperature == load.BottomTemperature)
      {
        ls.Add("CONS");
        ls.Add(load.TopTemperature.ToString());
      }
      else
      {
        ls.Add("DZ");
        ls.Add(load.TopTemperature.ToString());
        ls.Add(load.BottomTemperature.ToString());
      }

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DThermalLoad load)
    {
      return new GSA2DThermalLoading() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DThermalLoading dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DThermalLoading>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSA2DThermalLoading>();
      var e2Ds = new List<GSA2DElement>();
      var m2Ds = new List<GSA2DMember>();

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        e2Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DElement>().ToList();
      }
      else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
      {
        m2Ds = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DMember>();
      }

      foreach (var k in newLines.Keys)
      {
        var load = new GSA2DThermalLoading() { GSAId = k, GWACommand = newLines[k] };
        try
        {
          load.ParseGWACommand(e2Ds, m2Ds);
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
        loads.Add(load);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0 ) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
