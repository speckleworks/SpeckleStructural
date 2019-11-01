using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_2D_FACE.2", new string[] { "EL.3", "MEMB.7" }, "loads", true, true, new Type[] { typeof(GSA2DElement), typeof(GSA2DMember) }, new Type[] { typeof(GSA2DElement), typeof(GSA2DMember), typeof(GSA2DElementMesh) })]
  public class GSA2DLoad : IGSASpeckleContainer
  {
    public int Axis; // Store this temporarily to generate other loads
    public bool Projected;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DLoad();

    public void ParseGWACommand(List<GSA2DElement> elements, List<GSA2DMember> members)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DLoad();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

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
        var targetGroups = HelperClass.GetGroupsFromGSAList(pieces[counter++]);

        if (members != null)
        {
          var membs = members.Where(m => targetGroups.Contains(m.Group)).ToList();

          obj.ElementRefs = membs.Select(m => (string)m.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(membs.Select(n => n.GWACommand));
        }
      }

      obj.LoadCaseRef = Initialiser.Indexer.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

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

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural2DLoad;

      if (load.Loading == null)
        return "";

      var keyword = typeof(GSA2DLoad).GetGSAKeyword();

      List<int> elementRefs;
      List<int> groupRefs;

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        elementRefs = Initialiser.Indexer.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), typeof(GSA2DElement).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = Initialiser.Indexer.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), typeof(GSA2DElementMesh).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        elementRefs = new List<int>();
        groupRefs = Initialiser.Indexer.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), typeof(GSA2DMember).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else
      {
        return "";
      }

      var loadCaseRef = 0;
      try
      {
        loadCaseRef = Initialiser.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).ToSpeckleTypeName(), load.LoadCaseRef).Value;
      }
      catch {
        loadCaseRef = Initialiser.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).ToSpeckleTypeName(), load.LoadCaseRef);
      }

      var direction = new string[3] { "X", "Y", "Z" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        var ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Indexer.ResolveIndex(typeof(GSA2DLoad).GetGSAKeyword(), typeof(GSA2DLoad).ToSpeckleTypeName());
        ls.Add("SET_AT");
        ls.Add(index.ToString());
        //ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(keyword + ":" + HelperClass.GenerateSID(load));
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        // TODO: This is a hack.
        ls.Add(string.Join(
            " ",
            elementRefs.Select(x => x.ToString())
                .Concat(groupRefs.Select(x => "G" + x.ToString()))
        ));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL"); // Axis
        ls.Add("CONS"); // Type
        ls.Add("NO"); // Projected
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        gwaCommands.Add(string.Join("\t", ls));
      }

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DLoad load)
    {
      return new GSA2DLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DLoad>();

      var loads = new List<GSA2DLoad>();
      var elements = (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis) ? Initialiser.GSASenderObjects[typeof(GSA2DElement)].Cast<GSA2DElement>().ToList() : new List<GSA2DElement>();
      var members = (Initialiser.Settings.TargetLayer == GSATargetLayer.Design) ? Initialiser.GSASenderObjects[typeof(GSA2DMember)].Cast<GSA2DMember>().ToList() : new List<GSA2DMember>();

      /*
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA2DLoad)))
        Initialiser.GSASenderObjects[typeof(GSA2DLoad)] = new List<object>();

      string keyword = typeof(GSA2DLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSA2DLoad).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSA2DLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSA2DLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();
      */

      foreach (var p in newLines)
      {
        var loadSubList = new List<GSA2DLoad>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA2DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(elements, members);

        if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            var load = new GSA2DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            var elem = elements.Where(e => e.Value.ApplicationId == nRef).First();
            StructuralAxis loadAxis = HelperClass.Parse2DAxis(elem.Value.Vertices.ToArray(), 0, load.Axis != 0); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;

            // Perform projection
            if (load.Projected)
            {
              load.Value.Loading.Value[0] = 0;
              load.Value.Loading.Value[1] = 0;
            }
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // If the loading already exists, add element ref to list
            var match = loadSubList.Count() > 0 ? loadSubList.Where(l => l.Value.Loading.Equals(load.Value.Loading)).First() : null;
            if (match != null)
              match.Value.ElementRefs.Add(nRef);
            else
            {
              load.Value.ElementRefs = new List<string>() { nRef };
              loadSubList.Add(load);
            }
          }
        }
        else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            var load = new GSA2DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            var memb = members.Where(e => e.Value.ApplicationId == nRef).First();
            StructuralAxis loadAxis = HelperClass.Parse2DAxis(memb.Value.Vertices.ToArray(), 0, load.Axis != 0); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // Perform projection
            if (load.Projected)
            {
              load.Value.Loading.Value[0] = 0;
              load.Value.Loading.Value[1] = 0;
            }
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // If the loading already exists, add element ref to list
            var match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
            if (match != null)
              match.Value.ElementRefs.Add(nRef);
            else
            {
              load.Value.ElementRefs = new List<string>() { nRef };
              loadSubList.Add(load);
            }
          }
        }

        loads.AddRange(loadSubList);
      }

      Initialiser.GSASenderObjects[typeof(GSA2DLoad)].AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
