using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
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

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSA2DElement> elements, List<GSA2DMember> members)
    {
      if (this.GWACommand == null)
        return;

      Structural2DLoad obj = new Structural2DLoad();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier

      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        int[] targetElements = GSA.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.ELEMENT);

        if (elements != null)
        {
          List<GSA2DElement> elems = elements.Where(n => targetElements.Contains(n.GSAId)).ToList();

          obj.ElementRefs = elems.Select(n => (string)n.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(elems.Select(n => n.GWACommand));
        }
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        int[] targetGroups = HelperClass.GetGroupsFromGSAList(pieces[counter++]);

        if (members != null)
        {
          List<GSA2DMember> membs = members.Where(m => targetGroups.Contains(m.Group)).ToList();

          obj.ElementRefs = membs.Select(m => (string)m.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(membs.Select(n => n.GWACommand));
        }
      }

      obj.LoadCaseRef = GSA.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      string axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

      counter++; // Type. TODO: Skipping since we're taking the average

      this.Projected = pieces[counter++] == "YES";

      obj.Loading = new StructuralVectorThree(new double[3]);
      string direction = pieces[counter++].ToLower();

      double[] values = pieces.Skip(counter).Select(p => Convert.ToDouble(p)).ToArray();

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

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Structural2DLoad load = this.Value as Structural2DLoad;

      if (load.Loading == null)
        return;

      string keyword = typeof(GSA2DLoad).GetGSAKeyword();

      List<int> elementRefs;
      List<int> groupRefs;

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        //elementRefs = GSA.Indexer.LookupIndices(typeof(GSA2DElement).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        elementRefs = GSA.Indexer.LookupIndices(typeof(GSA2DElement).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        //groupRefs = GSA.Indexer.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = GSA.Indexer.LookupIndices(typeof(GSA2DElementMesh).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        elementRefs = new List<int>();
        //groupRefs = GSA.Indexer.LookupIndices(typeof(GSA2DMember).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = GSA.Indexer.LookupIndices(typeof(GSA2DMember).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else
      {
        return;
      }

      int loadCaseRef = 0;
      try
      {
        //loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef).Value;
        loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword().GetGSAKeyword(), load.LoadCaseRef).Value;
      }
      catch {
        //loadCaseRef = GSA.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
        loadCaseRef = GSA.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
      }

      string[] direction = new string[3] { "X", "Y", "Z" };

      for (int i = 0; i < load.Loading.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        //int index = GSA.Indexer.ResolveIndex(typeof(GSA2DLoad).GetGSAKeyword());
        var index = GSA.Indexer.ResolveIndex(typeof(GSA2DLoad).GetGSAKeyword());
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

        GSA.RunGWACommand(string.Join("\t", ls));
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural2DLoad load)
    {
      //new GSA2DLoad() { Value = load }.SetGWACommand(Initialiser.Interface);
      new GSA2DLoad() { Value = load }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA2DLoad dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA2DLoad)))
        GSASenderObjects[typeof(GSA2DLoad)] = new List<object>();

      List<GSA2DLoad> loads = new List<GSA2DLoad>();
      List<GSA2DElement> elements = GSATargetLayer == GSATargetLayer.Analysis ? GSASenderObjects[typeof(GSA2DElement)].Cast<GSA2DElement>().ToList() : new List<GSA2DElement>();
      List<GSA2DMember> members = GSATargetLayer == GSATargetLayer.Design ? GSASenderObjects[typeof(GSA2DMember)].Cast<GSA2DMember>().ToList() : new List<GSA2DMember>();

      string keyword = typeof(GSA2DLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSA2DLoad).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA2DLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA2DLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        List<GSA2DLoad> loadSubList = new List<GSA2DLoad>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        GSA2DLoad initLoad = new GSA2DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(GSA, elements, members);

        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            GSA2DLoad load = new GSA2DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            GSA2DElement elem = elements.Where(e => e.Value.ApplicationId == nRef).First();
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
            GSA2DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => l.Value.Loading.Equals(load.Value.Loading)).First() : null;
            if (match != null)
              match.Value.ElementRefs.Add(nRef);
            else
            {
              load.Value.ElementRefs = new List<string>() { nRef };
              loadSubList.Add(load);
            }
          }
        }
        else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            GSA2DLoad load = new GSA2DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            GSA2DMember memb = members.Where(e => e.Value.ApplicationId == nRef).First();
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
            GSA2DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
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

      GSASenderObjects[typeof(GSA2DLoad)].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
