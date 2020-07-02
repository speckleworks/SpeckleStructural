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

      var obj = new StructuralLoadCombo();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      // Parse type
      var description = pieces[counter++];
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
        var desc = Helper.ParseLoadDescription(description);

        foreach (var t in desc)
        {
          switch (t.Item1[0])
          {
            case 'A':
              obj.LoadTaskRefs.Add(Helper.GetApplicationId(typeof(GSALoadTask).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
              obj.LoadTaskFactors.Add(t.Item2);
              break;
            case 'C':
              obj.LoadComboRefs.Add(Helper.GetApplicationId(typeof(GSALoadCombo).GetGSAKeyword(), Convert.ToInt32(t.Item1.Substring(1))));
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

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var loadCombo = this.Value as StructuralLoadCombo;

      var keyword = typeof(GSALoadCombo).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSALoadCombo).GetGSAKeyword(), loadCombo.ApplicationId);

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(loadCombo),
        index.ToString(),
        loadCombo.Name == null || loadCombo.Name == "" ? " " : loadCombo.Name
      };

      var subLs = new List<string>();
      if (loadCombo.LoadTaskRefs != null)
      {
        for (var i = 0; i < loadCombo.LoadTaskRefs.Count(); i++)
        {
          var loadTaskRef = Initialiser.Cache.LookupIndex(typeof(GSALoadTask).GetGSAKeyword(), loadCombo.LoadTaskRefs[i]);

          if (loadTaskRef.HasValue)
          {
            subLs.Add((loadCombo.LoadTaskFactors != null && loadCombo.LoadTaskFactors.Count() > i) 
              ? loadCombo.LoadTaskFactors[i].ToString() + "A" + loadTaskRef.Value.ToString()
              : "A" + loadTaskRef.Value.ToString());
          }
        }
      }

      if (loadCombo.LoadComboRefs != null)
      {
        for (var i = 0; i < loadCombo.LoadComboRefs.Count(); i++)
        {
          var loadComboRef = Initialiser.Cache.LookupIndex(typeof(GSALoadTask).GetGSAKeyword(), loadCombo.LoadComboRefs[i]);

          if (loadComboRef.HasValue)
          {
            subLs.Add((loadCombo.LoadComboFactors != null && loadCombo.LoadComboFactors.Count() > i)
              ? loadCombo.LoadComboFactors[i].ToString() + "C" + loadComboRef.Value.ToString()
              : "C" + loadComboRef.Value.ToString());
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

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralLoadCombo loadCombo)
    {
      return new GSALoadCombo() { Value = loadCombo }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSALoadCombo dummyObject)
    {
      var newLines = ToSpeckleBase<GSALoadCombo>();
      var typeName = dummyObject.GetType().Name;
      var loadCombos = new List<GSALoadCombo>();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var combo = new GSALoadCombo() { GWACommand = p };
        try
        {
          combo.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, k.ToString());
        }
        loadCombos.Add(combo);
      }

      Initialiser.GSASenderObjects.AddRange(loadCombos);

      return (loadCombos.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
