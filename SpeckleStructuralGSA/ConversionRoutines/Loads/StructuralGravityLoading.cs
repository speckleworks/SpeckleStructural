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

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralGravityLoading();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      counter++; // Skip elements - assumed to always be "all" at this point int time

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var vector = new double[3];
      for (var i = 0; i < 3; i++)
        double.TryParse(pieces[counter++], out vector[i]);

      obj.GravityFactors = new StructuralVectorThree(vector);

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var load = this.Value as StructuralGravityLoading;

      if (load.GravityFactors == null)
        return "";

      var keyword = typeof(GSAGravityLoading).GetGSAKeyword();

      var loadCaseKeyword = typeof(GSALoadCase).GetGSAKeyword();
      var indexResult = Initialiser.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

      if (indexResult == null && load.ApplicationId != null)
      {
        if (load.LoadCaseRef == null)
        {
          Helper.SafeDisplay("Blank load case references found for these Application IDs:", load.ApplicationId);
        }
        else
        {
          Helper.SafeDisplay("Load case references not found:", load.ApplicationId + " referencing " + load.LoadCaseRef);
        }
      }

      var index = Initialiser.Cache.ResolveIndex(typeof(GSAGravityLoading).GetGSAKeyword());

      var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + ":" + Helper.GenerateSID(load),
          string.IsNullOrEmpty(load.Name) ? "" : load.Name,
          "all",
          loadCaseRef.ToString(),
          load.GravityFactors.Value[0].ToString(),
          load.GravityFactors.Value[1].ToString(),
          load.GravityFactors.Value[2].ToString(),
        };

      return (string.Join("\t", ls));
    }
  }


  public static partial class Conversions
  {
    public static string ToNative(this StructuralGravityLoading load)
    {
      return new GSAGravityLoading() { Value = load }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGravityLoading dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGravityLoading>();

      var loads = new List<GSAGravityLoading>();

      foreach (var p in newLines.Values)
      {
        var load = new GSAGravityLoading() { GWACommand = p };
        load.ParseGWACommand();
        loads.Add(load);
      }

      Initialiser.GSASenderObjects[typeof(GSAGravityLoading)].AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
