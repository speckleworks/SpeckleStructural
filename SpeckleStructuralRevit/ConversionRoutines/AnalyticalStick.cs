using System;
using System.Collections.Generic;
using System.Linq;
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
    public static Element ToNative(this Structural1DElement myBeam)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this AnalyticalModelStick myStick)
    {
      var returnObjects = new List<SpeckleObject>();

      if (!myStick.IsEnabled())
        return new List<SpeckleObject>();

      // Get the family
      var myFamily = (FamilyInstance)Doc.GetElement(myStick.GetElementId());

      var myElement = new Structural1DElementPolyline
      {
        Value = new List<double>()
      };

      var curves = myStick.GetCurves(AnalyticalCurveType.RigidLinkHead).ToList();
      curves.AddRange(myStick.GetCurves(AnalyticalCurveType.ActiveCurves));
      curves.AddRange(myStick.GetCurves(AnalyticalCurveType.RigidLinkTail));

      foreach (var curve in curves)
      {
        var points = curve.Tessellate();

        if (points.Count == 0)
          continue;

        if (myElement.Value.Count == 0)
        {
          myElement.Value.Add(points[0].X / Scale);
          myElement.Value.Add(points[0].Y / Scale);
          myElement.Value.Add(points[0].Z / Scale);   
        }

        foreach (var p in points.Skip(1))
        {
          myElement.Value.Add(p.X / Scale);
          myElement.Value.Add(p.Y / Scale);
          myElement.Value.Add(p.Z / Scale);
        }
      }

      myElement.ResultVertices = new List<double>(myElement.Value);

      var vertexCount = myElement.Value.Count / 3;

      var coordinateSystem = myStick.GetLocalCoordinateSystem();
      if (coordinateSystem != null)
        myElement.ZAxis = Enumerable.Repeat(new StructuralVectorThree(new double[] { coordinateSystem.BasisZ.X, coordinateSystem.BasisZ.Y, coordinateSystem.BasisZ.Z }), (vertexCount - 1)).ToList();
      
      try
      {
        var offset1 = myStick.GetOffset(AnalyticalElementSelector.StartOrBase);
        var offset2 = myStick.GetOffset(AnalyticalElementSelector.EndOrTop);

        // TODO: This should be linear interpolation?
        var fillerList = Enumerable.Repeat(new StructuralVectorThree(new double[] { 0, 0, 0 }), (vertexCount - 1) * 2 - 2).ToList();

        myElement.Offset = new List<StructuralVectorThree>() { new StructuralVectorThree(new double[] { offset1.X / Scale, offset1.Y / Scale, offset1.Z / Scale }) }
          .Concat(fillerList)
          .Concat(new List<StructuralVectorThree>() { new StructuralVectorThree(new double[] { offset2.X / Scale, offset2.Y / Scale, offset2.Z / Scale }) }).ToList();
      }
      catch
      {
        try
        {
          var offset = myStick.GetOffset(AnalyticalElementSelector.Whole);
          myElement.Offset = Enumerable.Repeat(new StructuralVectorThree(new double[] { offset.X / Scale, offset.Y / Scale, offset.Z / Scale }), (vertexCount - 1) * 2).ToList();
        }
        catch
        {

        }
      }

      if (myStick is AnalyticalModelColumn)
      {
        StructuralVectorBoolSix endRelease1 = null, endRelease2 = null;

        switch (myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_TYPE).AsInteger())
        {
          case 0:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false });
            break;
          case 1:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, true, true, true });
            break;
          case 2:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, true, true });
            break;
          case 3:
            endRelease1 = new StructuralVectorBoolSix(new bool[] {
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FZ).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_MZ).AsInteger() == 1,
            });
            break;
        }

        switch (myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_TYPE).AsInteger())
        {
          case 0:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false });
            break;
          case 1:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, true, true, true });
            break;
          case 2:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, true, true });
            break;
          case 3:
            endRelease2 = new StructuralVectorBoolSix(new bool[] {
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_FX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_FY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_FZ).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_MX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_MY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_MZ).AsInteger() == 1,
            });
            break;
        }

        var fillerList = Enumerable.Repeat(new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false }), (vertexCount - 1) * 2 - 2).ToList();

        myElement.EndRelease = new List<StructuralVectorBoolSix>() { endRelease1 }.Concat(fillerList).Concat(new List<StructuralVectorBoolSix>() { endRelease2 }).ToList() ;
      }
      else
      {
        StructuralVectorBoolSix endRelease1 = null, endRelease2 = null;

        switch (myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_TYPE).AsInteger())
        {
          case 0:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false });
            break;
          case 1:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, true, true, true });
            break;
          case 2:
            endRelease1 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, true, true });
            break;
          case 3:
            endRelease1 = new StructuralVectorBoolSix(new bool[] {
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_FX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_FY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_FZ).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_MX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_MY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_MZ).AsInteger() == 1,
            });
            break;
        }

        switch (myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_TYPE).AsInteger())
        {
          case 0:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false });
            break;
          case 1:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, true, true, true });
            break;
          case 2:
            endRelease2 = new StructuralVectorBoolSix(new bool[] { false, false, false, false, true, true });
            break;
          case 3:
            endRelease2 = new StructuralVectorBoolSix(new bool[] {
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_FX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_FY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_FZ).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_MX).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_MY).AsInteger() == 1,
              myStick.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_MZ).AsInteger() == 1,
            });
            break;
        }

        var fillerList = Enumerable.Repeat(new StructuralVectorBoolSix(new bool[] { false, false, false, false, false, false }), (vertexCount - 1) * 2 - 2).ToList();

        myElement.EndRelease = new List<StructuralVectorBoolSix>() { endRelease1 }.Concat(fillerList).Concat(new List<StructuralVectorBoolSix>() { endRelease2 }).ToList();
      }

      // Property
      try
      {
        var mySection = new Structural1DProperty
        {
          Name = Doc.GetElement(myStick.GetElementId()).Name,
          ApplicationId = myFamily.Symbol.UniqueId
        };

        switch (myFamily.Symbol.GetStructuralSection().StructuralSectionGeneralShape)
        {
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralI:
            mySection.Shape = Structural1DPropertyShape.I;
            mySection.Hollow = false;
            break;
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralT:
            mySection.Shape = Structural1DPropertyShape.T;
            mySection.Hollow = false;
            break;
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralH:
            mySection.Shape = Structural1DPropertyShape.Rectangular;
            mySection.Hollow = true;
            mySection.Thickness = (double)typeof(Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralH).GetProperty("WallNominalThickness").GetValue(myFamily.Symbol.GetStructuralSection()) / Scale;
            break;
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralR:
            mySection.Shape = Structural1DPropertyShape.Circular;
            mySection.Hollow = true;
            mySection.Thickness = (double)typeof(Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("WallNominalThickness").GetValue(myFamily.Symbol.GetStructuralSection()) / Scale;
            mySection.Profile = new SpeckleCircle(
              new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
              (double)typeof(Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("Diameter").GetValue(myFamily.Symbol.GetStructuralSection()) / 2 / Scale);
            break;
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralF:
            mySection.Shape = Structural1DPropertyShape.Rectangular;
            mySection.Hollow = false;
            break;
          case Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralShape.GeneralS:
            mySection.Shape = Structural1DPropertyShape.Circular;
            mySection.Profile = new SpeckleCircle(
              new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
              (double)typeof(Autodesk.Revit.DB.Structure.StructuralSections.StructuralSectionGeneralR).GetProperty("Diameter").GetValue(myFamily.Symbol.GetStructuralSection()) / 2 / Scale);
            mySection.Hollow = false;
            break;
          default:
            mySection.Shape = Structural1DPropertyShape.Generic;
            mySection.Hollow = false;
            break;
        }

        // Generate section profile
        var profile = myFamily.GetSweptProfile().GetSweptProfile();

        if (mySection.Shape != Structural1DPropertyShape.Circular)
        {
          var myProfile = new SpecklePolyline
          {
            Value = new List<double>()
          };

          for (var i = 0; i < profile.Curves.Size; i++)
          {
            var sectionCurves = SpeckleCore.Converter.Serialise(profile.Curves.get_Item(i));

            var sectionCoordinates = new List<double>();
            var nextCoordinates = new List<double>();

            if (sectionCurves is SpeckleLine)
            {
              sectionCoordinates = (sectionCurves as SpeckleLine).Value.Select(x => Math.Round(x, 10)).ToList();

              if (myProfile.Value.Count == 0)
              {
                myProfile.Value = sectionCoordinates;
                continue;
              }

              if (myProfile.Value.Skip(myProfile.Value.Count - 3).SequenceEqual(sectionCoordinates.Take(3)))
                nextCoordinates = sectionCoordinates.Skip(3).ToList();
              else
                break;
            }
            else if (sectionCurves is SpeckleArc)
            {
              if (myProfile.Value.Count == 0)
              {
                myProfile.Value = (sectionCurves as SpeckleArc).StartPoint.Value.Select(x => Math.Round(x, 10))
                  .Concat((sectionCurves as SpeckleArc).MidPoint.Value.Select(x => Math.Round(x, 10)))
                  .Concat((sectionCurves as SpeckleArc).EndPoint.Value.Select(x => Math.Round(x, 10)))
                  .ToList();
                continue;
              }

              if (myProfile.Value.Skip(myProfile.Value.Count - 3).SequenceEqual((sectionCurves as SpeckleArc).StartPoint.Value.Select(x => Math.Round(x, 10))))
                nextCoordinates = (sectionCurves as SpeckleArc).EndPoint.Value.Select(x => Math.Round(x, 10)).ToList();
              else if (myProfile.Value.Skip(myProfile.Value.Count - 3).SequenceEqual((sectionCurves as SpeckleArc).EndPoint.Value.Select(x => Math.Round(x, 10))))
                nextCoordinates = (sectionCurves as SpeckleArc).StartPoint.Value.Select(x => Math.Round(x, 10)).ToList();
              else
                break;
            }

            if (nextCoordinates.SequenceEqual(myProfile.Value.Take(3)))
            {
              myProfile.Closed = true;
              break;
            }
            else
              myProfile.Value.AddRange(nextCoordinates);
          }

          myProfile.GenerateHash();

          mySection.Profile = myProfile;
        }

        // Material
        try
        {
          var matType = myFamily.StructuralMaterialType;
          
          var structMat = (Material)Doc.GetElement(myFamily.StructuralMaterialId);
          if (structMat == null)
            structMat = (Material)Doc.GetElement(myFamily.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId());
          var matAsset = ((PropertySetElement)Doc.GetElement(structMat.StructuralAssetId)).GetStructuralAsset();

          SpeckleObject myMaterial = null;

          switch (matType)
          {
            case Autodesk.Revit.DB.Structure.StructuralMaterialType.Concrete:
              var concMat = new StructuralMaterialConcrete
              {
                ApplicationId = Doc.GetElement(myFamily.StructuralMaterialId).UniqueId,
                Name = Doc.GetElement(myFamily.StructuralMaterialId).Name,
                YoungsModulus = matAsset.YoungModulus.X,
                ShearModulus = matAsset.ShearModulus.X,
                PoissonsRatio = matAsset.PoissonRatio.X,
                Density = matAsset.Density,
                CoeffThermalExpansion = matAsset.ThermalExpansionCoefficient.X,
                CompressiveStrength = matAsset.ConcreteCompression,
                MaxStrain = 0,
                AggragateSize = 0
              };
              myMaterial = concMat;
              break;
            case Autodesk.Revit.DB.Structure.StructuralMaterialType.Steel:
              var steelMat = new StructuralMaterialSteel
              {
                ApplicationId = Doc.GetElement(myFamily.StructuralMaterialId).UniqueId,
                Name = Doc.GetElement(myFamily.StructuralMaterialId).Name,
                YoungsModulus = matAsset.YoungModulus.X,
                ShearModulus = matAsset.ShearModulus.X,
                PoissonsRatio = matAsset.PoissonRatio.X,
                Density = matAsset.Density,
                CoeffThermalExpansion = matAsset.ThermalExpansionCoefficient.X,
                YieldStrength = matAsset.MinimumYieldStress,
                UltimateStrength = matAsset.MinimumTensileStrength,
                MaxStrain = 0
              };
              myMaterial = steelMat;
              break;
            default:
              var defMat = new StructuralMaterialSteel
              {
                ApplicationId = Doc.GetElement(myFamily.StructuralMaterialId).UniqueId,
                Name = Doc.GetElement(myFamily.StructuralMaterialId).Name
              };
              myMaterial = defMat;
              break;
          }

          myMaterial.GenerateHash();
          mySection.MaterialRef = (myMaterial as SpeckleObject).ApplicationId;

          returnObjects.Add(myMaterial);
        }
        catch { }

        mySection.GenerateHash();
        myElement.PropertyRef = mySection.ApplicationId;

        returnObjects.Add(mySection);
      }
      catch { }

      myElement.GenerateHash();
      myElement.ApplicationId = myStick.UniqueId;
      returnObjects.Add(myElement);

      return returnObjects;
    }
  }
}
