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

    public void ParseGWACommand(IGSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var pieces = this.GWACommand.ListSplit("\t");

      var obj = new StructuralSpringProperty();

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Initialiser.Interface.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; //Skip colour
      string gsaAxis = pieces[counter++];

      if (gsaAxis == "GLOBAL")
        obj.Axis = HelperClass.Parse0DAxis(0, Initialiser.Interface, out string gwaRec);
      else if (gsaAxis == "VERTICAL")
        obj.Axis = HelperClass.Parse0DAxis(-14, Initialiser.Interface, out string gwaRec);
      else
      {
        obj.Axis = HelperClass.Parse0DAxis(Convert.ToInt32(gsaAxis), Initialiser.Interface, out string gwaRec);
        this.SubGWACommand.Add(gwaRec);
      }

      var springPropertyType = pieces[counter++];

      var stiffnesses = new double[6];

      switch (springPropertyType.ToLower())
      {
        case "general":
          counter--;
          for (var i = 0; i < 6; i++)
            double.TryParse(pieces[counter += 2], out stiffnesses[i]);
          counter--;
          break;
        case "axial":
          double.TryParse(pieces[counter++], out stiffnesses[0]);
          break;
        case "torsional":
          // TODO: As of build 48 of GSA, the torsional stiffness is not extracted in GWA records
          return;
        default:
          return;
      };
      
      obj.Stiffness = new StructuralVectorSix(stiffnesses);
      this.Value = obj;
    }

    public void SetGWACommand(IGSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Type destType = typeof(GSASpringProperty);

      StructuralSpringProperty springProp = this.Value as StructuralSpringProperty;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(keyword, springProp.ApplicationId);

      string axisRef = "GLOBAL";

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
          axisRef = HelperClass.SetAxis(springProp.Axis, springProp.Name).ToString();
        }
        catch { axisRef = "GLOBAL"; }


      List<string> ls = new List<string>
      {
        "SET",
        keyword + ":" + HelperClass.GenerateSID(springProp),
        index.ToString(),
        string.IsNullOrEmpty(springProp.Name) ? "" : springProp.Name,
        "NO_RGB",
        axisRef,
        "GENERAL"
      };

      for (var i = 0; i < 6; i++)
      {
        ls.Add("0"); //Curve
        ls.Add(springProp.Stiffness.Value[i].ToString());
      }
      ls.Add("0");  //Damping ratio

      Initialiser.Interface.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralSpringProperty prop)
    {
      if (Initialiser.Settings.TargetLayer == GSATargetLayer.Analysis)
        new GSASpringProperty() { Value = prop }.SetGWACommand(Initialiser.Interface);
      else if (Initialiser.Settings.TargetLayer == GSATargetLayer.Design)
        new GSASpringProperty() { Value = prop }.SetGWACommand(Initialiser.Interface);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSASpringProperty dummyObject)
    {
      Type objType = dummyObject.GetType();

      if (!Initialiser.GSASenderObjects.ContainsKey(objType))
        Initialiser.GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var springProperties = new List<GSASpringProperty>();

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = Initialiser.Interface.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(Initialiser.Interface.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      Initialiser.GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (var kvp in Initialiser.GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = Initialiser.GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          var springProperty = new GSASpringProperty() { GWACommand = p };
          springProperty.ParseGWACommand(Initialiser.Interface);
          springProperties.Add(springProperty);
        }
        catch { }
      }

      Initialiser.GSASenderObjects[objType].AddRange(springProperties);

      if (springProperties.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
