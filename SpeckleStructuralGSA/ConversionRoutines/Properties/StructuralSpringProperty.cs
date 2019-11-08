using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_SPR.3", new string[] { }, "properties", true, true, new Type[] { }, new Type[] { })]
  public class GSASpringProperty : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralSpringProperty();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var pieces = this.GWACommand.ListSplit("\t");

      var obj = new StructuralSpringProperty();

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = HelperClass.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; //Skip colour
      var gsaAxis = pieces[counter++];

      if (gsaAxis == "GLOBAL")
        obj.Axis = HelperClass.Parse0DAxis(0, Initialiser.Interface, out var gwaRec);
      else if (gsaAxis == "VERTICAL")
        obj.Axis = HelperClass.Parse0DAxis(-14, Initialiser.Interface, out var gwaRec);
      else
      {
        obj.Axis = HelperClass.Parse0DAxis(Convert.ToInt32(gsaAxis), Initialiser.Interface, out var gwaRec);
        this.SubGWACommand.Add(gwaRec);
      }

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

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var destType = typeof(GSASpringProperty);

      var springProp = this.Value as StructuralSpringProperty;

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.Indexer.ResolveIndex(keyword, springProp.Type, springProp.ApplicationId);

      var gwaAxisCommand = "";
      var gwaCommands = new List<string>();

      var axisRef = "GLOBAL";

      if (springProp.Axis == null)
      {
        //Default value
        axisRef = "GLOBAL";
      }
      else
      {
        if (springProp.Axis.Xdir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          springProp.Axis.Ydir.Value.SequenceEqual(new double[] { 0, 1, 0 }) &&
          springProp.Axis.Normal.Value.SequenceEqual(new double[] { 0, 0, 1 }))
          axisRef = "GLOBAL";
        else if (springProp.Axis.Xdir.Value.SequenceEqual(new double[] { 0, 0, 1 }) &&
          springProp.Axis.Ydir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          springProp.Axis.Normal.Value.SequenceEqual(new double[] { 0, 1, 0 }))
          axisRef = "VERTICAL";
        else
          try
          {
            HelperClass.SetAxis(springProp.Axis, out var axisIndex, out gwaAxisCommand, springProp.Name);
            if (gwaAxisCommand.Length > 0)
            {
              gwaCommands.Add(gwaAxisCommand);
            }
          }
          catch { axisRef = "GLOBAL"; }
      }

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + HelperClass.GenerateSID(springProp),
        index.ToString(),
        string.IsNullOrEmpty(springProp.Name) ? "" : springProp.Name,
        "NO_RGB",
        axisRef
      };

      ls.AddRange(SpringTypeCommandPieces(springProp.SpringType, springProp.Stiffness, springProp.DampingRatio ?? 0));

      gwaCommands.Add(string.Join("\t", ls));

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
      return new GSASpringProperty() { Value = prop }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSASpringProperty dummyObject)
    {
      var newLines = ToSpeckleBase<GSASpringProperty>();

      //Get all relevant GSA entities in this entire model
      var springProperties = new List<GSASpringProperty>();

      foreach (var p in newLines)
      {
        try
        {
          var springProperty = new GSASpringProperty() { GWACommand = p };
          springProperty.ParseGWACommand();
          springProperties.Add(springProperty);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[typeof(GSASpringProperty)].AddRange(springProperties);

      return (springProperties.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
