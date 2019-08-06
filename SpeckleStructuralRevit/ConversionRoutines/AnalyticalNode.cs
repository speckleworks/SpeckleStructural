﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;

namespace SpeckleStructuralRevit
{
  public static partial class Conversions
  {
    //TODO
    public static Element ToNative(this StructuralNode myBeam)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this Autodesk.Revit.DB.Structure.BoundaryConditions myRestraint)
    {
      List<XYZ> points = new List<XYZ>();

      var restraintType = myRestraint.GetBoundaryConditionsType();

      if (restraintType == BoundaryConditionsType.Point)
      {
        var point = myRestraint.Point;
        points.Add(point);
      }
      else if (restraintType == BoundaryConditionsType.Line)
      {
        var curve = myRestraint.GetCurve();
        points.Add(curve.GetEndPoint(0));
        points.Add(curve.GetEndPoint(1));
      }
      else if (restraintType == BoundaryConditionsType.Area)
      {
        var loops = myRestraint.GetLoops();
        foreach (CurveLoop loop in loops)
        {
          foreach (Curve curve in loop)
          {
            points.Add(curve.GetEndPoint(0));
            points.Add(curve.GetEndPoint(1));
          }
        }
        points = points.Distinct().ToList();
      }

      var coordinateSystem = myRestraint.GetDegreesOfFreedomCoordinateSystem();
      var axis = new StructuralAxis(
        new StructuralVectorThree(new double[] { coordinateSystem.BasisX.X, coordinateSystem.BasisX.Y, coordinateSystem.BasisX.Z }),
        new StructuralVectorThree(new double[] { coordinateSystem.BasisY.X, coordinateSystem.BasisY.Y, coordinateSystem.BasisY.Z }),
        new StructuralVectorThree(new double[] { coordinateSystem.BasisZ.X, coordinateSystem.BasisZ.Y, coordinateSystem.BasisZ.Z })
      );

      var restraint = new StructuralVectorBoolSix(new bool[6]);
      var stiffness = new StructuralVectorSix(new double[6]);

      var listOfParams = new BuiltInParameter[] {
        BuiltInParameter.BOUNDARY_DIRECTION_X,
        BuiltInParameter.BOUNDARY_DIRECTION_Y,
        BuiltInParameter.BOUNDARY_DIRECTION_Z,
        BuiltInParameter.BOUNDARY_DIRECTION_ROT_X,
        BuiltInParameter.BOUNDARY_DIRECTION_ROT_Y,
        BuiltInParameter.BOUNDARY_DIRECTION_ROT_Z
      };

      var listOfSpringParams = new BuiltInParameter[] {
        BuiltInParameter.BOUNDARY_RESTRAINT_X,
        BuiltInParameter.BOUNDARY_RESTRAINT_Y,
        BuiltInParameter.BOUNDARY_RESTRAINT_Z,
        BuiltInParameter.BOUNDARY_RESTRAINT_ROT_X,
        BuiltInParameter.BOUNDARY_RESTRAINT_ROT_Y,
        BuiltInParameter.BOUNDARY_RESTRAINT_ROT_Z,
      };

      for (int i = 0; i < 6; i++)
      { 
        switch (myRestraint.get_Parameter(listOfParams[i]).AsInteger())
        {
          case 0:
            restraint.Value[i] = true;
            break;
          case 1:
            restraint.Value[i] = false;
            break;
          case 2:
            stiffness.Value[i] = myRestraint.get_Parameter(listOfSpringParams[i]).AsDouble();
            break;
        }
      }
      restraint.GenerateHash();
      stiffness.GenerateHash();

      List<SpeckleObject> myNodes = new List<SpeckleObject>();

      foreach(XYZ point in points)
      {
        var myPoint = (SpeckleCoreGeometryClasses.SpecklePoint)SpeckleCore.Converter.Serialise(point);
        var myNode = new StructuralNode();
        myNode.basePoint = myPoint;
        myNode.Axis = axis;
        myNode.Restraint = restraint;
        myNode.Stiffness = stiffness;
        myNodes.Add(myNode);
      }
      
      return myNodes;
    }
  }
}