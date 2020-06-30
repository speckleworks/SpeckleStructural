using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Practices.Prism.Regions;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using MathNet.Spatial.Euclidean;
using System.Runtime.InteropServices;

namespace SpeckleStructuralGSA
{
  [GSAObject("GRID_LINE.1", new string[] { }, "misc", true, false, new Type[] { }, new Type[] { })]
  public class GSAGridLine : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralReferenceLine();

    public void ParseGWACommand()
    {
      var gwaGridLineCommand = GWACommand;
      if (gwaGridLineCommand == null || string.IsNullOrEmpty(gwaGridLineCommand))
      {
        return;
      }

      var pieces = gwaGridLineCommand.ListSplit("\t");

      var counter = 1; // Skip identifier

      var gsaId = Convert.ToInt32(pieces[counter++]);
      var applicationId = Helper.GetApplicationId(this.GetGSAKeyword(), gsaId);
      var name = pieces[counter++].Trim(new char[] { '"' });

      if (pieces[counter++].Equals("arc", StringComparison.InvariantCultureIgnoreCase))
      {
        //Doesn't support arc reference lines at this stage
        return;
      }

      var x1 = pieces[counter++].ToDouble();
      var y1 = pieces[counter++].ToDouble();
      var lineLen = pieces[counter++].ToDouble();

      if (lineLen == 0)
      {
        return;
      }

      var angleDegrees = pieces[counter++].ToDouble();
      var angleRadians = angleDegrees.ToRadians();

      var x2 = lineLen * Math.Cos(angleRadians);
      var y2 = lineLen * Math.Sin(angleRadians);

      this.Value.ApplicationId = applicationId;
      this.Value.Name = name;
      this.Value.Value = new List<double> { x1, y1, 0, x2, y2, 0 };

    }

    public string SetGWACommand()
    {
      if (this.Value == null)
      {
        return "";
      }

      var destType = typeof(GSAGridLine);

      var gridLine = this.Value as StructuralReferenceLine;
      if (gridLine.ApplicationId == null)
      {
        return "";
      }

      var keyword = destType.GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(keyword, gridLine.ApplicationId);

      //The width parameter is intentionally not being used here as the meaning doesn't map to the y coordinate parameter of the ASSEMBLY keyword
      //It is therefore to be ignored here for GSA purposes.

      var line = new Line2D(new Point2D(gridLine.Value[0], gridLine.Value[1]), new Point2D(gridLine.Value[3], gridLine.Value[4]));
      var angleDegrees = (new Vector2D(1, 0)).AngleTo(line.Direction).Degrees;

      var ls = new List<string>
        {
          "SET",
          keyword + ":" + Helper.GenerateSID(gridLine),
          index.ToString(),
          string.IsNullOrEmpty(gridLine.Name) ? "" : gridLine.Name,
          "LINE",
          gridLine.Value[0].ToString(),
          gridLine.Value[1].ToString(),
          angleDegrees.ToString(),
          "0" //ignored as the angle is in degrees
      };

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this StructuralReferenceLine gs)
    {
      return new GSAGridLine() { Value = gs }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSAGridLine dummyObject)
    {
      var newLines = ToSpeckleBase<GSAGridLine>();

      var rlLock = new object();
      //Get all relevant GSA entities in this entire model
      var rls = new List<GSAGridLine>();

      Parallel.ForEach(newLines.Values, p =>
      {
        var rl = new GSAGridLine() { GWACommand = p };
        //Pass in ALL the nodes and members - the Parse_ method will search through them
        rl.ParseGWACommand();
        lock (rlLock)
        {
          rls.Add(rl);
        }
      });

      Initialiser.GSASenderObjects.AddRange(rls);

      return (rls.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
