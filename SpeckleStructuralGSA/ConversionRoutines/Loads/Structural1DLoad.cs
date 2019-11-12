using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using SpeckleCore;
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

    public void ParseGWACommand(List<GSA1DElement> elements, List<GSA1DMember> members)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DLoad();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 0; // Skip identifier
      var identifier = pieces[counter++];

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

      obj.LoadCaseRef = HelperClass.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

      this.Projected = pieces[counter++] == "YES";

      obj.Loading = new StructuralVectorSix(new double[6]);
      var direction = pieces[counter++].ToLower();

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

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as Structural1DLoad;

      if (load.Loading == null)
        return "";

      var keyword = typeof(GSA1DLoad).GetGSAKeyword();

      List<int> elementRefs;
      List<int> groupRefs;

      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        elementRefs = Initialiser.Indexer.LookupIndices(typeof(GSA1DElement).GetGSAKeyword(), typeof(GSA1DElement).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs = Initialiser.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), typeof(GSA1DElementPolyline).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
      }
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
      {
        elementRefs = new List<int>();
        groupRefs = Initialiser.Indexer.LookupIndices(typeof(GSA1DMember).GetGSAKeyword(), typeof(GSA1DMember).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList();
        groupRefs.AddRange(Initialiser.Indexer.LookupIndices(typeof(GSA1DElementPolyline).GetGSAKeyword(), typeof(GSA1DElementPolyline).ToSpeckleTypeName(), load.ElementRefs).Where(x => x.HasValue).Select(x => x.Value).ToList());
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
        loadCaseRef = Initialiser.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), typeof(GSALoadCase).ToSpeckleTypeName(), load.LoadCaseRef).Value;
      }

      var direction = new string[6] { "X", "Y", "Z", "XX", "YY", "ZZ" };

      var gwaCommands = new List<string>();

      for (var i = 0; i < load.Loading.Value.Count(); i++)
      {
        var ls = new List<string>();

        if (load.Loading.Value[i] == 0) continue;

        var index = Initialiser.Indexer.ResolveIndex(typeof(GSA1DLoad).GetGSAKeyword(), typeof(GSA1DLoad).Name);

        ls.Add("SET_AT");
        ls.Add(index.ToString());
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

        gwaCommands.Add(string.Join("\t", ls));
      }

      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural1DLoad load)
    {
      return new GSA1DLoad() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA1DLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DLoad>();

      var loads = new List<GSA1DLoad>();
      var elements = Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis ? Initialiser.GSASenderObjects[typeof(GSA1DElement)].Cast<GSA1DElement>().ToList() : new List<GSA1DElement>();
      var members = Initialiser.Settings.TargetLayer == GSATargetLayer.Design ? Initialiser.GSASenderObjects[typeof(GSA1DMember)].Cast<GSA1DMember>().ToList() : new List<GSA1DMember>();

      foreach (var p in newLines.Values)
      {
        var loadSubList = new List<GSA1DLoad>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA1DLoad() { GWACommand = p };
        initLoad.ParseGWACommand(elements, members);

        if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            var load = new GSA1DLoad
            {
              GWACommand = initLoad.GWACommand,
              SubGWACommand = new List<string>(initLoad.SubGWACommand)
            };
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            var elem = elements.Where(e => e.Value.ApplicationId == nRef).First();
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
              var loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                var axisX = new Vector3D(elem.Value[5] - elem.Value[0], elem.Value[4] - elem.Value[1], elem.Value[3] - elem.Value[2]);
                var angle = Vector3D.AngleBetween(loadDirection, axisX);
                var factor = Math.Sin(angle);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

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
        else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
        {
          // Create load for each element applied
          foreach (string nRef in initLoad.Value.ElementRefs)
          {
            var load = new GSA1DLoad
            {
              GWACommand = initLoad.GWACommand,
              SubGWACommand = new List<string>(initLoad.SubGWACommand)
            };
            load.Value.Name = initLoad.Value.Name;
            load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

            // Transform load to defined axis
            var memb = members.Where(e => e.Value.ApplicationId == nRef).First();
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
              var loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                var axisX = new Vector3D(memb.Value[5] - memb.Value[0], memb.Value[4] - memb.Value[1], memb.Value[3] - memb.Value[2]);
                var angle = Vector3D.AngleBetween(loadDirection, axisX);
                var factor = Math.Sin(angle);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

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

        loads.AddRange(loadSubList);
      }

      Initialiser.GSASenderObjects[typeof(GSA1DLoad)].AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
