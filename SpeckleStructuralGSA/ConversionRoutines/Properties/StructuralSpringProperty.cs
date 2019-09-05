using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
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

    public void ParseGWACommand(GSAInterfacer GSA)
    {
      if (this.GWACommand == null)
        return;

      var pieces = this.GWACommand.ListSplit("\t");

      var obj = new StructuralSpringProperty();

      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; //Skip colour
      string gsaAxis = pieces[counter++];

      if (gsaAxis == "GLOBAL")
        obj.Axis = GSA.Parse0DAxis(0, out string gwaRec);
      else if (gsaAxis == "VERTICAL")
        obj.Axis = GSA.Parse0DAxis(-14, out string gwaRec);
      else
      {
        obj.Axis = GSA.Parse0DAxis(Convert.ToInt32(gsaAxis), out string gwaRec);
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

    public void SetGWACommand(GSAInterfacer GSA)
    {
      if (this.Value == null)
        return;

      Type destType = typeof(GSASpringProperty);

      StructuralSpringProperty springProp = this.Value as StructuralSpringProperty;

      string keyword = destType.GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(destType, springProp);

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
          axisRef = GSA.SetAxis(springProp.Axis, springProp.Name).ToString();
        }
        catch { axisRef = "GLOBAL"; }


      List<string> ls = new List<string>
      {
        "SET",
        keyword + ":" + GSA.GenerateSID(springProp),
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

      GSA.RunGWACommand(string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static bool ToNative(this StructuralSpringProperty prop)
    {
      if (Conversions.GSATargetLayer == GSATargetLayer.Analysis)
        new GSASpringProperty() { Value = prop }.SetGWACommand(GSA);
      else if (Conversions.GSATargetLayer == GSATargetLayer.Design)
        new GSASpringProperty() { Value = prop }.SetGWACommand(GSA);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSASpringProperty dummyObject)
    {
      Type objType = dummyObject.GetType();

      if (!GSASenderObjects.ContainsKey(objType))
        GSASenderObjects[objType] = new List<object>();

      //Get all relevant GSA entities in this entire model
      var springProperties = new List<GSASpringProperty>();

      string keyword = objType.GetGSAKeyword();
      string[] subKeywords = objType.GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[objType].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[objType].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        try
        {
          var springProperty = new GSASpringProperty() { GWACommand = p };
          springProperty.ParseGWACommand(GSA);
          springProperties.Add(springProperty);
        }
        catch { }
      }

      GSASenderObjects[objType].AddRange(springProperties);

      if (springProperties.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
