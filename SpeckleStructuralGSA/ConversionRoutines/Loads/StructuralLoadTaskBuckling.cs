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

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      //if (this.GWACommand == null)
      //  return;

      //StructuralLoadTaskBuckling obj = new StructuralLoadTaskBuckling();

      //string[] pieces = this.GWACommand.ListSplit("\t");

      //int counter = 1; // Skip identifier

      //this.GSAId = Convert.ToInt32(pieces[counter++]);
      //obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
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

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralLoadTaskBuckling loadTask = this.Value as StructuralLoadTaskBuckling;

      string keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      string subkeyword = typeof(GSALoadTaskBuckling).GetSubGSAKeyword().First();

      int taskIndex = GSA.Indexer.ResolveIndex("TASK.1", loadTask.ApplicationId);
      int? comboIndex = GSA.Indexer.LookupIndex(typeof(GSALoadCombo).GetGSAKeyword(), loadTask.ResultCaseRef);
      int? stageIndex = GSA.Indexer.LookupIndex(typeof(GSAConstructionStage).GetGSAKeyword(), loadTask.StageDefinitionRef);

      List<string> ls = new List<string>
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
      GSA.RunGWACommand(command);

      for (var i = 0; i < loadTask.NumModes; i++)
      {
        int caseIndex = GSA.Indexer.ResolveIndex(keyword);
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
        GSA.RunGWACommand(command);
      }
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralLoadTaskBuckling loadTask)
    {
      new GSALoadTaskBuckling() { Value = loadTask }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    // TODO: Same keyword as StructuralLoadTask so will conflict. Need a way to differentiate between.

    public static SpeckleObject ToSpeckle(this GSALoadTaskBuckling dummyObject)
    {
      //    if (!GSASenderObjects.ContainsKey(typeof(GSALoadTaskBuckling)))
      //      GSASenderObjects[typeof(GSALoadTaskBuckling)] = new List<object>();

      //    var loadTasks = new List<GSALoadTaskBuckling>();

      //    string keyword = typeof(GSALoadTaskBuckling).GetGSAKeyword();
      //    string[] subKeywords = typeof(GSALoadTaskBuckling).GetSubGSAKeyword();

      //    string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      //    List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      //    foreach (string k in subKeywords)
      //      deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      //    // Remove deleted lines
      //    GSASenderObjects[typeof(GSALoadTaskBuckling)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      //    foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
      //      kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      //    // Filter only new lines
      //    string[] prevLines = GSASenderObjects[typeof(GSALoadTaskBuckling)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      //    string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      //    foreach (string p in newLines)
      //    {
      //      GSALoadTaskBuckling task = new GSALoadTaskBuckling() { GWACommand = p };
      //      task.ParseGWACommand(GSA);
      //      loadTasks.Add(task);
      //    }

      //    GSASenderObjects[typeof(GSALoadTaskBuckling)].AddRange(loadTasks);

      //    if (loadTasks.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
