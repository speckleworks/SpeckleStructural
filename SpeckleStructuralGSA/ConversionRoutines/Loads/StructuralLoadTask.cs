using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL.1", new string[] { "TASK.1" }, "loads", true, true, new Type[] { typeof(GSALoadCase) }, new Type[] { typeof(GSALoadCase) })]
  public class GSALoadTask : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadTask();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralLoadTask();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      //Find task type
      int.TryParse(pieces[counter++], out int taskRef);
      var taskRec = Initialiser.Cache.GetGwa("TASK.1", taskRef).First();
      obj.TaskType = Helper.GetLoadTaskType(taskRec);
      this.SubGWACommand.Add(taskRec);

      // Parse description
      var description = pieces[counter++];
      obj.LoadCaseRefs = new List<string>();
      obj.LoadFactors = new List<double>();

      // TODO: this only parses the super simple linear add descriptions
      try
      {
        var desc = Helper.ParseLoadDescription(description);

        foreach (var t in desc)
        {
          switch (t.Item1[0])
          {
            case 'L':
              obj.LoadCaseRefs.Add(Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
              obj.LoadFactors.Add(t.Item2);
              break;
          }
        }
      }
      catch
      {
      }

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var loadTask = this.Value as StructuralLoadTask;

      var keyword = typeof(GSALoadTask).GetGSAKeyword();

      var taskIndex = Initialiser.Cache.ResolveIndex("TASK.1", loadTask.ApplicationId);
      var index = Initialiser.Cache.ResolveIndex(typeof(GSALoadTask).GetGSAKeyword(), loadTask.ApplicationId);

      var gwaCommands = new List<string>();

      var ls = new List<string>
      {
        // Set TASK
        "SET",
        "TASK.1" + ":" + Helper.GenerateSID(loadTask),
        taskIndex.ToString(),
        "", // Name
        "0" // Stage
      };
      switch (loadTask.TaskType)
      {
        case StructuralLoadTaskType.LinearStatic:
          ls.Add("GSS");
          ls.Add("STATIC");
          // Defaults:
          ls.Add("1");
          ls.Add("0");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
        case StructuralLoadTaskType.NonlinearStatic:
          ls.Add("GSRELAX");
          ls.Add("BUCKLING_NL");
          // Defaults:
          ls.Add("SINGLE");
          ls.Add("0");
          ls.Add("BEAM_GEO_YES");
          ls.Add("SHELL_GEO_NO");
          ls.Add("0.1");
          ls.Add("0.0001");
          ls.Add("0.1");
          ls.Add("CYCLE");
          ls.Add("100000");
          ls.Add("REL");
          ls.Add("0.0010000000475");
          ls.Add("0.0010000000475");
          ls.Add("DISP_CTRL_YES");
          ls.Add("0");
          ls.Add("1");
          ls.Add("0.01");
          ls.Add("LOAD_CTRL_NO");
          ls.Add("1");
          ls.Add("");
          ls.Add("10");
          ls.Add("100");
          ls.Add("RESID_NOCONV");
          ls.Add("DAMP_VISCOUS");
          ls.Add("0");
          ls.Add("0");
          ls.Add("1");
          ls.Add("1");
          ls.Add("1");
          ls.Add("1");
          ls.Add("AUTO_MASS_YES");
          ls.Add("AUTO_DAMP_YES");
          ls.Add("FF_SAVE_ELEM_FORCE_YES");
          ls.Add("FF_SAVE_SPACER_FORCE_TO_ELEM");
          ls.Add("DRCEFNSQBHU*");
          break;
        case StructuralLoadTaskType.Modal:
          ls.Add("GSS");
          ls.Add("MODAL");
          // Defaults:
          ls.Add("1");
          ls.Add("1");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
        default:
          ls.Add("GSS");
          ls.Add("STATIC");
          // Defaults:
          ls.Add("1");
          ls.Add("0");
          ls.Add("128");
          ls.Add("SELF");
          ls.Add("none");
          ls.Add("none");
          ls.Add("DRCMEFNSQBHU*");
          ls.Add("MIN");
          ls.Add("AUTO");
          ls.Add("0");
          ls.Add("0");
          ls.Add("0");
          ls.Add("NONE");
          ls.Add("FATAL");
          ls.Add("NONE");
          ls.Add("NONE");
          ls.Add("RAFT_LO");
          ls.Add("RESID_NO");
          ls.Add("0");
          ls.Add("1");
          break;
      }
      gwaCommands.Add(string.Join("\t", ls));

      // Set ANAL
      ls.Clear();
      ls.Add("SET");
      ls.Add(keyword + ":" + Helper.GenerateSID(loadTask));
      ls.Add(index.ToString());
      ls.Add(loadTask.Name == null || loadTask.Name == "" ? " " : loadTask.Name);
      ls.Add(taskIndex.ToString());
      if (loadTask.TaskType == StructuralLoadTaskType.Modal)
      {
        ls.Add("M1");
      }
      else
      {
        var subLs = new List<string>();
        for (var i = 0; i < loadTask.LoadCaseRefs.Count(); i++)
        {
          var loadCaseRef = Initialiser.Cache.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), loadTask.LoadCaseRefs[i]);

          if (loadCaseRef.HasValue)
          {
            if (loadTask.LoadFactors != null && loadTask.LoadFactors.Count() > i)
              subLs.Add(loadTask.LoadFactors[i].ToString() + "L" + loadCaseRef.Value.ToString());
            else
              subLs.Add("L" + loadCaseRef.Value.ToString());
          }
        }
        ls.Add(string.Join(" + ", subLs));
      }
      gwaCommands.Add(string.Join("\t", ls));
      return string.Join("\n", gwaCommands);
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadTask loadTask)
    {
      return new GSALoadTask() { Value = loadTask }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSALoadTask dummyObject)
    {
      var newLines = ToSpeckleBase<GSALoadTask>();

      var loadTasks = new List<GSALoadTask>();

      foreach (var p in newLines.Values)
      {
        var task = new GSALoadTask() { GWACommand = p };
        task.ParseGWACommand();
        loadTasks.Add(task);
      }

      Initialiser.GSASenderObjects[typeof(GSALoadTask)].AddRange(loadTasks);

      return (loadTasks.Count() > 0 ) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
