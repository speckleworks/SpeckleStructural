using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("COMBINATION.1", new string[] { }, "loads", true, true, new Type[] { typeof(GSALoadCase), typeof(GSALoadTask) }, new Type[] { typeof(GSALoadCase), typeof(GSALoadTask) })]
  public class GSALoadCombo : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralLoadCombo();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      StructuralLoadCombo obj = new StructuralLoadCombo();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Indexer.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      // Parse type
      string description = pieces[counter++];
      if (description.Contains("+"))
        obj.ComboType = StructuralLoadComboType.LinearAdd;
      else if (description.Contains("or"))
        obj.ComboType = StructuralLoadComboType.Envelope;
      else
        obj.ComboType = StructuralLoadComboType.LinearAdd;

      obj.LoadTaskRefs = new List<string>();
      obj.LoadTaskFactors = new List<double>();
      obj.LoadComboRefs = new List<string>();
      obj.LoadComboFactors = new List<double>();

      // TODO: this only parses the super simple linear add descriptions
      try
      {
        List<Tuple<string, double>> desc = HelperClass.ParseLoadDescription(description);

        foreach (Tuple<string, double> t in desc)
        {
          switch (t.Item1[0])
          {
            case 'A':
              obj.LoadTaskRefs.Add(Initialiser.Indexer.GetApplicationId(typeof(GSALoadTask).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
              obj.LoadTaskFactors.Add(t.Item2);
              break;
            case 'C':
              obj.LoadComboRefs.Add(Initialiser.Indexer.GetApplicationId(typeof(GSALoadCombo).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
              obj.LoadComboFactors.Add(t.Item2);
              break;
          }
        }
      }
      catch
      {
      }

      this.Value = obj;
    }

    public void SetGWACommand()
    {
      if (this.Value == null)
        return;

      StructuralLoadCombo loadCombo = this.Value as StructuralLoadCombo;

      string keyword = typeof(GSALoadCombo).GetGSAKeyword();

      int index = Initialiser.Indexer.ResolveIndex(typeof(GSALoadCombo).GetGSAKeyword(), typeof(GSALoadCombo).Name, loadCombo.ApplicationId);

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + HelperClass.GenerateSID(loadCombo));
      ls.Add(index.ToString());
      ls.Add(loadCombo.Name == null || loadCombo.Name == "" ? " " : loadCombo.Name);

      List<string> subLs = new List<string>();
      if (loadCombo.LoadTaskRefs != null)
      {
        for (int i = 0; i < loadCombo.LoadTaskRefs.Count(); i++)
        {
          int? loadTaskRef = Initialiser.Indexer.LookupIndex(typeof(GSALoadTask).GetGSAKeyword(), typeof(GSALoadTask).Name, loadCombo.LoadTaskRefs[i]);

          if (loadTaskRef.HasValue)
          {
            if (loadCombo.LoadTaskFactors != null && loadCombo.LoadTaskFactors.Count() > i)
              subLs.Add(loadCombo.LoadTaskFactors[i].ToString() + "A" + loadTaskRef.Value.ToString());
            else
              subLs.Add("A" + loadTaskRef.Value.ToString());
          }
        }
      }

      if (loadCombo.LoadComboRefs != null)
      {
        for (int i = 0; i < loadCombo.LoadComboRefs.Count(); i++)
        {
          int? loadComboRef = Initialiser.Indexer.LookupIndex(typeof(GSALoadTask).GetGSAKeyword(), typeof(GSALoadTask).Name, loadCombo.LoadComboRefs[i]);

          if (loadComboRef.HasValue)
          {
            if (loadCombo.LoadComboFactors != null && loadCombo.LoadComboFactors.Count() > i)
              subLs.Add(loadCombo.LoadComboFactors[i].ToString() + "C" + loadComboRef.Value.ToString());
            else
              subLs.Add("C" + loadComboRef.Value.ToString());
          }
        }
      }

      switch (loadCombo.ComboType)
      {
        case StructuralLoadComboType.LinearAdd:
          ls.Add(string.Join(" + ", subLs));
          break;
        case StructuralLoadComboType.Envelope:
          ls.Add(string.Join(" or ", subLs));
          break;
        default:
          ls.Add(string.Join(" + ", subLs));
          break;
      }

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralLoadCombo loadCombo)
    {
      new GSALoadCombo() { Value = loadCombo }.SetGWACommand();

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSALoadCombo dummyObject)
    {
      if (!Initialiser.GSASenderObjects.ContainsKey(typeof(GSALoadCombo)))
        Initialiser.GSASenderObjects[typeof(GSALoadCombo)] = new List<object>();

      List<GSALoadCombo> loadCombos = new List<GSALoadCombo>();

      string keyword = typeof(GSALoadCombo).GetGSAKeyword();
      string[] subKeywords = typeof(GSALoadCombo).GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[typeof(GSALoadCombo)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[typeof(GSALoadCombo)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSALoadCombo combo = new GSALoadCombo() { GWACommand = p };
        combo.ParseGWACommand(Initialiser.Interface);
        loadCombos.Add(combo);
      }

      Initialiser.GSASenderObjects[typeof(GSALoadCombo)].AddRange(loadCombos);

      if (loadCombos.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
