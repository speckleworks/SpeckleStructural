using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleStructuralGSA.Test
{
  //Copied from the Receiver class in SpeckleGSA - this will be refactored to simplify and avoid dependency
  public class ReceiverProcessor
  {
    public Dictionary<Type, List<Type>> TypePrerequisites = new Dictionary<Type, List<Type>>();
    public List<KeyValuePair<Type, List<Type>>> TypeCastPriority = new List<KeyValuePair<Type, List<Type>>>();

    public async Task Initialize()
    {
      // Run initialize receiver method in interfacer
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies();

      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.GetInterfaces().Contains(typeof(SpeckleCore.ISpeckleInitializer)))
          {
            if (type.GetProperties().Select(p => p.Name).Contains("GSA"))
            {
              try
              {
                var gsaInterface = type.GetProperty("GSA").GetValue(null);
                gsaInterface.GetType().GetMethod("InitializeSender").Invoke(gsaInterface, new object[] { GSA.GSAObject });
              }
              catch
              {
                throw new Exception("Unable to initialize");
              }
            }
          }
        }
      }

      // Grab GSA interface and attribute type
      Type interfaceType = null;
      Type attributeType = null;
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.FullName.Contains("IGSASpeckleContainer"))
          {
            interfaceType = type;
          }

          if (type.FullName.Contains("GSAObject"))
          {
            attributeType = type;
          }
        }
      }

      if (interfaceType == null)
        return;

      // Grab all GSA related object
      List<Type> objTypes = new List<Type>();
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
          if (interfaceType.IsAssignableFrom(type) && type != interfaceType)
            objTypes.Add(type);
      }

      foreach (Type t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if (Settings.TargetAnalysisLayer && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if (Settings.TargetDesignLayer && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        List<Type> prereq = new List<Type>();
        if (t.GetAttribute("WritePrerequisite", attributeType) != null)
          prereq = ((Type[])t.GetAttribute("WritePrerequisite", attributeType)).ToList();

        TypePrerequisites[t] = prereq;
      }

      // Remove wrong layer objects from prerequisites
      foreach (Type t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if (Settings.TargetAnalysisLayer && !(bool)t.GetAttribute("AnalysisLayer", attributeType))
            foreach (KeyValuePair<Type, List<Type>> kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if (Settings.TargetDesignLayer && !(bool)t.GetAttribute("DesignLayer", attributeType))
            foreach (KeyValuePair<Type, List<Type>> kvp in TypePrerequisites)
              kvp.Value.Remove(t);
      }

      // Generate which GSA object to cast for each type
      TypeCastPriority = TypePrerequisites.ToList();
      TypeCastPriority.Sort((x, y) => x.Value.Count().CompareTo(y.Value.Count()));

      // Get Indexer
      object indexer = null;
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.GetInterfaces().Contains(typeof(SpeckleCore.ISpeckleInitializer)))
          {
            if (type.GetProperties().Select(p => p.Name).Contains("GSA"))
            {
              try
              {
                var gsaInterface = type.GetProperty("GSA").GetValue(null);
                indexer = gsaInterface.GetType().GetField("Indexer").GetValue(gsaInterface);
              }
              catch
              {
                Status.AddError("Unable to access kit. Try updating Speckle installation to a later release.");
                throw new Exception("Unable to initialize");
              }
            }
          }
        }
      }

      // Add existing GSA file objects to counters
      foreach (KeyValuePair<Type, List<Type>> kvp in TypePrerequisites)
      {
        try
        {
          List<string> keywords = new List<string>() { (string)kvp.Key.GetAttribute("GSAKeyword", attributeType) };
          keywords.AddRange((string[])kvp.Key.GetAttribute("SubGSAKeywords", attributeType));

          foreach (string k in keywords)
          {
            int highestRecord = (int)GSA.GSAObject.GwaCommand("HIGHEST\t" + k);

            if (highestRecord > 0)
              indexer.GetType().GetMethod("ReserveIndices", new Type[] { typeof(string), typeof(List<int>) }).Invoke(indexer, new object[] { k, Enumerable.Range(1, highestRecord).ToList() });
          }
        }
        catch { }
      }
      indexer.GetType().GetMethod("SetBaseline").Invoke(indexer, new object[] { });

      // Create receivers
      Status.ChangeStatus("Accessing stream");

      foreach (Tuple<string, string> streamInfo in GSA.Receivers)
      {
        if (streamInfo.Item1 == "")
          Status.AddMessage("No stream specified.");
        else
        {
          Status.AddMessage("Creating receiver " + streamInfo.Item1);
          Receivers[streamInfo.Item1] = new SpeckleGSAReceiver(restApi, apiToken);
          await Receivers[streamInfo.Item1].InitializeReceiver(streamInfo.Item1, streamInfo.Item2);
          Receivers[streamInfo.Item1].UpdateGlobalTrigger += Trigger;
        }
      }

      Status.ChangeStatus("Ready to receive");
      IsInit = true;
    }

    /// <summary>
    /// Trigger to update stream. Is called automatically when update-global ws message is received on stream.
    /// </summary>
    public void Trigger(object sender, EventArgs e)
    {
      if (IsBusy) return;
      if (!IsInit) return;

      IsBusy = true;
      GSA.UpdateUnits();

      // Run pre receiving method and inject!!!!
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies();
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.GetInterfaces().Contains(typeof(SpeckleCore.ISpeckleInitializer)))
          {
            try
            {
              if (type.GetProperties().Select(p => p.Name).Contains("GSA"))
              {
                var gsaInterface = type.GetProperty("GSA").GetValue(null);

                gsaInterface.GetType().GetMethod("PreReceiving").Invoke(gsaInterface, new object[] { });
              }

              if (type.GetProperties().Select(p => p.Name).Contains("GSAUnits"))
                type.GetProperty("GSAUnits").SetValue(null, GSA.Units);

              if (type.GetProperties().Select(p => p.Name).Contains("GSACoincidentNodeAllowance"))
                type.GetProperty("GSACoincidentNodeAllowance").SetValue(null, Settings.CoincidentNodeAllowance);

              if (Settings.TargetDesignLayer)
                if (type.GetProperties().Select(p => p.Name).Contains("GSATargetDesignLayer"))
                  type.GetProperty("GSATargetDesignLayer").SetValue(null, true);

              if (Settings.TargetAnalysisLayer)
                if (type.GetProperties().Select(p => p.Name).Contains("GSATargetAnalysisLayer"))
                  type.GetProperty("GSATargetAnalysisLayer").SetValue(null, true);
            }
            catch
            {
              Status.AddError("Unable to access kit. Try updating Speckle installation to a later release.");
              throw new Exception("Unable to trigger");
            }
          }
        }
      }

      List<SpeckleObject> objects = new List<SpeckleObject>();

      // Read objects
      foreach (KeyValuePair<string, SpeckleGSAReceiver> kvp in Receivers)
      {
        try
        {
          Status.ChangeStatus("Receiving stream");
          var receivedObjects = Receivers[kvp.Key].GetObjects().Distinct();

          Status.ChangeStatus("Scaling objects");
          double scaleFactor = (1.0).ConvertUnit(Receivers[kvp.Key].Units.ShortUnitName(), GSA.Units);

          foreach (SpeckleObject o in receivedObjects)
          {
            try
            {
              o.Scale(scaleFactor);
            }
            catch { }
          }
          objects.AddRange(receivedObjects);
        }
        catch { Status.AddError("Unable to get stream " + kvp.Key); }
      }

      // Get Indexer
      object indexer = null;
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.GetInterfaces().Contains(typeof(SpeckleCore.ISpeckleInitializer)))
          {
            if (type.GetProperties().Select(p => p.Name).Contains("GSA"))
            {
              try
              {
                var gsaInterface = type.GetProperty("GSA").GetValue(null);
                indexer = gsaInterface.GetType().GetField("Indexer").GetValue(gsaInterface);
              }
              catch
              {
                Status.AddError("Unable to access kit. Try updating Speckle installation to a later release.");
                throw new Exception("Unable to trigger");
              }
            }
          }
        }
      }

      // Add existing GSA file objects to counters
      indexer.GetType().GetMethod("ResetToBaseline").Invoke(indexer, new object[] { });

      // Write objects
      List<Type> currentBatch = new List<Type>();
      List<Type> traversedTypes = new List<Type>();
      do
      {
        currentBatch = TypePrerequisites.Where(i => i.Value.Count(x => !traversedTypes.Contains(x)) == 0).Select(i => i.Key).ToList();
        currentBatch.RemoveAll(i => traversedTypes.Contains(i));

        foreach (Type t in currentBatch)
        {
          Status.ChangeStatus("Writing " + t.Name);

          object dummyObject = Activator.CreateInstance(t);

          Type valueType = t.GetProperty("Value").GetValue(dummyObject).GetType();
          var targetObjects = objects.Where(o => o.GetType() == valueType);
          Converter.Deserialise(targetObjects);

          objects.RemoveAll(x => targetObjects.Any(o => x == o));

          traversedTypes.Add(t);
        }
      } while (currentBatch.Count > 0);

      // Write leftover
      Converter.Deserialise(objects);

      // Run post receiving method
      foreach (var ass in assemblies)
      {
        var types = ass.GetTypes();
        foreach (var type in types)
        {
          if (type.GetInterfaces().Contains(typeof(SpeckleCore.ISpeckleInitializer)))
          {
            if (type.GetProperties().Select(p => p.Name).Contains("GSA"))
            {
              try
              {
                var gsaInterface = type.GetProperty("GSA").GetValue(null);
                gsaInterface.GetType().GetMethod("PostReceiving").Invoke(gsaInterface, new object[] { });
              }
              catch
              {
                Status.AddError("Unable to access kit. Try updating Speckle installation to a later release.");
                throw new Exception("Unable to trigger");
              }
            }
          }
        }
      }

      GSA.UpdateCasesAndTasks();
      GSA.UpdateViews();

      IsBusy = false;
      Status.ChangeStatus("Finished receiving", 100);
    }
  }
}
