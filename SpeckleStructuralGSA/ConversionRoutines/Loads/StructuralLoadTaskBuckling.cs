using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL.1", new string[] { "TASK.2" }, "model", true, true, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) }, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) })]
  public class GSALoadTaskBuckling : GSABase<StructuralLoadTaskBuckling>
  {

    public void ParseGWACommand()
    {
      //if (this.GWACommand == null)
      //  return;

      //StructuralLoadTaskBuckling obj = new StructuralLoadTaskBuckling();

      //string[] pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      //int counter = 1; // Skip identifier

      //this.GSAId = Convert.ToInt32(pieces[counter++]);
      //obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      //obj.Name = pieces[counter++];

      ////Find task type
      //string taskRef = pieces[counter++];

      //// Parse description
      //string description = pieces[counter++];

      //// TODO: this only parses the super simple linear add descriptions
      //try
      //{
      //  List<Tuple<string, double>> desc = Helper.ParseLoadDescription(description);
      //}
      //catch { }

      //this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var gwaCommands = new List<string>();

      var loadTask = this.Value as StructuralLoadTaskBuckling;

      var keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      var subkeyword = typeof(GSALoadTaskBuckling).GetSubGSAKeyword().First();

      var taskIndex = Initialiser.AppResources.Cache.ResolveIndex("TASK.2", loadTask.ApplicationId);
      var comboIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSALoadCombo).GetGSAKeyword(), loadTask.ResultCaseRef);
      var stageIndex = Initialiser.AppResources.Cache.LookupIndex(typeof(GSAConstructionStage).GetGSAKeyword(), loadTask.StageDefinitionRef);

      var ls = new List<string>
        {
          "SET",
          subkeyword,
          taskIndex.ToString(),
          string.IsNullOrEmpty(loadTask.Name) ? " " : loadTask.Name, // Name
          (stageIndex == null) ? "0" : stageIndex.ToString(), // Stage
          "GSS",
          "BUCKLING",
          "1",
          loadTask.NumModes.ToString(),
          loadTask.MaxNumIterations.ToString(),
          (comboIndex == null) ? "0" : "C" + comboIndex,
          "none",
          "none",
          "DRCMEFNSQBHU*",
          "MIN",
          "AUTO",
          "0",
          "0",
          "0","" +
          "NONE",
          "FATAL",
          "NONE",
          "NONE",
          "RAFT_LO",
          "RESID_NO",
          "0",
          "1"
        };
      var command = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);

      gwaCommands.Add(command);
      //Initialiser.AppResources.Proxy.RunGWACommand(command);

      for (var i = 0; i < loadTask.NumModes; i++)
      {
        var caseIndex = Initialiser.AppResources.Cache.ResolveIndex(keyword);
        // Set ANAL
        ls.Clear();
        ls.AddRange(new[] {
          "SET",
          keyword,
          caseIndex.ToString(),
          string.IsNullOrEmpty(loadTask.Name) ? " " : loadTask.Name,
          taskIndex.ToString().ToString(),
          "M" + (i + 1) //desc
        });
        command = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);
        //Initialiser.AppResources.Proxy.RunGWACommand(command);
        gwaCommands.Add(command);
      }

      return string.Join("\n", gwaCommands);
    }
    
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadTaskBuckling loadTask)
    {
      return new GSALoadTaskBuckling() { Value = loadTask }.SetGWACommand();
    }

    // TODO: Same keyword as StructuralLoadTask so will conflict. Need a way to differentiate between.

    public static SpeckleObject ToSpeckle(this GSALoadTaskBuckling dummyObject)
    {
      //    if (!Initialiser.GsaKit.GSASenderObjects.ContainsKey(typeof(GSALoadTaskBuckling)))
      //      Initialiser.GsaKit.GSASenderObjects.Get<GSALoadTaskBuckling)] = new List<object>();

      //    var loadTasks = new List<GSALoadTaskBuckling>();

      //    string keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      //    string[] subKeywords = typeof(GSALoadTaskBuckling).GetSubGSAKeyword();

      //    string[] lines = Initialiser.AppResources.Proxy.GetGWARecords("GET_ALL\t" + keyword);
      //    List<string> deletedLines = Initialiser.AppResources.Proxy.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      //    foreach (string k in subKeywords)
      //      deletedLines.AddRange(Initialiser.AppResources.Proxy.GetDeletedGWARecords("GET_ALL\t" + k));

      //    // Remove deleted lines
      //    Initialiser.GsaKit.GSASenderObjects.Get<GSALoadTaskBuckling)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      //    foreach (var kvp in Initialiser.Instance.GSASenderObjects)
      //      kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      //    // Filter only new lines
      //    string[] prevLines = Initialiser.GsaKit.GSASenderObjects.Get<GSALoadTaskBuckling)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      //    string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      //    foreach (string p in newLines)
      //    {
      //      GSALoadTaskBuckling task = new GSALoadTaskBuckling() { GWACommand = p };
      //      task.ParseGWACommand();
      //      loadTasks.Add(task);
      //    }

      //    Initialiser.GsaKit.GSASenderObjects.AddRange(loadTasks);

      //    if (loadTasks.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
