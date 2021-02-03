using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_GRAVITY.3", new string[] { }, "model", true, true, new Type[] { typeof(GSALoadCase) }, new Type[] { typeof(GSALoadCase) })]
  public class GSAGravityLoading : GSABase<StructuralGravityLoading>
  {
    public void ParseGWACommand()
    {
      // LOAD_GRAVITY.3 | name | elemlist | nodelist | case | x | y | z

      if (this.GWACommand == null)
        return;

      var obj = new StructuralGravityLoading();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      counter++; // elemlist - Skip elements - assumed to always be "all" at this point in time
      counter++; // nodelist - also skipped

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++])); // case

      // x | y| z
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
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(loadCaseKeyword, load.LoadCaseRef);
      var loadCaseRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(loadCaseKeyword, load.LoadCaseRef);

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

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSAGravityLoading).GetGSAKeyword());

      var sid = Helper.GenerateSID(load);
      var ls = new List<string>
        {
          "SET_AT",
          index.ToString(),
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
          string.IsNullOrEmpty(load.Name) ? "" : load.Name,
          "all",
          "all",
          loadCaseRef.ToString(),
          load.GravityFactors.Value[0].ToString(),
          load.GravityFactors.Value[1].ToString(),
          load.GravityFactors.Value[2].ToString(),
        };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
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
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSAGravityLoading>();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var load = new GSAGravityLoading() { GWACommand = p, GSAId = k };
        try
        {
          load.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
        loads.Add(load);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
