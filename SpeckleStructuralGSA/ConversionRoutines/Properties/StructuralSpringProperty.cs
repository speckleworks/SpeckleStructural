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
  [GSAObject("PROP_SPR.4", new string[] { }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSASpringProperty : GSABase<StructuralSpringProperty>
  {
    public void ParseGWACommand()
    {
      // GSA documentation of this GWA command is almost useless
      // save a GSA file as GWA to learn about it as GSA creates it
      
      if (this.GWACommand == null)
        return;

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var obj = new StructuralSpringProperty();

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; //Skip colour

      var springPropertyType = pieces[counter++];

      var stiffnesses = new double[6];
      var dampingRatio = 0d;
      switch (springPropertyType.ToLower())
      {
        case "axial":
          obj.SpringType = StructuralSpringPropertyType.Axial;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;

        case "compression":
          obj.SpringType = StructuralSpringPropertyType.Compression;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;

        case "tension":
          obj.SpringType = StructuralSpringPropertyType.Tension;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;

        case "gap":
          obj.SpringType = StructuralSpringPropertyType.Gap;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;

        case "friction":
          obj.SpringType = StructuralSpringPropertyType.Friction;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          double.TryParse(pieces[counter++], out stiffnesses[1]);
          double.TryParse(pieces[counter++], out stiffnesses[2]);
          counter++; //Coefficient of friction, not supported yet
          break;

        case "torsional":
          // TODO: As of build 48 of GSA, the torsional stiffness is not extracted in GWA records
          //return;
          obj.SpringType = StructuralSpringPropertyType.Torsional;
          double.TryParse(pieces[counter++], out stiffnesses[3]);
          break;

        case "lockup":
          obj.SpringType = StructuralSpringPropertyType.Lockup;
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;

        case "general":
          // Speckle spring currently only supports linear springs
          obj.SpringType = StructuralSpringPropertyType.General;
          counter--;
          for (var i = 0; i < 6; i++)
          {
            double.TryParse(pieces[counter += 2], out stiffnesses[i]);
          }
          counter++;
          double.TryParse(pieces[counter], out dampingRatio);
          break;

        default:
          return;
      };

      obj.Stiffness = new StructuralVectorSix(stiffnesses);

      double.TryParse(pieces[counter++], out dampingRatio);
      //Found some extremely small floating point issues so rounding to (arbitrarily-chosen) 4 digits
      obj.DampingRatio = Math.Round(dampingRatio, 4);

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSASpringProperty);

      var springProp = this.Value as StructuralSpringProperty;
      if (springProp.SpringType == StructuralSpringPropertyType.NotSet)
        return "";

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, springProp.ApplicationId);

      var gwaCommands = new List<string>();

      var sid = Helper.GenerateSID(springProp);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        string.IsNullOrEmpty(springProp.Name) ? "" : springProp.Name,
        "NO_RGB"
      };

      ls.AddRange(SpringTypeCommandPieces(springProp.SpringType, springProp.Stiffness, springProp.DampingRatio ?? 0));

      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      return string.Join("\n", gwaCommands);
    }

    private List<string> SpringTypeCommandPieces(StructuralSpringPropertyType structuralSpringPropertyType, StructuralVectorSix stiffness, double dampingRatio)
    {
      var dampingRatioStr = dampingRatio.ToString();

      var stiffnessToUse = (stiffness == null) ? new StructuralVectorSix(new double[] { 0, 0, 0, 0, 0, 0 }) : stiffness;

      switch (structuralSpringPropertyType)
      {
        case StructuralSpringPropertyType.Torsional:
          return new List<string> { "TORSIONAL", stiffnessToUse.Value[3].ToString(), dampingRatioStr }; //xx stiffness only

        case StructuralSpringPropertyType.Tension:
          return new List<string> { "TENSION", stiffnessToUse.Value[0].ToString(), dampingRatioStr };

        case StructuralSpringPropertyType.Compression:
          return new List<string> { "COMPRESSION", stiffnessToUse.Value[0].ToString(), dampingRatioStr };

        //Pasting GWA commands for CONNECT doesn't seem to work yet in GSA
        //case StructuralSpringPropertyType.Connector:
        //  return new List<string> { "CONNECT", "0", dampingRatioStr }; // Not sure what the argument after CONNECT is

        case StructuralSpringPropertyType.Lockup:
          return new List<string> { "LOCKUP", stiffnessToUse.Value[0].ToString(), dampingRatioStr, "0", "0" }; // Not sure what the last two arguments are

        case StructuralSpringPropertyType.Gap:
          return new List<string> { "GAP", stiffnessToUse.Value[0].ToString(), dampingRatioStr };

        case StructuralSpringPropertyType.Axial:
          return new List<string> { "AXIAL", stiffnessToUse.Value[0].ToString(), dampingRatioStr };

        case StructuralSpringPropertyType.Friction:
          //Coeff of friction (2nd-last) isn't supported yet
          return new List<string> { "FRICTION", stiffnessToUse.Value[0].ToString(), stiffness.Value[1].ToString(), stiffnessToUse.Value[2].ToString(), "0", dampingRatioStr };

        default:
          var ls = new List<string>() { "GENERAL" };
          for (var i = 0; i < 6; i++)
          {
            ls.Add("0"); //Curve
            ls.Add((stiffnessToUse == null) ? "0" : stiffnessToUse.Value[i].ToString());
          }
          ls.Add(dampingRatioStr);
          return ls;
      }
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralSpringProperty prop)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(prop, () => new GSASpringProperty() { Value = prop }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSASpringProperty dummyObject)
    {
      var newLines = ToSpeckleBase<GSASpringProperty>();
      var typeName = dummyObject.GetType().Name;
      var keyword = dummyObject.GetGSAKeyword();

      var springPropLock = new object();
      //Get all relevant GSA entities in this entire model
      var springProperties = new SortedDictionary<int, GSASpringProperty>();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        try
        {
          var springProperty = new GSASpringProperty() { GWACommand = newLines[k] };
          springProperty.ParseGWACommand();
          lock (springPropLock)
          {
            springProperties.Add(k, springProperty);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GsaKit.GSASenderObjects.AddRange(springProperties.Values.ToList());

      return (springProperties.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
