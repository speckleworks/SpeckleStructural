using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRAVITY.2", new string[] { }, "loads", true, true, new Type[] { typeof(GSALoadCase) }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGravityLoading : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralGravityLoading();

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      StructuralGravityLoading obj = new StructuralGravityLoading();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      counter++; // Skip elements - assumed to always be "all" at this point int time

      obj.LoadCaseRef = Initialiser.Interface.GetSID(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var vector = new double[3];
      for (var i = 0; i < 3; i++)
        double.TryParse(pieces[counter++], out vector[i]);

      obj.GravityFactors = new StructuralVectorThree(vector);

      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      StructuralGravityLoading load = this.Value as StructuralGravityLoading;

      if (load.GravityFactors == null)
        return;

      string keyword = typeof(GSAGravityLoading).GetGSAKeyword();

      int loadCaseIndex = 0;
      try
      {
        //loadCaseIndex = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef).Value;
        loadCaseIndex = GSA.Indexer.LookupIndex(typeof(GSALoadCase).GetGSAKeyword().GetGSAKeyword(), load.LoadCaseRef).Value;
      }
      catch {
        //loadCaseIndex = GSA.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
        loadCaseIndex = GSA.Indexer.ResolveIndex(typeof(GSALoadCase).GetGSAKeyword(), load.LoadCaseRef);
      }

      //int index = GSA.Indexer.ResolveIndex(typeof(GSAGravityLoading).GetGSAKeyword());
      var index = GSA.Indexer.ResolveIndex(typeof(GSAGravityLoading).GetGSAKeyword());

      var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          //keyword + ":" + HelperClass.GenerateSID(load),
          keyword + ":" + HelperClass.GenerateSID(load),
          string.IsNullOrEmpty(load.Name) ? "" : load.Name,
          "all",
          loadCaseIndex.ToString(),
          load.GravityFactors.Value[0].ToString(),
          load.GravityFactors.Value[1].ToString(),
          load.GravityFactors.Value[2].ToString(),
        };

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }


  public static partial class Conversions
  {
    public static bool ToNative(this StructuralGravityLoading load)
    {
      //new GSAGravityLoading() { Value = load }.SetGWACommand(Initialiser.Interface);
      new GSAGravityLoading() { Value = load }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSAGravityLoading dummyObject)
    {
      Type objType = dummyObject.GetType();

      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSAGravityLoading)))
        Initialiser.GSASenderObjects[typeof(GSAGravityLoading)] = new List<object>();

      List<GSAGravityLoading> loads = new List<GSAGravityLoading>();

      string keyword = typeof(GSAGravityLoading).GetGSAKeyword();
      string[] subKeywords = typeof(GSAGravityLoading).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSAGravityLoading)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSAGravityLoading)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSAGravityLoading load = new GSAGravityLoading() { GWACommand = p };
        load.ParseGWACommand(Initialiser.Interface);
        loads.Add(load);
      }

      Initialiser.GSASenderObjects[objType].AddRange(loads);

      if (loads.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
