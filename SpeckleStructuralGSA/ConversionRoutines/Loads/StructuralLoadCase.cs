using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_TITLE.2", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSALoadCase : GSABase<StructuralLoadCase>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralLoadCase();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++];

      var type = pieces[counter++];
      switch (type)
      {
        case "LC_PERM_SELF":
        case "DEAD":
          obj.CaseType = StructuralLoadCaseType.Dead;
          break;
        case "LC_VAR_IMP":
          obj.CaseType = StructuralLoadCaseType.Live;
          break;
        case "WIND":
          obj.CaseType = StructuralLoadCaseType.Wind;
          break;
        case "SNOW":
          obj.CaseType = StructuralLoadCaseType.Snow;
          break;
        case "SEISMIC":
          obj.CaseType = StructuralLoadCaseType.Earthquake;
          break;
        case "LC_PERM_SOIL":
          obj.CaseType = StructuralLoadCaseType.Soil;
          break;
        case "LC_VAR_TEMP":
          obj.CaseType = StructuralLoadCaseType.Thermal;
          break;
        default:
          obj.CaseType = StructuralLoadCaseType.Generic;
          break;
      }

      // Rest is unimportant

      this.Value = obj;
    }
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSALoadCase dummyObject)
    {
      var newLines = ToSpeckleBase<GSALoadCase>();
      var typeName = dummyObject.GetType().Name;
      var loadCases = new SortedDictionary<int, GSALoadCase>();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var loadCase = new GSALoadCase() { GWACommand = p, GSAId = k };
        try
        {
          loadCase.ParseGWACommand();
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, k.ToString()); 
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, k.ToString());
        }
        loadCases.Add(k, loadCase);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loadCases.Values.ToList());

      return (loadCases.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
