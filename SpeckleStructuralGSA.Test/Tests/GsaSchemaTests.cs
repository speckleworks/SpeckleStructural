using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using Moq;
using NUnit.Framework;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;
using SpeckleCoreGeometryClasses;
using DeepEqual.Syntax;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class GsaSchemaTests
  {
    //Used in multiple tests
    private static readonly GsaAxis gsaAxis1 = new GsaAxis() { Index = 1, ApplicationId = "Axis1", Name = "StandardAxis", XDirX = 1, XDirY = 0, XDirZ = 0, XYDirX = 0, XYDirY = 1, XYDirZ = 0, OriginX = 10, OriginY = 20, OriginZ = 30 };
    private static readonly GsaAxis gsaAxis2 = new GsaAxis() { Index = 2, ApplicationId = "Axis2", Name = "AngledAxis", XDirX = 1, XDirY = 1, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
    private static readonly string streamId1 = "TestStream1";

    [SetUp]
    public void SetUp()
    {
      Initialiser.AppResources = new MockGSAApp();
      Initialiser.GsaKit.Clear();
    }

    [Test]
    public void StructuralLoadCaseToNative()
    {
      var load1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Generic, ApplicationId = "lc1", Name = "LoadCaseOne" };
      var load2 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "lc2", Name = "LoadCaseTwo" };

      SchemaConversion.StructuralLoadCaseToNative.ToNative(load1);
      SchemaConversion.StructuralLoadCaseToNative.ToNative(load2);

      var gwa = Initialiser.AppResources.Cache.GetGwa(GsaRecord.GetKeyword<GsaLoadCase>());
      Assert.AreEqual(2, gwa.Count());
      Assert.False(gwa.Any(g => string.IsNullOrEmpty(g)));

      Assert.IsTrue(ModelValidation(gwa, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaLoadCase>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      var gsaLoadCase1 = new GsaLoadCase();
      var gsaLoadCase2 = new GsaLoadCase();
      Assert.IsTrue(gsaLoadCase1.FromGwa(gwa[0]));
      Assert.IsTrue(gsaLoadCase2.FromGwa(gwa[1]));
    }

    [Test]
    public void StructuralAssembly()
    {
      var assembly1 = new StructuralAssembly() { ApplicationId = "gh/d73615b388a8c37b6322a607a2ed5e60", Name = "C1W3S3_L5" };
      assembly1.BaseLine = new SpeckleLine(new List<double> { 41.6, 20.5, 27.25, 40.6, 20.5, 27.25 });
      assembly1.PointDistances = new List<double>() { 0.5500000000000043, 0.5500000000000043, 0.5500000000000043,
        0.5499999999999972, 0.5499999999999972, 0.5499999999999972,
        0.5, 0.5, 0.5,
        1.5007648109742667, 1.5007648109742667, 1.5007648109742667,
        0.7503825000000006, 0.7503825000000006, 0.7503825000000006,
        0.5, 0.5, 0.5};
      assembly1.OrientationPoint = new SpecklePoint(41.6, 20.5, 26.5);
      assembly1.Width = 1.5;
      assembly1.ElementRefs = new List<string>() { "A", "B", "C" };

      var assembly2 = new StructuralAssembly() { ApplicationId = "gh/7fc46bbaaa572ce40c6205d1602677f9", Name = "C1W1P1" };
      assembly2.BaseLine = new SpeckleLine(new List<double> { 40.333333499999998, 11.5, 0, 40.333333499999998, 11.5, 48 });
      assembly2.PointDistances = new List<double>() { 0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48 };
      assembly2.OrientationPoint = new SpecklePoint(39.6, 11.5, 48);
      assembly2.Width = 1.466666999999994;
      assembly2.ElementRefs = new List<string>() { "D", "E", "F" };

      StructuralAssemblyToNative.ToNative(assembly1);
      StructuralAssemblyToNative.ToNative(assembly2);

      var gwa = Initialiser.AppResources.Cache.GetGwa(GsaRecord.GetKeyword<GsaAssembly>());
      Assert.AreEqual(2, gwa.Count());
      Assert.False(gwa.Any(g => string.IsNullOrEmpty(g)));

      Assert.IsTrue(ModelValidation(gwa, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaAssembly>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      var gsaAssembly1 = new GsaAssembly();
      Assert.IsTrue(gsaAssembly1.FromGwa(gwa[0]));
    }

    //This just tests transitions from the GSA schema to GWA commands, and back again, since there is no need at the moment for a ToNative() method for StructuralAxis
    [Test]
    public void GsaAxisSimple()
    {
      Assert.IsTrue(gsaAxis1.Gwa(out var axis1gwa));
      Assert.IsTrue(gsaAxis2.Gwa(out var axis2gwa));

      Assert.IsTrue(ModelValidation(new string[] { axis1gwa.First(), axis2gwa.First() }, new Dictionary<string, int> { { GsaRecord.GetKeyword<GsaAxis>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());
      
      Assert.IsTrue(gsaAxis1.FromGwa(axis1gwa.First()));
      Assert.IsTrue(gsaAxis2.FromGwa(axis2gwa.First()));
    }

    [TestCase(0)]
    [TestCase(30)]
    [TestCase(180)]
    public void GsaGridSurfaceAngles(double angleDegrees)
    {
      var gsaGridSurface1 = new GsaGridSurface()
      {
        Name = "Surface1",
        ApplicationId = "lgs1",
        Index = 1,
        PlaneRefType = GridPlaneAxisRefType.Global,
        Type = GridSurfaceElementsType.OneD,
        //leave entities blank, which will be treated as "all"
        Tolerance = 0.01,
        Span = GridSurfaceSpan.One,
        Angle = angleDegrees,
        Expansion = GridExpansion.PlaneCorner
      };

      Assert.IsTrue(gsaGridSurface1.Gwa(out var gwa, false));
      Assert.IsNotNull(gwa);
      Assert.Greater(gwa.Count(), 0);
      Assert.IsFalse(string.IsNullOrEmpty(gwa.First()));
      Assert.IsTrue(ModelValidation(gwa.First(), GsaRecord.GetKeyword<GsaGridSurface>(), 1, out var mismatch, visible: false));
      Assert.AreEqual(0, mismatch);
    }

    [Test]
    public void GsaLoadNodeSimple()
    {
      var gsaObjRx = new GsaLoadNode()
      {
        ApplicationId = "AppId",
        Name = "Zero Dee Lode",
        Index = 1,
        NodeIndices = new List<int>() { 3, 4 },
        LoadCaseIndex = 3,
        GlobalAxis = true,
        LoadDirection = AxisDirection6.XX,
        Value = 23
      };

      Assert.IsTrue(gsaObjRx.Gwa(out var gwa, true));
      Assert.IsNotEmpty(gwa);
      Assert.IsTrue(ModelValidation(gwa, GsaRecord.GetKeyword<GsaLoadNode>(), 1, out var _));
    }


    //Note for understanding:
    //StructuralStorey <-> GRID_PLANE
    //StructuralLoadPlane <-> GRID_SURFACE, which references GRID_PLANEs
    [Test]
    public void GsaLoadPanelHierarchyToNative()
    {
      var gsaElevatedAxis = new GsaAxis() { Index = 1, ApplicationId = "Axis1", Name = "StandardAxis", XDirX = 1, XDirY = 0, XDirZ = 0, XYDirX = 0, XYDirY = 1, XYDirZ = 0, OriginX = 10, OriginY = 20, OriginZ = 30 };
      var gsaRotatedAxis = new GsaAxis() { Index = 2, ApplicationId = "Axis2", Name = "AngledAxis", XDirX = 1, XDirY = 1, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };

      var storey1 = new StructuralStorey()
      {
        ApplicationId = "TestStorey",
        Name = "Test Storey",
        Axis = (StructuralAxis)gsaRotatedAxis.ToSpeckle(),
        Elevation = 10,
        ToleranceAbove = 5,
        ToleranceBelow = 6
      };
      StructuralStoreyToNative.ToNative(storey1).Split('\n');

      //Without storey reference, but with an axis (must have one or the other) - should be written as GLOBAL
      //There is no way currently to specify X elevation, Y elevation etc
      var plane1 = new StructuralLoadPlane()
      {
        ApplicationId = "lp1",
        Axis = (StructuralAxis)gsaElevatedAxis.ToSpeckle(),
        ElementDimension = 2,
        Tolerance = 0.1,
        Span = 2,
        SpanAngle = 30,
      };
      StructuralLoadPlaneToNative.ToNative(plane1).Split('\n');

      var plane2 = new StructuralLoadPlane()
      {
        ApplicationId = "lp2",
        ElementDimension = 1,
        Tolerance = 0.1,
        Span = 1,
        SpanAngle = 0,
        StoreyRef = "TestStorey"
      };
      StructuralLoadPlaneToNative.ToNative(plane2).Split('\n');

      var loadCase1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "LcDead", Name = "Dead Load Case" };
      SchemaConversion.StructuralLoadCaseToNative.ToNative(loadCase1);

      var polylineCoords = CreateFlatRectangleCoords(0, 0, 0, 30, 5, 5);
      var loading = new StructuralVectorThree(new double[] { 0, -10, -5 });
      var load2dPanelWithoutPlane = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel1");
      SchemaConversion.Structural2DLoadPanelToNative.ToNative(load2dPanelWithoutPlane);

      var load2dPanelWithPlane1 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel2") { LoadPlaneRef = "lp1" };
      SchemaConversion.Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane1);

      var load2dPanelWithPlane2 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel3") { LoadPlaneRef = "lp2" };
      SchemaConversion.Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane2);

      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetCurrentGwa();

      //Try all the entities' GWA commands to check if the 
      Assert.IsTrue(ModelValidation(allGwa,
        new Dictionary<string, int> {
          { GsaRecord.GetKeyword<GsaAxis>(), 3 },
          { GsaRecord.GetKeyword<GsaLoadCase>(), 1 },
          { GsaRecord.GetKeyword<GsaGridPlane>(), 3 } ,
          { GsaRecord.GetKeyword<GsaGridSurface>(), 3 },
          { GsaRecord.GetKeyword<GsaLoadGridArea>(), 6 }
        },
        out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());
      Assert.Zero(((MockGSAMessenger)Initialiser.AppResources.Messenger).Messages.Count());
    }
    
    [Test]
    public void Structural2DLoadPanelToNative()
    {
      var loadCaseAppId = "LoadCase1";
      var loadPanelAppId = "LoadPanel1";
      var loadCase = new StructuralLoadCase
      {
        ApplicationId = loadCaseAppId,
        CaseType = StructuralLoadCaseType.Dead
      };
      SchemaConversion.StructuralLoadCaseToNative.ToNative(loadCase);

      var loadPanel = new Structural2DLoadPanel
      {
        ApplicationId = loadPanelAppId,
        basePolyline = new SpecklePolyline(CreateFlatRectangleCoords(10, 10, 10, angleDegrees: 45, 20, 30)),
        Loading = new StructuralVectorThree(new double[] { 0, 0, 10000000 }),
        LoadCaseRef = "LoadCase1"
      };
      SchemaConversion.Structural2DLoadPanelToNative.ToNative(loadPanel).Split('\n');

      var LoadPanelGwa = ((IGSACache)Initialiser.AppResources.Cache).GetCurrentGwa();
      Assert.AreEqual(5, LoadPanelGwa.Count()); //should be a load case, axis, plane, surface and a load panel

      var gsaLoadCase = new GsaLoadCase() { ApplicationId = loadCaseAppId, CaseType = StructuralLoadCaseType.Dead, Index = 1 };
      var gsaAxis = new GsaAxis() { Index = 1, OriginX = 10, OriginY = 10, OriginZ = 10, XDirX = Math.Sqrt(2), XDirY = Math.Sqrt(2), XYDirX = -Math.Sqrt(2), XYDirY = Math.Sqrt(2) };
      var gsa2dLoadPanel = new GsaLoadGridArea();

      Assert.IsTrue(gsaAxis.Gwa(out var gsaAxisGwa));
      Assert.IsTrue(gsaLoadCase.Gwa(out var gsaLoadCaseGwa));
      Assert.IsTrue(gsa2dLoadPanel.Gwa(out var gsa2dLoadPanelGwa));
    }

    [Test]
    public void Structural1DPropertyExplicit()
    {
      var steel = new StructuralMaterialSteel(200000, 76923.0769, 0.3, 7850, 0.000012, 300, 440, 0.05, "steel");
      var concrete = new StructuralMaterialConcrete(29910.2016, 12462.584, 0.2, 2400, 0.00001, 12.8, 0.003, 0.02, "conc");

      var gwaSteel = steel.ToNative();
      var gwaConcrete = concrete.ToNative();
      var gwaPropNonExp = "SET\tSECTION.7:{speckle_app_id:columnProp}\t1\tNO_RGB\tColumn property\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tCONCRETE\t1\tGEO P(m) M(-0.836|-1.141) L(-3.799|3.396) L(1.71|0.992) L(0.931|-0.92) M(-1.881|1.512) L(-0.247|1.195) L(-0.431|-0.418) M(1.313|0.588) L(1.098|0.683) L(1.115|0.273) L(1.24|0.259)\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t89.99999998\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t1\t0\t0\t0\t1\t0\tNO\tNO\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON";
      Helper.GwaToCache(gwaSteel, streamId1);
      Helper.GwaToCache(gwaConcrete, streamId1);
      Helper.GwaToCache(gwaPropNonExp, streamId1);

      var propExp1 = new Structural1DPropertyExplicit() { ApplicationId = "propexp1", Name = "PropExp1", MaterialRef = "steel", Area = 11, Iyy = 21, Izz = 31, J = 41, Ky = 51, Kz = 61 };
      var propExp2 = new Structural1DPropertyExplicit() { ApplicationId = "propexp2", Name = "PropExp2", MaterialRef = "conc", Area = 12, Iyy = 22, Izz = 32, J = 42, Ky = 52, Kz = 62 };
      var propExp3 = new Structural1DPropertyExplicit() { ApplicationId = "propexp3", Name = "PropExp3", Area = 13, Iyy = 23, Izz = 33, J = 43, Ky = 53, Kz = 63 };

      Structural1DPropertyExplicitToNative.ToNative(propExp1);
      Structural1DPropertyExplicitToNative.ToNative(propExp2);
      Structural1DPropertyExplicitToNative.ToNative(propExp3);

      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetNewGwaSetCommands();
      var expectedCountByKw = new Dictionary<string, int>()
      {
        { "MAT_STEEL", 1},
        { "MAT_CONCRETE", 1 },
        { "SECTION", 4 }
      };
      Assert.IsTrue(ModelValidation(allGwa, expectedCountByKw, out var mismatchByKw, false));
      Assert.AreEqual(0, mismatchByKw.Keys.Count());

      //Check all the FromGwa commands - this includes the non-EXP one since the keyword for GSA1DPropertyExplicit is the same as for other 1D properties
      var gsaSections = SchemaConversion.Helper.GetNewFromCache<GSA1DPropertyExplicit, GsaSection>();
      Assert.AreEqual(4, gsaSections.Count());

      (new GSAMaterialConcrete()).ToSpeckle();
      (new GSAMaterialSteel()).ToSpeckle();

      var dummy = new GsaSection();
      GsaSectionToSpeckle.ToSpeckle(dummy);

      var newPropExps = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DPropertyExplicit>();

      newPropExps[0].SpeckleObject.ShouldDeepEqual(propExp1);
      newPropExps[1].SpeckleObject.ShouldDeepEqual(propExp2);
      newPropExps[2].SpeckleObject.ShouldDeepEqual(propExp3);
    }

    //Both ToNative and ToSpeckle
    [Test]
    public void Structural0DLoad()
    {
      //PREREQUISITES/REFERENCES - CONVERT TO GSA

      var node1 = new StructuralNode() { ApplicationId = "Node1", Name = "Node One", basePoint = new SpecklePoint(1, 2, 3) };
      var node2 = new StructuralNode() { ApplicationId = "Node2", Name = "Node Two", basePoint = new SpecklePoint(4, 5, 6) };
      var loadcase = new StructuralLoadCase() { ApplicationId = "LoadCase1", Name = "Load Case One", CaseType = StructuralLoadCaseType.Dead };
      Helper.GwaToCache(Conversions.ToNative(node1), streamId1);
      Helper.GwaToCache(Conversions.ToNative(node2), streamId1);
      SchemaConversion.StructuralLoadCaseToNative.ToNative(loadcase);

      //OBJECT UNDER TEST - CONVERT TO GSA

      var loading = new double[] { 10, 20, 30, 40, 50, 60 };
      var receivedObj = new Structural0DLoad()
      {
        ApplicationId = "Test0DLoad",
        Loading = new StructuralVectorSix(loading),
        NodeRefs = new List<string> { "Node1", "Node2" },
        LoadCaseRef = "LoadCase1"
      };
      Structural0DLoadToNative.ToNative(receivedObj);

      ((IGSACache)Initialiser.AppResources.Cache).Snapshot(streamId1);

      //PREREQUISITES/REFERENCES - CONVERT TO SPECKLE

      Conversions.ToSpeckle(new GSANode());
      Conversions.ToSpeckle(new GSALoadCase());

      //OBJECT UNDER TEST - CONVERT TO SPECKLE

      Conversions.ToSpeckle(new GSA0DLoad());

      var sentObjectsDict = Initialiser.GsaKit.GSASenderObjects.GetAll();
      Assert.IsTrue(sentObjectsDict.ContainsKey(typeof(GSA0DLoad)));

      var gsaLoadNodes = sentObjectsDict[typeof(GSA0DLoad)];
      var sentObjs = gsaLoadNodes.Select(o => ((IGSAContainer<Structural0DLoad>)o).Value).Cast<Structural0DLoad>().ToList();
      Assert.AreEqual(1, sentObjs.Count());
      Assert.IsTrue(sentObjs.First().Loading.Value.SequenceEqual(loading));
    }

    [TestCase(GSATargetLayer.Design)]
    [TestCase(GSATargetLayer.Analysis)]
    public void Structural1DLoad(GSATargetLayer layer)
    {
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = layer;

      var loadCase = new StructuralLoadCase()
      {
        ApplicationId = "gh/16c5d83d5f6226cc18c0a6489689fc90",
        CaseType = StructuralLoadCaseType.Live,
        Name = "Live Loads"
      };
      SchemaConversion.StructuralLoadCaseToNative.ToNative(loadCase);

      var materialSteel = new StructuralMaterialSteel()
      {
        ApplicationId = "gh/3eef066b812432a598be446180b74195"
      };
      Helper.GwaToCache(Conversions.ToNative(materialSteel), streamId1);

      var prop1d = new Structural1DProperty()
      {
        ApplicationId = "gh/d55f6475ea931e3ebfe0d81065486370",
        Profile = new SpecklePolyline(new double[] { 0, 0, 0, 500, 0, 0, 500, 500, 0, 0, 500, 0 }),
        Shape = Structural1DPropertyShape.Rectangular,
        MaterialRef = "gh/3eef066b812432a598be446180b74195",
      };
      Helper.GwaToCache(Conversions.ToNative(prop1d), streamId1);

      var elements = new List<Structural1DElement>
      {
        new Structural1DElement()
        {
          ApplicationId = "gh/b4db9f1651ca1189a64582098de85d37",
          Value = new List<double>() { 18742.85535595166, -98509.31912320339, 0, 31898.525787319068, -52834.7904308187, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        },
        new Structural1DElement()
        {
          ApplicationId = "gh/3c73c2754d2b24f42ec0bdea51133372",
          Value = new List<double>() { -5900.5943603799719, -38540.61268631692, 0, 18177.8512833416, 39670.54271647791, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        },
        new Structural1DElement()
        {
          ApplicationId = "gh/169aa416831dd4c282ec7050156037c4",
          Value = new List<double>() { -30544.044076711598, 21428.093750569555, 0, 4457.17677936413, 132175.8758637745, 0 },
          PropertyRef = "gh/d55f6475ea931e3ebfe0d81065486370",
          ElementType = Structural1DElementType.Beam
        }
      };
      foreach (var e in elements)
      {
        Helper.GwaToCache(Conversions.ToNative(e), streamId1);
      }

      //Based on YEKd4q0p9 on Canada server - not sure why the loading has an application ID!
      var loading = new StructuralVectorSix(new double[] { 0, 0, -8, 0, 0, 0 }, "gh/7c2df985ad21853a345f7a85edd3b47f");
      var load = new Structural1DLoad()
      {
        ApplicationId = "gh/44d23deb343b84e0a7fc95ce37604314",
        Loading = loading,
        ElementRefs = new List<string>
        {
          "gh/b4db9f1651ca1189a64582098de85d37",
          "gh/3c73c2754d2b24f42ec0bdea51133372",
          "gh/169aa416831dd4c282ec7050156037c4"
        },
        LoadCaseRef = "gh/16c5d83d5f6226cc18c0a6489689fc90"
      };

      Structural1DLoadToNative.ToNative(load);
      var allGwa = ((IGSACache)Initialiser.AppResources.Cache).GetNewGwaSetCommands();

      ((IGSACache)Initialiser.AppResources.Cache).Snapshot(streamId1);

      var entityKeyword = (layer == GSATargetLayer.Design) ? GsaRecord.GetKeyword<GsaMemb>() : GsaRecord.GetKeyword<GsaEl>();
      var loadBeamKeyword = GsaRecord.GetKeyword<GsaLoadBeam>();
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(entityKeyword, out var gwaEntities, out var _, out var _));
      Assert.AreEqual(3, gwaEntities.Count());
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(typeof(GSA1DProperty).GetGSAKeyword(), out var gwa1dProp, out var _, out var _));
      Assert.AreEqual(1, gwa1dProp.Count());
      Assert.IsTrue(Initialiser.AppResources.Cache.GetKeywordRecordsSummary(loadBeamKeyword, out var gwaLoadBeam, out var _, out var _));
      Assert.AreEqual(1, gwaLoadBeam.Count());

      var expectedCountByKw = new Dictionary<string, int>()
      {
        { loadBeamKeyword, 1}, 
        { "SECTION", 1 }, //PROP_SEC is written but SECTION is returned by GSA
        { entityKeyword, 3 } 
      };
      Assert.IsTrue(ModelValidation(allGwa, expectedCountByKw, out var mismatchByKw, false));
      Assert.AreEqual(0, mismatchByKw.Keys.Count());
    }

    [Test]
    public void GsaLoadNodeToSpeckle()
    {
      var baseAppId1 = "LoadFromSpeckle1";
      var baseAppId2 = "LoadFromSpeckle2";
      var loadCase1 = new GsaLoadCase() { Index = 1, CaseType = StructuralLoadCaseType.Dead };
      var loadCase2 = new GsaLoadCase() { Index = 2, CaseType = StructuralLoadCaseType.Live };
      var nodes = GenerateGsaNodes();
      var axis1 = new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var axis2 = new GsaAxis() { Index = 2, OriginX = 20, OriginY = -20, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var load1 = new GsaLoadNode() { Index = 1, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 1, AxisIndex = 1, LoadDirection = AxisDirection6.X, Value = 10 };
      var load2 = new GsaLoadNode() { Index = 2, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 1, AxisIndex = 2, LoadDirection = AxisDirection6.X, Value = 10 };
      var load3 = new GsaLoadNode() { Index = 3, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, AxisIndex = 1, LoadDirection = AxisDirection6.X, Value = 10 };
      var load4 = new GsaLoadNode() { Index = 4, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 2, AxisIndex = 2, LoadDirection = AxisDirection6.X, Value = 10 };
      var load5 = new GsaLoadNode() { Index = 5, ApplicationId = (baseAppId1 + "_XX"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = AxisDirection6.XX, Value = 12 };
      var load6 = new GsaLoadNode() { Index = 6, ApplicationId = (baseAppId1 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = AxisDirection6.YY, Value = 13 };
      var load7 = new GsaLoadNode() { Index = 7, ApplicationId = (baseAppId2 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = AxisDirection6.YY, Value = 14 };
      var load8 = new GsaLoadNode() { Index = 8, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = AxisDirection6.Z, Value = -10 };  //Test global without application ID

      var gsaRecords = new List<GsaRecord> { loadCase1, loadCase2 };
      gsaRecords.AddRange(nodes);
      gsaRecords.AddRange(new GsaRecord[] { axis1, axis2, load1, load2, load3, load4, load5, load6, load7, load8 });
      Assert.IsTrue(ExtractAndValidateGwa(gsaRecords, out var gwaCommands, out var mismatchByKw));

      Assert.IsTrue(UpsertGwaIntoCache(gwaCommands));

      //Ensure the prerequisite objects are in the send objects collection
      //Note: don't need Axis here as the GWA from the cache is used instead of GSA__ objects
      Conversions.ToSpeckle(new GSANode());

      var dummy = new GsaLoadNode();
      SchemaConversion.GsaLoadNodeToSpeckle.ToSpeckle(dummy);

      var sos = Initialiser.GsaKit.GSASenderObjects.Get<GSA0DLoad>().Select(g => g.Value).Cast<Structural0DLoad>().ToList();

      Assert.AreEqual(5, sos.Count());
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals(baseAppId1, StringComparison.InvariantCultureIgnoreCase) && o.Loading.Value.SequenceEqual(new double[] { 0, 0, 0, 12, 13, 0 })));
      Assert.AreEqual(0, sos.Count(o => string.IsNullOrEmpty(o.ApplicationId)));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals(baseAppId2)));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-1-2")));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-3-4")));
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals("gsa/LOAD_NODE-8")));
      //TO DO: check if this expected output is actually correct since it was based on the way StructuralVectorSix.TransformOntoAxis works
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.Loading.Value.SequenceEqual(new double[] { 10, 20, 0, -50, 50, 30 })));
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.LoadCaseRef.Equals("gsa/LOAD_TITLE-1")));
      Assert.AreEqual(2, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE-1", "gsa/NODE-2" }) && o.LoadCaseRef.Equals("gsa/LOAD_TITLE-2")));
    }

    [Test]
    public void GsaMemb1dSimple()
    {
      //An asterisk next to a row signifies non-obvious values I've specifically changed between all 3 (obvious values are application ID, index and name)

      var gsaMembBeamAuto = new GsaMemb()
      {
        ApplicationId = "beamauto",
        Name = "Beam Auto",
        Index = 1,
        Type = MemberType.Beam, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 3,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BEAM, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.X, ReleaseCode.Released }, { AxisDirection6.XX, ReleaseCode.Released } }, //*
        Releases2 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }}, //*
        RestraintEnd1 = Restraint.Fixed, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Automatic, //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.TopFlange, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembBeamAuto.Gwa(out var gwa1, false));

      var gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa1.First()));
      gsaMembBeamAuto.ShouldDeepEqual(gsaMemb);

      var gsaMembColEffLen = new GsaMemb() 
      { 
        ApplicationId = "efflencol",
        Name = "Eff Len Col",
        Index = 2,
        Type = MemberType.Column, //*
        Exposure = ExposedSurfaces.ONE, //*
        PropertyIndex = 3,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.BAR, //*
        Fire = FireResistance.FourHours,//*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.EffectiveLength, //*
        EffectiveLengthYY = 18, //*
        PercentageZZ = 65, //*
        EffectiveLengthLateralTorsional = 19, //*
        LoadHeight = 19, //*
        LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembColEffLen.Gwa(out var gwa2, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa2.First()));
      gsaMembColEffLen.ShouldDeepEqual(gsaMemb);

      var gsaMembGeneric1dExplicit = new GsaMemb()
      {
        ApplicationId = "explicitcol",
        Name = "Explicit Generic 1D",
        Index = 3,
        Type = MemberType.Generic1d, //*
        Exposure = ExposedSurfaces.NONE, //*
        PropertyIndex = 3,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5 },
        OrientationNodeIndex = 6,
        Angle = 10,
        MeshSize = 11,
        IsIntersector = true,
        AnalysisType = AnalysisType.DAMPER, //*
        Fire = FireResistance.FourHours, //*
        LimitingTemperature = 12,
        CreationFromStartDays = 13,
        RemovedAtDays = 16,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 17 }, //*
        RestraintEnd1 = Restraint.FullRotational, //*
        RestraintEnd2 = Restraint.Pinned, //*
        EffectiveLengthType = EffectiveLengthType.Explicit, //*
        PointRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { All = true, Restraint = Restraint.TopFlangeLateral }
        },  //*
        SpanRestraints = new List<RestraintDefinition>()
        {
          new RestraintDefinition() { Index = 1, Restraint = Restraint.Fixed },
          new RestraintDefinition() { Index = 3, Restraint = Restraint.PartialRotational }
        },  //*
        LoadHeight = 19,
        LoadHeightReferencePoint = LoadHeightReferencePoint.BottomFlange, //*
        MemberHasOffsets = false
      };
      Assert.IsTrue(gsaMembGeneric1dExplicit.Gwa(out var gwa3, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa3.First()));
      gsaMembGeneric1dExplicit.ShouldDeepEqual(gsaMemb);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Test]
    public void GsaMemb2dSimple()
    {
      var gsaMembSlabLinear = new GsaMemb()
      {
        ApplicationId = "slablinear",
        Name = "Slab Linear",
        Index = 1,
        Type = MemberType.Slab, //*
        Exposure = ExposedSurfaces.ALL, //*
        PropertyIndex = 2,
        Group = 1,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.LINEAR, //*
        Fire = FireResistance.HalfHour, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembSlabLinear.Gwa(out var gwa1, false));

      var gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa1.First()));
      gsaMembSlabLinear.ShouldDeepEqual(gsaMemb);

      var gsaMembWallQuadratic = new GsaMemb()
      {
        ApplicationId = "wallquadratic",
        Name = "Wall Quadratic",
        Index = 2,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 2,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.QUADRATIC, //*
        Fire = FireResistance.ThreeHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembWallQuadratic.Gwa(out var gwa2, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa2.First()));
      gsaMembWallQuadratic.ShouldDeepEqual(gsaMemb);

      var gsaMembGeneric = new GsaMemb()
      {
        ApplicationId = "generic2dRigid",
        Name = "Wall XY Rigid Diaphragm",
        Index = 3,
        Type = MemberType.Wall, //*
        Exposure = ExposedSurfaces.SIDES, //*
        PropertyIndex = 2,
        Group = 3,
        NodeIndices = new List<int>() { 4, 5, 6, 7 },
        Voids = new List<List<int>>() { new List<int>() { 8, 9, 10 } },
        OrientationNodeIndex = 3,
        Angle = 11,
        MeshSize = 12,
        IsIntersector = true,
        AnalysisType = AnalysisType.RIGID, //*
        Fire = FireResistance.TwoHours, //*
        LimitingTemperature = 13,
        CreationFromStartDays = 14,
        RemovedAtDays = 15,
        Offset2dZ = 16,
        OffsetAutomaticInternal = false
      };
      Assert.IsTrue(gsaMembGeneric.Gwa(out var gwa3, false));

      gsaMemb = new GsaMemb();
      Assert.IsTrue(gsaMemb.FromGwa(gwa3.First()));
      gsaMembGeneric.ShouldDeepEqual(gsaMemb);

      var gwaToTest = gwa1.Union(gwa2).Union(gwa3).ToList();

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaMemb>(), 3, out var mismatch));
    }

    [Test]
    public void GsaElSimple()
    {
      var gsaEls = GenerateMixedGsaEls();
      var gwaToTest = new List<string>();
      foreach (var gsaEl in gsaEls)
      {
        Assert.IsTrue(gsaEl.Gwa(out var gwa, false));

        var gsaElNew = new GsaEl();
        Assert.IsTrue(gsaElNew.FromGwa(gwa.First()));
        gsaEl.ShouldDeepEqual(gsaElNew);

        gwaToTest = gwaToTest.Union(gwa).ToList();
      }

      Assert.IsTrue(ModelValidation(gwaToTest, GsaRecord.GetKeyword<GsaEl>(), 2, out var mismatch));
    }

    [TestCase(GSATargetLayer.Design)]
    [TestCase(GSATargetLayer.Analysis)]
    public void GsaLoadBeamToSpeckle(GSATargetLayer layer)
    {
      //Currently only UDL is supported, so only test that for now, despte the new schema containing classes for the other types

      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = layer;

      var gsaPrereqs = new List<GsaRecord>()
      {
        new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 },
        new GsaLoadCase() { Index = 1, ApplicationId = "LoadCase1", CaseType = StructuralLoadCaseType.Dead },
        new GsaLoadCase() { Index = 2, ApplicationId = "LoadCase2",  CaseType = StructuralLoadCaseType.Live },
      };

      if (layer == GSATargetLayer.Design)
      {
        gsaPrereqs.Add(CreateMembBeam(1, "mb1", "Beam One", 1, new List<int> { 1, 2 }, 3));
        gsaPrereqs.Add(CreateMembBeam(2, "mb2", "Beam Two", 1, new List<int> { 4, 5 }, 6));
      }
      else
      {
        //gsaPrereqs.Add(CreateElBeam(1, "eb1", "Beam One", 1, new List<int> { 1, 2 }, 3));
        //gsaPrereqs.Add(CreateElBeam(2, "eb2", "Beam Two", 1, new List<int> { 4, 5 }, 6));
      }

      //Each one is assumed to create just one GWA record each
      foreach (var g in gsaPrereqs)
      {
        Assert.IsTrue(g.Gwa(out var gwa, false));
        Assert.IsTrue(Initialiser.AppResources.Cache.Upsert(g.Keyword, g.Index.Value, gwa.First(), g.StreamId, g.ApplicationId, g.GwaSetCommandType));
      }

      var baseAppId1 = "LoadFromSpeckle1";
      var baseAppId2 = "LoadFromSpeckle2";

      //Testing grouping rules:
      //1. GSA-sourced (no Speckle Application ID) beam load records with same loading, load case & entities
      //2. Speckle-sourced beam load records with same base application ID, load case & entities whose loads can be combined

      var gsaLoadBeams = new List<GsaLoadBeamUdl>
      {
        //For design layer, the entity list contains MEMB indices, which are written in terms of groups ("G1" etc); for analysis, it's the EL indices
        CreateLoadBeamUdl(1, "", "", new List<int>() { 1 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        CreateLoadBeamUdl(2, "", "", new List<int>() { 1 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        //This one shouldn't be grouped with the first 2 since it has a different load case
        CreateLoadBeamUdl(3, "", "", new List<int>() { 1 }, 2, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        //This one should be grouped with the first 2 either since it has the same loading (although different entities) and the same load case
        CreateLoadBeamUdl(4, "", "", new List<int>() { 1, 2 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),

        CreateLoadBeamUdl(5, baseAppId1 + "_X", "", new List<int>() { 1, 2 }, 1, AxisDirection6.X, -11, LoadBeamAxisRefType.Global),
        CreateLoadBeamUdl(6, baseAppId1 + "_XX", "", new List<int>() { 1, 2 }, 1, AxisDirection6.XX, 15, LoadBeamAxisRefType.Global),
        //This one shouldn't be grouped with the previous two since, due to the axis (which is a sign of manual editing after previous Speckle reception), 
        //the loads can't be combined
        CreateLoadBeamUdl(7, baseAppId1 + "_Z", "", new List<int>() { 1, 2 }, 1, AxisDirection6.Z, -5, LoadBeamAxisRefType.Reference, 1),
        //This one shouldn't be grouped with 5 and 6 either since, although the loads can be combined and the entities are the same, the load case is different
        CreateLoadBeamUdl(8, baseAppId1 + "_XX", "", new List<int>() { 1, 2 }, 2, AxisDirection6.XX, 15, LoadBeamAxisRefType.Global),

        //This one doesn't share the same application ID as the others, so just verify it isn't grouped with the previous records even though its loading matches one of them
        CreateLoadBeamUdl(9, baseAppId2 + "_Y", "", new List<int>() { 1 }, 1, AxisDirection6.Y, -11, LoadBeamAxisRefType.Global)
      };
      Assert.AreEqual(0, gsaLoadBeams.Where(lb => lb == null).Count());

      foreach (var gsalb in gsaLoadBeams)
      {
        Assert.IsTrue(gsalb.Gwa(out var lbGwa, false));
        Initialiser.AppResources.Cache.Upsert(gsalb.Keyword, gsalb.Index.Value, lbGwa.First(), gsalb.StreamId, gsalb.ApplicationId, gsalb.GwaSetCommandType);
      }

      //Still using dummy objects for the ToSpeckle commands - any GsaLoadBeam concrete class can be used here
      Assert.NotNull(SchemaConversion.GsaLoadBeamToSpeckle.ToSpeckle(new GsaLoadBeamUdl()));

      var structural1DLoads = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DLoad>().Select(o => o.Value).Cast<Structural1DLoad>().ToList();
      
      Assert.AreEqual(6, structural1DLoads.Count());
    }

    [Test]
    public void GsaSectionSimple()
    {
      var gwa1 = "SECTION.7\t3\tNO_RGB\tSTD GZ 10 3 3 1.5 1.6 1\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tSTD GZ 10 3 3 1.5 1.6 1\t0\t0\t0\tY_AXIS\t0\tNONE\t0\t0\t0\tNO_ENVIRON";
      var gwa2 = "SECTION.7\t2\tNO_RGB\t150x150x12EA-BtB\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t1\tSTEEL\t1\tSTD D 150 150 12 12\t0\t0\t0\tNONE\t0\tNONE\t0\tSECTION_STEEL.2\t0\t1\t1\t1\t0.4\tNO_LOCK\tROLLED\tUNDEF\t0\t0\tNO_ENVIRON";
      var gwa3 = "SECTION.7\t7\tNO_RGB\tfgds\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t2\tCONCRETE\t1\tSTD CH 99 60 8 9\t0\t0\t0\tNONE\t0\tSIMPLE\t0\tSECTION_CONC.6\t1\tNO_SLAB\t89.99999998\t0.025\t0\tSECTION_LINK.3\t0\t0\tDISCRETE\tRECT\t0\t\tSECTION_COVER.3\tUNIFORM\t0\t0\tNO_SMEAR\tSECTION_TMPL.4\tUNDEF\t0\t0\t0\t0\t0\t0\tNO_ENVIRON";
      var gwaExp = "SECTION.7\t2\tNO_RGB\tEXP 1 2 3 4 5 6\t1D_GENERIC\t0\tCENTROID\t0\t0\t0\t1\t0\t0\t0\t0\t1\tSECTION_COMP.4\t\t0\tGENERIC\t0\tEXP 1 2 3 4 5 6\t0\t0\t0\tNONE\t0\tNONE\t0\t0\t0\tNO_ENVIRON";

      var gsaSection1 = new GsaSection();
      gsaSection1.FromGwa(gwa1);
      gsaSection1.Gwa(out var gwaOut1);
      Assert.IsTrue(gwa1.Equals(gwaOut1.First(), StringComparison.InvariantCulture));

      var gsaSection2 = new GsaSection();
      gsaSection2.FromGwa(gwa2);
      gsaSection2.Gwa(out var gwaOut2);
      Assert.IsTrue(gwa2.Equals(gwaOut2.First(), StringComparison.InvariantCulture));

      var gsaSection3 = new GsaSection();
      gsaSection3.FromGwa(gwa3);
      gsaSection3.Gwa(out var gwaOut3);
      Assert.IsTrue(gwa3.Equals(gwaOut3.First(), StringComparison.InvariantCulture));

      var gsaSectionExp = new GsaSection();
      gsaSectionExp.FromGwa(gwaExp);
      gsaSectionExp.Gwa(out var gwaOutExp);
      Assert.IsTrue(gwaExp.Equals(gwaOutExp.First(), StringComparison.InvariantCulture));
    }

    [Test]
    public void GsaLoadGravitySimple()
    {
      ((MockSettings)Initialiser.AppResources.Settings).TargetLayer = GSATargetLayer.Analysis;

      var gsaEls = GenerateMixedGsaEls();
      foreach (var gsaEl in gsaEls)
      {
        Assert.IsTrue(gsaEl.Gwa(out var gwa, true));
        Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gsaNodes = GenerateGsaNodes();
      foreach (var gsaNode in gsaNodes)
      {
        Assert.IsTrue(gsaNode.Gwa(out var gwa, true));
        Helper.GwaToCache(gwa.First(), streamId1);
      }

      var gwa1 = "LOAD_GRAVITY.3\t+10% connections\tall\tall\t1\t0\t0\t-1.100000024";

      var gsaGrav1 = new GsaLoadGravity()
      {
        Index = 1,
        Name = "+10% connections",
        Entities = new List<int> { 1, 2 }, //all
        Nodes = new List<int> { 1, 2 }, //all
        LoadCaseIndex = 1,
        Z = -1.100000024
      };

      Assert.IsTrue(gsaGrav1.Gwa(out var gsaGravGwa, false));
      Assert.IsTrue(gwa1.Equals(gsaGravGwa.First()));

      Assert.IsTrue(ModelValidation(gsaGravGwa, GsaRecord.GetKeyword<GsaLoadGravity>(), 1, out var mismatch));
    }

    [Ignore("Conversion code not implemented yet")]
    [Test]
    public void GsaAnalStage()
    {
      //TO DO
      Assert.IsTrue(false);
    }

    [Ignore("Conversion code not implemented yet")]
    [Test]
    public void GsaAnal()
    {
      //TO DO
      Assert.IsTrue(false);
    }

    [Ignore("Conversion code not implemented yet")]
    [Test]
    public void GsaCombination()
    {
      //TO DO
      Assert.IsTrue(false);
    }

    [Ignore("Conversion code not implemented yet")]
    [Test]
    public void GsaLoad2dThermal()
    {
      //TO DO
      Assert.IsTrue(false);
    }

    #region data_gen_fns
    private List<GsaEl> GenerateMixedGsaEls()
    {
      var gsaElBeam = new GsaEl()
      {
        ApplicationId = "elbeam",
        Name = "Beam",
        Index = 1,
        Type = ElementType.Beam, //*
        Group = 1,
        PropertyIndex = 2,
        NodeIndices = new List<int> { 3, 4 },
        OrientationNodeIndex = 5,
        Angle = 6,
        ReleaseInclusion = ReleaseInclusion.Included,
        Releases1 = new Dictionary<AxisDirection6, ReleaseCode>() { { AxisDirection6.Y, ReleaseCode.Released }, { AxisDirection6.YY, ReleaseCode.Stiff } }, //*
        Stiffnesses1 = new List<double>() { 7 }, //*
        End1OffsetX = 8,
        End2OffsetX = 9,
        OffsetY = 10,
        OffsetZ = 11,
        ParentIndex = 1
      };

      var gsaElTri3 = new GsaEl()
      {
        ApplicationId = "eltri3",
        Name = "Triangle 3",
        Index = 2,
        Type = ElementType.Triangle3, //*
        Group = 1,
        PropertyIndex = 3,
        NodeIndices = new List<int> { 4, 5, 6 },
        OrientationNodeIndex = 7,
        Angle = 8,
        ReleaseInclusion = ReleaseInclusion.NotIncluded,  //only BEAMs have releases
        End1OffsetX = 10
      };

      return new List<GsaEl> { gsaElBeam, gsaElTri3 };
    }

    private List<GsaNode> GenerateGsaNodes()
    {
      var node1 = new GsaNode() { Index = 1, X = 10, Y = 10, Z = 0 };
      var node2 = new GsaNode() { Index = 2, X = 30, Y = -10, Z = 10 };
      return new List<GsaNode> { node1, node2 };
    }
    #endregion

    #region other_methods

    //Since the classes don't have constructors with parameters (by design, to avoid schema complexity for now), use this method instead
    private GsaLoadBeamUdl CreateLoadBeamUdl(int index, string applicationId, string name, List<int> entities, int loadCaseIndex, AxisDirection6 loadDirection,
      double load, LoadBeamAxisRefType axisRefType, int? axisIndex = null)
    {
      return new GsaLoadBeamUdl()
      {
        Index = index,
        ApplicationId = applicationId,
        Name = name,
        Entities = entities,
        LoadCaseIndex = loadCaseIndex,
        LoadDirection = loadDirection,
        Load = load,
        AxisRefType = axisRefType,
        AxisIndex = axisIndex
      };
    }

    private GsaMemb CreateMembBeam(int index, string applicationId, string name, int propIndex, List<int> nodeIndices, int orientationNodeIndex)
    {
      var gsaMemb = new GsaMemb()
      {
        ApplicationId = applicationId,
        Name = name,
        Index = index,
        Type = MemberType.Beam,
        Exposure = ExposedSurfaces.ALL,
        PropertyIndex = propIndex,
        Group = index,
        NodeIndices = nodeIndices,
        OrientationNodeIndex = orientationNodeIndex,
        IsIntersector = true,
        AnalysisType = AnalysisType.BEAM,
        RestraintEnd1 = Restraint.Fixed,
        RestraintEnd2 = Restraint.Fixed,
        EffectiveLengthType = EffectiveLengthType.Automatic,
        LoadHeightReferencePoint = LoadHeightReferencePoint.ShearCentre,
        MemberHasOffsets = false
      };
      return gsaMemb;
    }

    private double[] CreateFlatRectangleCoords(double x, double y, double z, double angleDegrees, double width, double depth)
    {
      var xUnitDir = UnitVector3D.XAxis.Rotate(UnitVector3D.ZAxis, Angle.FromDegrees(angleDegrees));
      var yUnitDir = xUnitDir.Rotate(UnitVector3D.ZAxis, Angle.FromDegrees(90));

      var p1 = new Point3D(x, y, z);
      var p2 = p1 + xUnitDir.ToVector3D().ScaleBy(width);
      var p3 = p2 + yUnitDir.ToVector3D().ScaleBy(depth);
      var p4 = p1 + yUnitDir.ToVector3D().ScaleBy(depth);

      var coords = new List<double>() { p1.X, p1.Y, p1.Z, p2.X, p2.Y, p2.Z, p3.X, p3.Y, p3.Z, p4.X, p4.Y, p4.Z };
      return coords.ToArray();
    }

    private bool UpsertGwaIntoCache(List<string> gwaCommands)
    {
      foreach (var gwaC in gwaCommands)
      {
        Initialiser.AppResources.Proxy.ParseGeneralGwa(gwaC, out var keyword, out var index, out var streamId, out var applicationId, out var gwaWithoutSet, out var gwaSetCommandType);
        if (!Initialiser.AppResources.Cache.Upsert(keyword, index.Value, gwaWithoutSet, streamId, applicationId, gwaSetCommandType.Value))
        {
          return false;
        }
      }
      return true;
    }

    private bool ExtractAndValidateGwa(IEnumerable<GsaRecord> records, out List<string> gwaLines, out Dictionary<string, int> mismatchByKw, bool visible = false)
    {
      gwaLines = new List<string>();
      var numByKw = new Dictionary<string, int>();
      foreach (var r in records)
      {
        if (r.Gwa(out var gwa, true))
        {
          foreach (var gwaL in gwa)
          {
            var pieces = gwaL.Split('\t');
            var keyword = pieces[0].Equals("SET_AT", StringComparison.InvariantCultureIgnoreCase) ? pieces[2] : pieces[1];
            keyword = keyword.Split(':').First().Split('.').First();
            if (!numByKw.ContainsKey(keyword))
            {
              numByKw.Add(keyword, 0);
            }
            numByKw[keyword]++;
          }
          gwaLines.AddRange(gwa);
        }
      }

      return ModelValidation(gwaLines, numByKw, out mismatchByKw, visible: visible);
    }

    private List<string> CollateGwaCommands(IEnumerable<GsaRecord> records)
    {
      var gwaLines = new List<string>();
      foreach (var r in records)
      {
        if (r.Gwa(out var gwa, true))
        {
          gwaLines.AddRange(gwa);
        }
      }
      return gwaLines;
    }
    #endregion

    #region model_validation_fns
    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(string gwaCommand, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(new string[] { gwaCommand }, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    private bool ModelValidation(IEnumerable<string> gwaCommands, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      var result = ModelValidation(gwaCommands, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly, visible);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(IEnumerable<string> gwaCommands, Dictionary<string, int> expectedCountByKw, out Dictionary<string, int> mismatchByKw, bool nodesWithAppIdOnly = false, bool visible = false)
    {
      mismatchByKw = new Dictionary<string, int>();

      //Use a real proxy, not the mock one used elsewhere in tests
      var gsaProxy = new GSAProxy();
      gsaProxy.NewFile(visible);
      foreach (var gwaC in gwaCommands)
      {
        gsaProxy.SetGwa(gwaC);
      }
      gsaProxy.Sync();
      if (visible)
      {
        gsaProxy.UpdateViews();
      }
      var lines =  gsaProxy.GetGwaData(expectedCountByKw.Keys, nodesWithAppIdOnly);
      lines.ForEach(l => l.Keyword = Helper.RemoveVersionFromKeyword(l.Keyword));
      gsaProxy.Close();

      foreach (var k in expectedCountByKw.Keys)
      {
        var numFound = lines.Where(l => l.Keyword.Equals(k, StringComparison.InvariantCultureIgnoreCase)).Count();
        if (numFound != expectedCountByKw[k])
        {
          mismatchByKw.Add(k, numFound);
        }
      }

      return (mismatchByKw.Keys.Count() == 0);
    }
    #endregion
  }
}
