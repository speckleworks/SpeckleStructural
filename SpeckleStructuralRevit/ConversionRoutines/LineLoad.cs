using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using SpeckleCore;
using SpeckleStructuralClasses;

namespace SpeckleStructuralRevit
{
  public static partial class Conversions
  {
    //TODO
    public static Element ToNative(this Structural1DLoadLine myLoad)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this Autodesk.Revit.DB.Structure.LineLoad myLineLoad)
    {
      var myLoad = new Structural1DLoadLine();

      myLoad.Name = myLineLoad.Name;

      var points = myLineLoad.GetCurve().Tessellate();

      myLoad.Value = new List<double>();

      foreach (XYZ p in points)
      {
        myLoad.Value.Add(p.X / Scale);
        myLoad.Value.Add(p.Y / Scale);
        myLoad.Value.Add(p.Z / Scale);
      }

      var forces = new StructuralVectorSix(new double[6]);

      forces.Value[0] = (myLineLoad.ForceVector1.X + myLineLoad.ForceVector2.X) / 2;
      forces.Value[1] = (myLineLoad.ForceVector1.Y + myLineLoad.ForceVector2.Y) / 2;
      forces.Value[2] = (myLineLoad.ForceVector1.Z + myLineLoad.ForceVector2.Z) / 2;
      forces.Value[3] = (myLineLoad.MomentVector1.X + myLineLoad.MomentVector2.X) / 2;
      forces.Value[4] = (myLineLoad.MomentVector1.Y + myLineLoad.MomentVector2.Y) / 2;
      forces.Value[5] = (myLineLoad.MomentVector1.Z + myLineLoad.MomentVector2.Z) / 2;

      if (myLineLoad.OrientTo == LoadOrientTo.HostLocalCoordinateSystem)
      {
        var hostTransform = myLineLoad.HostElement.GetLocalCoordinateSystem();

        XYZ b0 = hostTransform.get_Basis(0);
        XYZ b1 = hostTransform.get_Basis(1);
        XYZ b2 = hostTransform.get_Basis(2);

        double fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        double fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        double fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;
        double mx = forces.Value[3] * b0.X + forces.Value[4] * b1.X + forces.Value[5] * b2.X;
        double my = forces.Value[3] * b0.Y + forces.Value[4] * b1.Y + forces.Value[5] * b2.Y;
        double mz = forces.Value[3] * b0.Z + forces.Value[4] * b1.Z + forces.Value[5] * b2.Z;

        forces = new StructuralVectorSix(new double[] { fx, fy, fz, mx, my, mz });
      }
      else if (myLineLoad.OrientTo == LoadOrientTo.WorkPlane)
      {
        var workPlane = ((Autodesk.Revit.DB.SketchPlane)Doc.GetElement(myLineLoad.WorkPlaneId)).GetPlane();
        
        XYZ b0 = workPlane.XVec;
        XYZ b1 = workPlane.YVec;
        XYZ b2 = workPlane.Normal;

        double fx = forces.Value[0] * b0.X + forces.Value[1] * b1.X + forces.Value[2] * b2.X;
        double fy = forces.Value[0] * b0.Y + forces.Value[1] * b1.Y + forces.Value[2] * b2.Y;
        double fz = forces.Value[0] * b0.Z + forces.Value[1] * b1.Z + forces.Value[2] * b2.Z;
        double mx = forces.Value[3] * b0.X + forces.Value[4] * b1.X + forces.Value[5] * b2.X;
        double my = forces.Value[3] * b0.Y + forces.Value[4] * b1.Y + forces.Value[5] * b2.Y;
        double mz = forces.Value[3] * b0.Z + forces.Value[4] * b1.Z + forces.Value[5] * b2.Z;

        forces = new StructuralVectorSix(new double[] { fx, fy, fz, mx, my, mz });
      }

      myLoad.Loading = forces;

      var myLoadCase = new StructuralLoadCase();
      myLoadCase.Name = myLineLoad.LoadCaseName;
      myLoadCase.ApplicationId = myLineLoad.LoadCase.UniqueId;
      switch (myLineLoad.LoadCategoryName)
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

      myLoad.LoadCaseRef = myLoadCase.ApplicationId;
      myLoad.ApplicationId = myLineLoad.UniqueId;

      return new List<SpeckleObject>() { myLoad, myLoadCase };
    }
  }
}
