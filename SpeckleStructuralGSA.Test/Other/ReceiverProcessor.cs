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

    private void ConvertSpeckleObjectsToGsaInterfacerCache()
    {
      // Write objects
      var currentBatch = new List<Type>();
      var traversedTypes = new List<Type>();
      var TypePrerequisites = Helper.GetTypeCastPriority(ioDirection.Receive, GSATargetLayer.Design, false);
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (var t in currentBatch)
        {
          var dummyObject = Activator.CreateInstance(t);
          var keyword = dummyObject.GetAttribute("GSAKeyword").ToString();
          var valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var speckleTypeName = ((SpeckleObject)((IGSASpeckleContainer)dummyObject).Value).Type;
          var targetObjects = receivedObjects.Where(o => o.Item2.GetType() == valueType).ToList();

          for (var i = 0; i < targetObjects.Count(); i++)
          {
            var streamId = targetObjects[i].Item1;
            var obj = targetObjects[i].Item2;
            var sidValue = streamId + "|" + obj.ApplicationId;

            //DESERIALISE
            var deserialiseReturn = ((string)Converter.Deserialise(obj));
            var gwaCommands = deserialiseReturn.Split(new[] { '\n' }).Where(c => c.Length > 0).ToList();

            for (var j = 0; j < gwaCommands.Count(); j++)
            {
              gwaCommands[j].ExtractKeywordApplicationId(out keyword, out int? foundIndex, out string sid, out string gwaWithoutSet, out GwaSetCommandType? gwaSetCommandType);

              GSAInterfacer.SetGWA(gwaCommands[j]);

              //Only cache the object against, the top-level GWA command, not the sub-commands
              GSACache.Upsert(keyword, foundIndex.Value, gwaWithoutSet, sid, (sid == sidValue) ? obj : null);
            }
          }


          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);
    }

    private string ExtractApplicationId(string gwaCommand)
    {
      if (!gwaCommand.Contains(SID_TAG))
      {
        return null;
      }
      return gwaCommand.Split(new string[] { SID_TAG }, StringSplitOptions.None)[1].Substring(1).Split('}')[0];
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

