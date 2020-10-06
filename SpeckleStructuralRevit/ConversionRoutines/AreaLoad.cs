using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using SpeckleCore;
using SpeckleStructuralClasses;

namespace SpeckleStructuralRevit
{
  public static partial class Conversions
  {
    //TODO
    public static Element ToNative(this Structural2DLoadPanel myLoad)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this AreaLoad myAreaLoad)
    {
      var polylines = new List<double[]>();
      
      var loops = myAreaLoad.GetLoops();
      foreach (var loop in loops)
      {
        var coor = new List<double>();
        foreach (var curve in loop)
        {
          var points = curve.Tessellate();

          foreach (var p in points.Skip(1))
          {
            coor.Add(p.X / Scale);
            coor.Add(p.Y / Scale);
            coor.Add(p.Z / Scale);
          }
        }

        polylines.Add(coor.ToArray());

        // Only get outer loop
        break;
      }

      var forces = new StructuralVectorThree(new double[3]);
      
      forces.Value[0] = myAreaLoad.ForceVector1.X;
      forces.Value[1] = myAreaLoad.ForceVector1.Y;
      forces.Value[2] = myAreaLoad.ForceVector1.Z;

      if (myAreaLoad.OrientTo == LoadOrientTo.HostLocalCoordinateSystem)
      {
        var hostTransform = myAreaLoad.HostElement.GetLocalCoordinateSystem();

        var b0 = hostTransform.get_Basis(0);
        var b1 = hostTransform.get_Basis(1);
        var b2 = hostTransform.get_Basis(2);

        var fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        var fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        var fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;

        forces = new StructuralVectorThree(new double[] { fx, fy, fz });
      }
      else if (myAreaLoad.OrientTo == LoadOrientTo.WorkPlane)
      {
        var workPlane = ((SketchPlane)Doc.GetElement(myAreaLoad.WorkPlaneId)).GetPlane();
        
        var b0 = workPlane.XVec;
        var b1 = workPlane.YVec;
        var b2 = workPlane.Normal;

        var fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        var fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        var fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;

        forces = new StructuralVectorThree(new double[] { fx, fy, fz });
      }

      var myLoadCase = new StructuralLoadCase
      {
        Name = myAreaLoad.LoadCaseName,
        ApplicationId = myAreaLoad.LoadCase.UniqueId
      };
      switch (myAreaLoad.LoadCategoryName)
      {
        case "Dead Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Dead;
          break;
        case "Live Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Live;
          break;
        case "Seismic Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Earthquake;
          break;
        case "Snow Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Snow;
          break;
        case "Wind Loads":
          myLoadCase.CaseType = StructuralLoadCaseType.Wind;
          break;
        default:
          myLoadCase.CaseType = StructuralLoadCaseType.Generic;
          break;
      }

      var myLoads = new List<SpeckleObject>();

      var counter = 0;
      foreach (var vals in polylines)
      {
        var myLoad = new Structural2DLoadPanel
        {
          Name = myAreaLoad.Name,
          Value = vals.ToList(),
          Loading = forces,
          LoadCaseRef = myLoadCase.ApplicationId,
          Closed = true,

          ApplicationId = Helper.CreateChildApplicationId(counter++, myAreaLoad.UniqueId)
        };

        myLoads.Add(myLoad);
      }
      
      return myLoads.Concat(new List<SpeckleObject>() { myLoadCase }).ToList();
    }
  }
}
