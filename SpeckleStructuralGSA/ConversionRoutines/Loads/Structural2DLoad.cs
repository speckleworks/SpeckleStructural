using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  public abstract class GSA2DLoadBase
  {
    public int Axis; // Store this temporarily to generate other loads
    public bool Projected;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DLoad();

    protected void ParseGWACommand(List<GSA2DElement> elements, List<GSA2DMember> members)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DLoad();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        var targetElements = Initialiser.Interface.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.ELEMENT);

        if (elements != null)
        {
          var elems = elements.Where(n => targetElements.Contains(n.GSAId)).ToList();

          obj.ElementRefs = elems.Select(n => (string)n.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(elems.Select(n => n.GWACommand));
        }
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        var targetGroups = Helper.GetGroupsFromGSAList(pieces[counter++]);

        if (members != null)
        {
          var membs = members.Where(m => targetGroups.Contains(m.Group)).ToList();

          obj.ElementRefs = membs.Select(m => (string)m.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(membs.Select(n => n.GWACommand));
        }
      }

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

      obj.AxisType = (axis == "LOCAL") ? StructuralLoadAxisType.Local : StructuralLoadAxisType.Global;

      counter++; // Type. TODO: Skipping since we're taking the average

      this.Projected = pieces[counter++] == "YES";

      obj.Loading = new StructuralVectorThree(new double[3]);
      var direction = pieces[counter++].ToLower();

      var values = pieces.Skip(counter).Select(p => Convert.ToDouble(p)).ToArray();

      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = values.Average();
          break;
        case "Y":
          obj.Loading.Value[1] = values.Average();
          break;
        case "Z":
          obj.Loading.Value[2] = values.Average();
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }

    protected string SetGWACommand(string keyword)
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural2DLoad;

      if (load.Loading == null)
        return "";

      List<int> elementRefs;
      List<int> groupRefs;

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        elementRefs = Initialiser.Cache.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = Initialiser.Cache.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        elementRefs = new List<int>();
        groupRefs = Initialiser.Cache.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else
      {
        return "";
      }

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

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

      var direction = new string[3] { "X", "Y", "Z" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Cache.ResolveIndex(keyword);

        var sid = Helper.GenerateSID(load);
        var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          load.Name == null || load.Name == "" ? " " : load.Name + (load.Name.All(char.IsDigit) ? " " : ""),
          // TODO: This is a hack.
          string.Join(" ", elementRefs.Select(x => x.ToString()).Concat(groupRefs.Select(x => "G" + x.ToString()))),
          loadCaseRef.ToString(),
          load.AxisType == StructuralLoadAxisType.Local ? "LOCAL" : "GLOBAL", // Axis
          "CONS", // Type
          "NO", // Projected
          direction[i],
          load.Loading.Value[i].ToString()
        };

        gwaCommands.Add(string.Join("\t", ls));
      }

      return string.Join("\n", gwaCommands);
    }
  }

  [GSAObject("LOAD_2D_FACE.2", new string[] { "EL.4" }, "model", true, false, new Type[] { typeof(GSA2DElementMesh), typeof(GSA2DElement) }, new Type[] { typeof(GSA2DElement), typeof(GSA2DElementMesh) })]
  public class GSA2DLoadAnalysisLayer : GSA2DLoadBase, IGSASpeckleContainer
  {
    public void ParseGWACommand(List<GSA2DElement> elements)
    {
      base.ParseGWACommand(elements, new List<GSA2DMember>());
    }

    public string SetGWACommand()
    {
      return base.SetGWACommand(typeof(GSA2DLoadAnalysisLayer).GetGSAKeyword());
    }
  }

  [GSAObject("LOAD_2D_FACE.2", new string[] { "MEMB.8" }, "model", false, true, new Type[] {typeof(GSA2DMember) }, new Type[] {typeof(GSA2DMember)})]
  public class GSA2DLoadDesignLayer : GSA2DLoadBase, IGSASpeckleContainer
  {
    public void ParseGWACommand(List<GSA2DMember> members)
    {
      base.ParseGWACommand(new List<GSA2DElement>(), members);
    }

    public string SetGWACommand()
    {
      return base.SetGWACommand(typeof(GSA2DLoadDesignLayer).GetGSAKeyword());
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DLoad load)
    {
      return (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        ? new GSA2DLoadAnalysisLayer() { Value = load }.SetGWACommand()
        : new GSA2DLoadDesignLayer() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DLoadAnalysisLayer dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DLoadAnalysisLayer>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSA2DLoadAnalysisLayer>();
      var elements = Initialiser.GSASenderObjects.Get<GSA2DElement>();
      var loadLock = new object();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var p = newLines[k];
        var loadSubList = new List<GSA2DLoadAnalysisLayer>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA2DLoadAnalysisLayer() { GWACommand = p, GSAId = k };
        try
        {
          initLoad.ParseGWACommand(elements);
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }

        lock (loadLock)
        {
          loads.Add(initLoad);
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA2DLoadDesignLayer dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DLoadDesignLayer>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSA2DLoadDesignLayer>();
      var members = Initialiser.GSASenderObjects.Get<GSA2DMember>();
      var loadLock = new object();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var p = newLines[k];
        var loadSubList = new List<GSA2DLoadDesignLayer>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA2DLoadDesignLayer() { GWACommand = p, GSAId = k };
        try
        {
          initLoad.ParseGWACommand(members);
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }

        lock (loadLock)
        {
          loads.Add(initLoad);
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
