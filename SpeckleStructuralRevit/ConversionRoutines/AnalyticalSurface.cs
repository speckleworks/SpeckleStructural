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
    public static Element ToNative(this Structural2DElementMesh myMesh)
    {
      return null;
    }

    public static List<SpeckleObject> ToSpeckle(this AnalyticalModelSurface mySurface)
    {
      var returnObjects = new List<SpeckleObject>();

      if (!mySurface.IsEnabled())
        return new List<SpeckleObject>();

      // Get the family
      var myRevitElement = Doc.GetElement(mySurface.GetElementId());

      var type = Structural2DElementType.Generic;
      if (myRevitElement is Floor)
        type = Structural2DElementType.Slab;
      else if (myRevitElement is Wall)
        type = Structural2DElementType.Wall;

      // Voids first

      var voidLoops = mySurface.GetLoops(AnalyticalLoopType.Void);
      foreach (var loop in voidLoops)
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
        
        returnObjects.Add(new Structural2DVoid(coor.ToArray(), null));
      }

      var polylines = new List<double[]>();

      var loops = mySurface.GetLoops(AnalyticalLoopType.External);
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
      }

      var coordinateSystem = mySurface.GetLocalCoordinateSystem();
      var axis = coordinateSystem == null ? null : new StructuralAxis(
        new StructuralVectorThree(new double[] { coordinateSystem.BasisX.X, coordinateSystem.BasisX.Y, coordinateSystem.BasisX.Z }),
        new StructuralVectorThree(new double[] { coordinateSystem.BasisY.X, coordinateSystem.BasisY.Y, coordinateSystem.BasisY.Z }),
        new StructuralVectorThree(new double[] { coordinateSystem.BasisZ.X, coordinateSystem.BasisZ.Y, coordinateSystem.BasisZ.Z })
      );

      // Property
      string sectionID = null;
      try
      {
        var mySection = new Structural2DProperty();

        mySection.Name = Doc.GetElement(myRevitElement.GetTypeId()).Name;
        mySection.ApplicationId = Doc.GetElement(myRevitElement.GetTypeId()).UniqueId;

        if (myRevitElement is Floor)
        {
          var myFloor = myRevitElement as Floor;
          mySection.Thickness = myFloor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble() / Scale;
        }
        else if (myRevitElement is Wall)
        {
          var myWall = myRevitElement as Wall;
          mySection.Thickness = myWall.WallType.Width / Scale;
        }

        try
        {
          // Material
          Material myMat = null;
          StructuralAsset matAsset = null;

          if (myRevitElement is Floor)
          {
            var myFloor = myRevitElement as Floor;
            myMat = Doc.GetElement(myFloor.FloorType.StructuralMaterialId) as Material;
          }
          else if (myRevitElement is Wall)
          {
            var myWall = myRevitElement as Wall;
            myMat = Doc.GetElement(myWall.WallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId()) as Material;
          }

          SpeckleObject myMaterial = null;

          matAsset = ((PropertySetElement)Doc.GetElement(myMat.StructuralAssetId)).GetStructuralAsset();

          var matType = myMat.MaterialClass;

          switch (matType)
          {
            case "Concrete":
              var concMat = new StructuralMaterialConcrete();
              concMat.ApplicationId = myMat.UniqueId;
              concMat.Name = Doc.GetElement(myMat.StructuralAssetId).Name;
              concMat.YoungsModulus = matAsset.YoungModulus.X;
              concMat.ShearModulus = matAsset.ShearModulus.X;
              concMat.PoissonsRatio = matAsset.PoissonRatio.X;
              concMat.Density = matAsset.Density;
              concMat.CoeffThermalExpansion = matAsset.ThermalExpansionCoefficient.X;
              concMat.CompressiveStrength = matAsset.ConcreteCompression;
              concMat.MaxStrain = 0;
              concMat.AggragateSize = 0;
              myMaterial = concMat;
              break;
            case "Steel":
              var steelMat = new StructuralMaterialSteel();
              steelMat.ApplicationId = myMat.UniqueId;
              steelMat.Name = Doc.GetElement(myMat.StructuralAssetId).Name;
              steelMat.YoungsModulus = matAsset.YoungModulus.X;
              steelMat.ShearModulus = matAsset.ShearModulus.X;
              steelMat.PoissonsRatio = matAsset.PoissonRatio.X;
              steelMat.Density = matAsset.Density;
              steelMat.CoeffThermalExpansion = matAsset.ThermalExpansionCoefficient.X;
              steelMat.YieldStrength = matAsset.MinimumYieldStress;
              steelMat.UltimateStrength = matAsset.MinimumTensileStrength;
              steelMat.MaxStrain = 0;
              myMaterial = steelMat;
              break;
            default:
              var defMat = new StructuralMaterialSteel();
              defMat.ApplicationId = myMat.UniqueId;
              defMat.Name = Doc.GetElement(myMat.StructuralAssetId).Name;
              myMaterial = defMat;
              break;
          }

          myMaterial.GenerateHash();
          mySection.MaterialRef = (myMaterial as SpeckleObject).ApplicationId;

          returnObjects.Add(myMaterial);
        }
        catch { }

        mySection.GenerateHash();

        sectionID = mySection.ApplicationId;

        returnObjects.Add(mySection);
      }
      catch { }

      var counter = 0;
      foreach(var coor in polylines)
      {
        var dummyMesh = new Structural2DElementMesh(coor, null, type, null, null, null);
        
        var numFaces = 0;
        for (var i = 0; i < dummyMesh.Faces.Count(); i++)
        {
          numFaces++;
          i += dummyMesh.Faces[i] + 3;
        }

        var mesh = new Structural2DElementMesh();
        mesh.Vertices = dummyMesh.Vertices;
        mesh.Faces = dummyMesh.Faces;
        mesh.Colors = dummyMesh.Colors;
        mesh.ElementType = type;
        if (sectionID != null)
          mesh.PropertyRef = sectionID;
        if (axis != null)
          mesh.Axis = Enumerable.Repeat(axis, numFaces).ToList();
        mesh.Offset = Enumerable.Repeat(0.0, numFaces).Cast<double>().ToList(); //TODO

        mesh.GenerateHash();
        mesh.ApplicationId = mySurface.UniqueId + "_" + (counter++).ToString();

        returnObjects.Add(mesh);
      }

      return returnObjects;
    }
  }
}
