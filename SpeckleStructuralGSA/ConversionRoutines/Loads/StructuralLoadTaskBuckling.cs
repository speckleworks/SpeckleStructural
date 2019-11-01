using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("ANAL.1", new string[] { "TASK.1" }, "loads", true, true, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) }, new Type[] { typeof(GSALoadCase), typeof(GSAConstructionStage), typeof(GSALoadCombo) })]
  public class GSALoadTaskBuckling : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadTaskBuckling();

    public void ParseGWACommand()
    {
      //if (this.GWACommand == null)
      //  return;

      //StructuralLoadTaskBuckling obj = new StructuralLoadTaskBuckling();

      //string[] pieces = this.GWACommand.ListSplit("\t");

      //int counter = 1; // Skip identifier

      //this.GSAId = Convert.ToInt32(pieces[counter++]);
      //obj.ApplicationId = Initialiser.Indexer.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      //obj.Name = pieces[counter++];

      ////Find task type
      //string taskRef = pieces[counter++];

      //// Parse description
      //string description = pieces[counter++];

      //// TODO: this only parses the super simple linear add descriptions
      //try
      //{
      //  List<Tuple<string, double>> desc = HelperClass.ParseLoadDescription(description);
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

      var taskIndex = Initialiser.Indexer.ResolveIndex("TASK.1", "", loadTask.ApplicationId);
      var comboIndex = Initialiser.Indexer.LookupIndex(typeof(GSALoadCombo).GetGSAKeyword(), typeof(GSALoadCombo).Name, loadTask.ResultCaseRef);
      var stageIndex = Initialiser.Indexer.LookupIndex(typeof(GSAConstructionStage).GetGSAKeyword(), typeof(GSAConstructionStage).Name, loadTask.StageDefinitionRef);

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
      var command = string.Join("\t", ls);

      gwaCommands.Add(command);
      //Initialiser.Interface.RunGWACommand(command);

      for (var i = 0; i < loadTask.NumModes; i++)
      {
        var caseIndex = Initialiser.Indexer.ResolveIndex(keyword, typeof(GSALoadTaskBuckling).Name);
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
        command = string.Join("\t", ls);
        //Initialiser.Interface.RunGWACommand(command);
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
      //    if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSALoadTaskBuckling)))
      //      Initialiser.GSASenderObjects[typeof(GSALoadTaskBuckling)] = new List<object>();

      //    var loadTasks = new List<GSALoadTaskBuckling>();

      //    string keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      //    string[] subKeywords = typeof(GSALoadTaskBuckling).GetSubGSAKeyword();

      //    string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      //    List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      //    foreach (string k in subKeywords)
      //      deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      //    // Remove deleted lines
      //    Initialiser.GSASenderObjects[typeof(GSALoadTaskBuckling)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      //    foreach (var kvp in Initialiser.GSASenderObjects)
      //      kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      //    // Filter only new lines
      //    string[] prevLines = Initialiser.GSASenderObjects[typeof(GSALoadTaskBuckling)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      //    string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      //    foreach (string p in newLines)
      //    {
      //      GSALoadTaskBuckling task = new GSALoadTaskBuckling() { GWACommand = p };
      //      task.ParseGWACommand();
      //      loadTasks.Add(task);
      //    }

      //    Initialiser.GSASenderObjects[typeof(GSALoadTaskBuckling)].AddRange(loadTasks);

      //    if (loadTasks.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
