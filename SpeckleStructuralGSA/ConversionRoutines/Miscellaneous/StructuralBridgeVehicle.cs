using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("USER_VEHICLE.1", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSABridgeVehicle : GSABase<StructuralBridgeVehicle>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralBridgeVehicle();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      //TO DO : replace these defaults with the real thing
      obj.Width = 0;

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSABridgeVehicle);

      var vehicle = this.Value as StructuralBridgeVehicle;
      if (vehicle.ApplicationId == null)
      {
        return "";
      }

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, vehicle.ApplicationId);

      //The width parameter is intentionally not being used here as the meaning doesn't map to the y coordinate parameter of the ASSEMBLY keyword
      //It is therefore to be ignored here for GSA purposes.

      var sid = Helper.GenerateSID(vehicle);
      //the sid shouldn't be blank because the applicationId never being negative due to the test earlier, but the check below
      //has been included for consistency with other conversion code
      var ls = new List<string>
        {
          "SET",
          keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),  
          index.ToString(),
          string.IsNullOrEmpty(vehicle.Name) ? "" : vehicle.Name,
          ((vehicle.Width == null) ? 0 : vehicle.Width).ToString(),
          ((vehicle.Axles == null) ? 0 : vehicle.Axles.Count()).ToString()
      };

      if (vehicle.Axles != null && vehicle.Axles.Count() > 0)
      {
        foreach (var axle in vehicle.Axles)
        {
          ls.AddRange(new[] {
          axle.Position.ToString(),
          axle.WheelOffset.ToString(),
          axle.LeftWheelLoad.ToString(),
          axle.RightWheelLoad.ToString() });
        }
      }

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralBridgeVehicle assembly)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(assembly, () => new GSABridgeVehicle() { Value = assembly }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSABridgeVehicle dummyObject)
    {
      var newLines = ToSpeckleBase<GSABridgeVehicle>();
      var typeName = dummyObject.GetType().Name;
      var alignmentsLock = new object();
      //Get all relevant GSA entities in this entire model
      var alignments = new SortedDictionary<int, GSABridgeVehicle>();
      var keyword = dummyObject.GetGSAKeyword();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var p = newLines[k];
        var alignment = new GSABridgeVehicle() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        try
        {
          alignment.ParseGWACommand();
          lock (alignmentsLock)
          {
            alignments.Add(k, alignment);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(alignments.Values.ToList());

      return (alignments.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
