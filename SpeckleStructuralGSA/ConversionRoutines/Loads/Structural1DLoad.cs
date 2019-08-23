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
  [GSAObject("LOAD_BEAM", new string[] { "EL.3", "MEMB.7" }, "loads", true, true, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember) }, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember), typeof(GSA1DElementPolyline) })]
  public class GSA1DLoad : IGSASpeckleContainer
  {
    public int Axis; // Store this temporarily to generate other loads
    public bool Projected;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DLoad();

    public void ParseGWACommand(IGSAInterfacer GSA, List<GSA1DElement> elements, List<GSA1DMember> members)
    {
      if (this.GWACommand == null)
        return;

      Structural1DLoad obj = new Structural1DLoad();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 0; // Skip identifier
      string identifier = pieces[counter++];

      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        int[] targetElements = GSA.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.ELEMENT);

        if (elements != null)
        {
          List<GSA1DElement> elems = elements.Where(n => targetElements.Contains(n.GSAId)).ToList();

          obj.ElementRefs = elems.Select(n => (string)n.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(elems.Select(n => n.GWACommand));
        }
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        int[] targetGroups = HelperClass.GetGroupsFromGSAList(pieces[counter++]);

        if (members != null)
        {
          List<GSA1DMember> membs = members.Where(m => targetGroups.Contains(m.Group)).ToList();

          obj.ElementRefs = membs.Select(m => (string)m.Value.ApplicationId).ToList();
          this.SubGWACommand.AddRange(membs.Select(n => n.GWACommand));
        }
      }

      obj.LoadCaseRef = Initialiser.Interface.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      string axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

      this.Projected = pieces[counter++] == "YES";

      obj.Loading = new StructuralVectorSix(new double[6]);
      string direction = pieces[counter++].ToLower();

      double value = 0;

      // TODO: Only reads UDL load properly
      if (identifier.Contains("LOAD_BEAM_POINT.2"))
      {
        counter++; // Position
        counter++; // Value
        value = 0;
      }
      else if (identifier.Contains("LOAD_BEAM_UDL.2"))
        value = Convert.ToDouble(pieces[counter++]);
      else if (identifier.Contains("LOAD_BEAM_LINE.2"))
      {
        value = Convert.ToDouble(pieces[counter++]);
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else if (identifier.Contains("LOAD_BEAM_PATCH.2"))
      {
        counter++; // Position
        value = Convert.ToDouble(pieces[counter++]);
        counter++; // Position
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else if (identifier.Contains("LOAD_BEAM_TRILIN.2"))
      {
        counter++; // Position
        value = Convert.ToDouble(pieces[counter++]);
        counter++; // Position
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else
      {
        value = 0;
      }

      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = value;
          break;
        case "Y":
          obj.Loading.Value[1] = value;
          break;
        case "Z":
          obj.Loading.Value[2] = value;
          break;
        case "XX":
          obj.Loading.Value[3] = value;
          break;
        case "YY":
          obj.Loading.Value[4] = value;
          break;
        case "ZZ":
          obj.Loading.Value[5] = value;
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

      Structural1DLoad load = this.Value as Structural1DLoad;

      if (load.Loading == null)
        return;

      string keyword = typeof(GSA1DLoad).GetGSAKeyword();

      List<int> elementRefs;
      List<int> groupRefs;

      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
      {
        //elementRefs = GSA.Indexer.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        elementRefs = GSA.Indexer.LookupIndices(typeof(GSA1DElement).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        //groupRefs = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
      {
        elementRefs = new List<int>();
        groupRefs = GSA.Indexer.LookupIndices(typeof(GSA1DMember).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs.AddRange(GSA.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword().GetGSAKeyword(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList());
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
        loadCaseRef = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword().GetGSAKeyword(), load.LoadCaseRef).Value;
      }

      string[] direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      for (int i = 0; i < load.Loading.Value.Count(); i++)
      {
        List<string> ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        //int index = GSA.Indexer.ResolveIndex(typeof(GSA1DLoad).GetGSAKeyword());
        var index = GSA.Indexer.ResolveIndex(typeof(GSA1DLoad).GetGSAKeyword());

        ls.Add("SET_AT");
        ls.Add(index.ToString());
        //ls.Add("LOAD_BEAM_UDL" + ":" + HelperClass.GenerateSID(load)); // TODO: Only writes to UDL load
        ls.Add("LOAD_BEAM_UDL" + ":" + HelperClass.GenerateSID(load)); // TODO: Only writes to UDL load
        ls.Add(load.Name == null || load.Name == "" ? " " : load.Name);
        // TODO: This is a hack.
        ls.Add(string.Join(
            " ",
            elementRefs.Select(x => x.ToString())
                .Concat(groupRefs.Select(x => "G" + x.ToString()))
        ));
        ls.Add(loadCaseRef.ToString());
        ls.Add("GLOBAL"); // Axis
        ls.Add("NO"); // Projected
        ls.Add(direction[i]);
        ls.Add(load.Loading.Value[i].ToString());

        Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this Structural1DLoad load)
    {
      //new GSA1DLoad() { Value = load }.SetGWACommand(Initialiser.Interface);
      new GSA1DLoad() { Value = load }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DLoad dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSA1DLoad)))
        Initialiser.GSASenderObjects[typeof(GSA1DLoad)] = new List<object>();

      List<GSA1DLoad> loads = new List<GSA1DLoad>();
      List<GSA1DElement> elements = Conversions.GSATargetLayer == GSATargetLayer.Analysis ? Initialiser.GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList() : new List<GSA1DElement>();
      List<GSA1DMember> members = Conversions.GSATargetLayer == GSATargetLayer.Design ? Initialiser.GSASenderObjects[typeof(GSA1DMember)].Cast<GSA1DMember>().ToList() : new List<GSA1DMember>();

      string keyword = typeof(GSA1DLoad).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DLoad).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSA1DLoad)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSA1DLoad)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        List<GSA1DLoad> loadSubList = new List<GSA1DLoad>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        GSA1DLoad initLoad = new GSA1DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(Initialiser.Interface, elements, members);

        if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            GSA1DLoad load = new GSA1DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            GSA1DElement elem = elements.Where(e => e.Value.ApplicationId == nRef).First();
            StructuralAxis loadAxis = load.Axis == 0 ? new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })) :
                HelperClass.LocalAxisEntity1D(elem.Value.Value.ToArray(), elem.Value.ZAxis); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // Perform projection
            if (load.Projected)
            {
              Vector3D loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                Vector3D axisX = new Vector3D(elem.Value[5] - elem.Value[0], elem.Value[4] - elem.Value[1], elem.Value[3] - elem.Value[2]);
                double angle = Vector3D.AngleBetween(loadDirection, axisX);
                double factor = Math.Sin(angle);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

            // If the loading already exists, add element ref to list
            GSA1DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value.Loading.Value as List<double>).SequenceEqual(load.Value.Loading.Value as List<double>)).First() : null;
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
            GSA1DLoad load = new GSA1DLoad();
            load.GWACommand = initLoad.GWACommand;
            load.SubGWACommand = new List<string>(initLoad.SubGWACommand);
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            GSA1DMember memb = members.Where(e => e.Value.ApplicationId == nRef).First();
            StructuralAxis loadAxis = load.Axis == 0 ? new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })) :
                HelperClass.LocalAxisEntity1D(memb.Value.Value.ToArray(), memb.Value.ZAxis); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // Perform projection
            if (load.Projected)
            {
              Vector3D loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                Vector3D axisX = new Vector3D(memb.Value[5] - memb.Value[0], memb.Value[4] - memb.Value[1], memb.Value[3] - memb.Value[2]);
                double angle = Vector3D.AngleBetween(loadDirection, axisX);
                double factor = Math.Sin(angle);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

            // If the loading already exists, add element ref to list
            GSA1DLoad match = loadSubList.Count() > 0 ? loadSubList.Where(l => l.Value.Loading.Equals(load.Value.Loading)).First() : null;
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

      Initialiser.GSASenderObjects[typeof(GSA1DLoad)].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
