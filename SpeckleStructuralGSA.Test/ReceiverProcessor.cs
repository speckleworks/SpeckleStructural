using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  //Copied from the Receiver class in SpeckleGSA - this will be refactored to simplify and avoid dependency
  public class ReceiverProcessor : ProcessorBase
  {
    private List<SpeckleObject> receivedObjects;

    public ReceiverProcessor(string directory, GSAProxy gsaInterfacer, GSACache gsaCache, GSATargetLayer layer = GSATargetLayer.Design) : base (directory)
    {
      GSAInterfacer = gsaInterfacer;
      GSACache = gsaCache;
      Initialiser.Settings.TargetLayer = layer;
    }

    public void JsonSpeckleStreamsToGwaRecords(IEnumerable<string> savedJsonFileNames, out List<GwaRecord> gwaRecords)
    {
      gwaRecords = new List<GwaRecord>();

      receivedObjects = JsonSpeckleStreamsToSpeckleObjects(savedJsonFileNames);

      ScaleObjects();

      ConvertSpeckleObjectsToGsaInterfacerCache();

      var gwaCommands = GSACache.GetGwaSetCommands();
      foreach (var gwaC in gwaCommands)
      {
        gwaRecords.Add(new GwaRecord(ExtractApplicationId(gwaC), gwaC));
      }
    }

    #region private_methods    

    private List<SpeckleObject> JsonSpeckleStreamsToSpeckleObjects(IEnumerable<string> savedJsonFileNames)
    {
      //Read JSON files into objects
      return ExtractObjects(savedJsonFileNames.ToArray(), TestDataDirectory);
    }

    private void ScaleObjects()
    {
      //Status.ChangeStatus("Scaling objects");
      var scaleFactor = (1.0).ConvertUnit("mm", "m");
      foreach (var o in receivedObjects)
      {
        try
        {
          o.Scale(scaleFactor);
        }
        catch { }
      }
    }

    private void ConvertSpeckleObjectsToGsaInterfacerCache()
    {
      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      var TypePrerequisites = GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Design, false);
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);
          var keyword = "";
          try
          {
            keyword = dummyObject.GetAttribute("GSAKeyword").ToString();
          }
          catch { }
          var valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var targetObjects = receivedObjects.Where(o => o.GetType() == valueType).ToList();

          for (var i = 0; i < targetObjects.Count(); i++)
          {
            var applicationId = targetObjects[i].ApplicationId;
            var deserialiseReturn = ((string)Converter.Deserialise(targetObjects[i]));
            var gwaCommands = deserialiseReturn.Split(new[] { '\n' }).Where(c => c.Length > 0).ToList();

            for (var j = 0; j < gwaCommands.Count(); j++)
            {
              ProcessDeserialiseReturnObject(gwaCommands[j], out keyword, out var index, out var gwa, out var gwaSetCommandType);
              var itemApplicationId = gwaCommands[j].ExtractApplicationId();

              GSAInterfacer.SetGWA(gwaCommands[j]);

              //Only cache the object against, the top-level GWA command, not the sub-commands
              GSACache.Upsert(keyword, index, gwa, itemApplicationId, (itemApplicationId == applicationId) ? targetObjects[i] : null);
            }
          }


          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      // Write leftover
      Converter.Deserialise(receivedObjects);
    }

    private string ExtractApplicationId(string gwaCommand)
    {
      if (!gwaCommand.Contains(SID_TAG))
      {
        return null;
      }
      return gwaCommand.Split(new string[] { SID_TAG }, StringSplitOptions.None)[1].Substring(1).Split('}')[0];
    }


    public List<SpeckleObject> ExtractObjects(string fileName, string directory)
    {
      return ExtractObjects(new string[] { fileName }, directory);
    }

    public List<SpeckleObject> ExtractObjects(string[] fileNames, string directory)
    {
      var speckleObjects = new List<SpeckleObject>();
      foreach (var fileName in fileNames)
      {
        var json = Helper.ReadFile(fileName, directory);

        var response = ResponseObject.FromJson(json);
        speckleObjects.AddRange(response.Resources);
      }
      return speckleObjects;
    }

    #endregion
  }
}

