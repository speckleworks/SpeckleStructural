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
    private List<Tuple<string, SpeckleObject>> receivedObjects;

    public ReceiverProcessor(string directory, IGSAAppResources appResources, GSATargetLayer layer = GSATargetLayer.Design) : base (directory)
    {
      this.appResources = appResources;
      //GSAInterfacer = gsaInterfacer;
      //GSACache = gsaCache;
      this.appResources.Settings.TargetLayer = layer;
    }

    public void JsonSpeckleStreamsToGwaRecords(IEnumerable<string> savedJsonFileNames, out List<GwaRecord> gwaRecords, GSATargetLayer layer)
    {
      gwaRecords = new List<GwaRecord>();

      receivedObjects = JsonSpeckleStreamsToSpeckleObjects(savedJsonFileNames);

      ScaleObjects();

      ConvertSpeckleObjectsToGsaInterfacerCache(layer);

      var gwaCommands = ((IGSACacheForTesting) this.appResources.Cache).GetGwaSetCommands();
      foreach (var gwaC in gwaCommands)
      {
        this.appResources.Proxy.ParseGeneralGwa(gwaC, out var keyword, out int? index, out var streamId, out var applicationId, out var gwaWithoutSet, out GwaSetCommandType? gwaSetType);
        gwaRecords.Add(new GwaRecord(string.IsNullOrEmpty(applicationId) ? null : applicationId, gwaC));
      }
    }

    #region private_methods    

    private List<Tuple<string,SpeckleObject>> JsonSpeckleStreamsToSpeckleObjects(IEnumerable<string> savedJsonFileNames)
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
          o.Item2.Scale(scaleFactor);
        }
        catch { }
      }
    }

    private void ConvertSpeckleObjectsToGsaInterfacerCache(GSATargetLayer layer)
    {
      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      var TypePrerequisites = Helper.GetTypeCastPriority(ioDirection.Receive, layer, false);
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);
          var keyword = dummyObject.GetAttribute<GSAObject>("GSAKeyword").ToString();
          var valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var speckleTypeName = valueType.GetType().Name;
          var targetObjects = receivedObjects.Where(o => o.Item2.GetType() == valueType).ToList();

          for (var i = 0; i < targetObjects.Count(); i++)
          {
            var streamId = targetObjects[i].Item1;
            var obj = targetObjects[i].Item2;

            //DESERIALISE
            var deserialiseReturn = ((string)Converter.Deserialise(obj));
            var gwaCommands = deserialiseReturn.Split(new[] { '\n' }).Where(c => c.Length > 0).ToList();

            for (var j = 0; j < gwaCommands.Count(); j++)
            {
              appResources.Proxy.ParseGeneralGwa(gwaCommands[j], out keyword, out int? foundIndex, out var foundStreamId, out var foundApplicationId, 
                out var gwaWithoutSet, out var gwaSetCommandType);

              //Only cache the object against, the top-level GWA command, not the sub-commands
              ((IGSACache)appResources.Cache).Upsert(keyword, foundIndex.Value, gwaWithoutSet, applicationId: foundApplicationId, 
                so: (foundApplicationId == obj.ApplicationId) ? obj : null, gwaSetCommandType: gwaSetCommandType.Value);
            }
          }

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      var toBeAddedGwa = ((IGSACache)appResources.Cache).GetNewGwaSetCommands();
      for (int i = 0; i < toBeAddedGwa.Count(); i++)
      {
        appResources.Proxy.SetGwa(toBeAddedGwa[i]);
      }
    }

    public List<Tuple<string, SpeckleObject>> ExtractObjects(string fileName, string directory)
    {
      return ExtractObjects(new string[] { fileName }, directory);
    }

    public List<Tuple<string,SpeckleObject>> ExtractObjects(string[] fileNames, string directory)
    {
      var speckleObjects = new List<Tuple<string, SpeckleObject>>();
      foreach (var fileName in fileNames)
      {
        var json = Helper.ReadFile(fileName, directory);
        var streamId = fileName.Split(new[] { '.' }).First();

        var response = ResponseObject.FromJson(json);
        for (int i = 0; i < response.Resources.Count(); i++)
        {
          speckleObjects.Add(new Tuple<string, SpeckleObject>(streamId, response.Resources[i]));
        }
      }
      return speckleObjects;
    }

    #endregion
  }
}

