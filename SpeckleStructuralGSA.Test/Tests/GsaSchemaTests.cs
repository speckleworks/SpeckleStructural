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
      var mockGSAObject = new Mock<IGSAProxy>();
      mockGSAObject.Setup(x => x.ParseGeneralGwa(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<int?>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<GwaSetCommandType?>.IsAny, It.IsAny<bool>()))
       .Callback(new MockGSAProxy.ParseCallback(MockGSAProxy.ParseGeneralGwa));
      mockGSAObject.Setup(x => x.NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
        .Returns(new Func<double, double, double, double, int>(MockGSAProxy.NodeAt));
      mockGSAObject.Setup(x => x.ConvertGSAList(It.IsAny<string>(), It.IsAny<GSAEntity>()))
        .Returns(new Func<string, GSAEntity, int[]>(MockGSAProxy.ConvertGSAList));
      mockGSAObject.SetupGet(x => x.GwaDelimiter).Returns(GSAProxy.GwaDelimiter);

      Initialiser.Interface = mockGSAObject.Object;
      Initialiser.Cache = new GSACache();
      Initialiser.AppUI = new SpeckleAppUI();
    }

    [Test]
    public void GsaLoadCaseToNative()
    {
      var load1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Generic, ApplicationId = "lc1", Name = "LoadCaseOne" };
      var load2 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "lc2", Name = "LoadCaseTwo" };

      var load1gwa = StructuralLoadCaseToNative.ToNative(load1);
      var load2gwa = StructuralLoadCaseToNative.ToNative(load2);

      Assert.IsFalse(string.IsNullOrEmpty(load1gwa));
      Assert.IsFalse(string.IsNullOrEmpty(load2gwa));

      Assert.IsTrue(ModelValidation(new string[] { load1gwa, load2gwa }, new Dictionary<string, int> { { GsaRecord.Keyword<GsaLoadCase>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());

      var gsaLoadCase1 = new GsaLoadCase();
      var gsaLoadCase2 = new GsaLoadCase();
      Assert.IsTrue(gsaLoadCase1.FromGwa(load1gwa));
      Assert.IsTrue(gsaLoadCase2.FromGwa(load2gwa));
    }

    //This just tests transitions from the GSA schema to GWA commands, and back again, since there is no need at the moment for a ToNative() method for StructuralAxis
    [Test]
    public void GsaAxis()
    {
      Assert.IsTrue(gsaAxis1.Gwa(out var axis1gwa));
      Assert.IsTrue(gsaAxis2.Gwa(out var axis2gwa));

      Assert.IsTrue(ModelValidation(new string[] { axis1gwa.First(), axis2gwa.First() }, new Dictionary<string, int> { { GsaRecord.Keyword<GsaAxis>(), 2 } }, out var mismatchByKw));
      Assert.Zero(mismatchByKw.Keys.Count());
      
      Assert.IsTrue(gsaAxis1.FromGwa(axis1gwa.First()));
      Assert.IsTrue(gsaAxis2.FromGwa(axis2gwa.First()));
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
      var storeyGwas = StructuralStoreyToNative.ToNative(storey1).Split('\n');
      Helper.GwaToCache(storeyGwas, streamId1);

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
      var plane1Gwas = StructuralLoadPlaneToNative.ToNative(plane1).Split('\n');
      Helper.GwaToCache(plane1Gwas, streamId1);

      var plane2 = new StructuralLoadPlane()
      {
        ApplicationId = "lp2",
        ElementDimension = 1,
        Tolerance = 0.1,
        Span = 1,
        SpanAngle = 0,
        StoreyRef = "TestStorey"
      };
      var plane2Gwas = StructuralLoadPlaneToNative.ToNative(plane2).Split('\n');
      Helper.GwaToCache(plane2Gwas, streamId1);

      var loadCase1 = new StructuralLoadCase() { CaseType = StructuralLoadCaseType.Dead, ApplicationId = "LcDead", Name = "Dead Load Case" };
      var loadCase1Gwas = StructuralLoadCaseToNative.ToNative(loadCase1).Split('\n');
      Helper.GwaToCache(loadCase1Gwas, streamId1);

      var polylineCoords = CreateFlatRectangleCoords(0, 0, 0, 30, 5, 5);
      var loading = new StructuralVectorThree(new double[] { 0, -10, -5 });
      var load2dPanelWithoutPlane = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel1");
      var load2dGwa1s = Structural2DLoadPanelToNative.ToNative(load2dPanelWithoutPlane).Split('\n');
      Helper.GwaToCache(load2dGwa1s, streamId1);

      var load2dPanelWithPlane1 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel2") { LoadPlaneRef = "lp1" };
      var load2dGwa2s = Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane1).Split('\n');
      Helper.GwaToCache(load2dGwa2s, streamId1);

      var load2dPanelWithPlane2 = new Structural2DLoadPanel(polylineCoords, loading, "LcDead", "loadpanel3") { LoadPlaneRef = "lp2" };
      var load2dGwa3s = Structural2DLoadPanelToNative.ToNative(load2dPanelWithPlane2).Split('\n');
      Helper.GwaToCache(load2dGwa3s, streamId1);

      var allGwa = new List<string>();
      allGwa.AddRange(storeyGwas);
      allGwa.AddRange(plane1Gwas);
      allGwa.AddRange(plane2Gwas);
      allGwa.AddRange(loadCase1Gwas);
      allGwa.AddRange(load2dGwa1s);
      allGwa.AddRange(load2dGwa2s);
      allGwa.AddRange(load2dGwa3s);

      //Try all the entities' GWA commands to check if the 
      Assert.IsTrue(ModelValidation(allGwa,
        new Dictionary<string, int> {
          { GsaRecord.Keyword<GsaAxis>(), 3 },
          { GsaRecord.Keyword<GsaLoadCase>(), 1 },
          { GsaRecord.Keyword<GsaGridPlane>(), 3 } ,
          { GsaRecord.Keyword<GsaGridSurface>(), 3 },
          { GsaRecord.Keyword<GsaLoadGridArea>(), 6 }
        },
        out var mismatchByKw, visible: true));
      Assert.Zero(mismatchByKw.Keys.Count());
      Assert.Zero(((SpeckleAppUI)Initialiser.AppUI).GroupMessages().Count());
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
        LoadDirection = LoadDirection6.XX,
        Value = 23
      };

      Assert.IsTrue(gsaObjRx.Gwa(out var gwa, true));
      Assert.IsNotEmpty(gwa);
      Assert.IsTrue(ModelValidation(gwa, GsaRecord.Keyword<GsaLoadNode>(), 1, out var _));
    }

    [Test]
    public void GsaLoadGridAreaToNative()
    {
      var loadCaseAppId = "LoadCase1";
      var loadPanelAppId = "LoadPanel1";
      var loadCase = new StructuralLoadCase
      {
        ApplicationId = loadCaseAppId,
        CaseType = StructuralLoadCaseType.Dead
      };

      var loadPanel = new Structural2DLoadPanel
      {
        ApplicationId = loadPanelAppId,
        basePolyline = new SpecklePolyline(CreateFlatRectangleCoords(10, 10, 10, angleDegrees: 45, 20, 30)),
        Loading = new StructuralVectorThree(new double[] { 0, 0, 10000000 }),
        LoadCaseRef = "LoadCase1"
      };

      var LoadCaseGwa = loadCase.ToNative();
      var LoadPanelGwa = Structural2DLoadPanelToNative.ToNative(loadPanel).Split('\n');
      Assert.AreEqual(4, LoadPanelGwa.Count()); //should be an axis, plane surface and a load panel

      var gsaLoadCase = new GsaLoadCase() { ApplicationId = loadCaseAppId, CaseType = StructuralLoadCaseType.Dead, Index = 1 };
      var gsaAxis = new GsaAxis() { Index = 1, OriginX = 10, OriginY = 10, OriginZ = 10, XDirX = Math.Sqrt(2), XDirY = Math.Sqrt(2), XYDirX = -Math.Sqrt(2), XYDirY = Math.Sqrt(2) };
      var gsa2dLoadPanel = new GsaLoadGridArea();

      Assert.IsTrue(gsaAxis.Gwa(out var gsaAxisGwa));
      Assert.IsTrue(gsaLoadCase.Gwa(out var gsaLoadCaseGwa));
      Assert.IsTrue(gsa2dLoadPanel.Gwa(out var gsa2dLoadPanelGwa));
    }

    [Test]
    public void GsaLoadNodeToSpeckle()
    {
      var baseAppId1 = "LoadFromSpeckle1";
      var baseAppId2 = "LoadFromSpeckle2";
      var loadCase1 = new GsaLoadCase() { Index = 1, CaseType = StructuralLoadCaseType.Dead };
      var loadCase2 = new GsaLoadCase() { Index = 2, CaseType = StructuralLoadCaseType.Live };
      var node1 = new GsaNode() { Index = 1, X = 10, Y = 10, Z = 0 };
      var node2 = new GsaNode() { Index = 2, X = 30, Y = -10, Z = 10 };
      var axis1 = new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var axis2 = new GsaAxis() { Index = 2, OriginX = 20, OriginY = -20, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var load1 = new GsaLoadNode() { Index = 1, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 1, AxisIndex = 1, LoadDirection = LoadDirection6.X, Value = 10 };
      var load2 = new GsaLoadNode() { Index = 2, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 1, AxisIndex = 2, LoadDirection = LoadDirection6.X, Value = 10 };
      var load3 = new GsaLoadNode() { Index = 3, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, AxisIndex = 1, LoadDirection = LoadDirection6.X, Value = 10 };
      var load4 = new GsaLoadNode() { Index = 4, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 2, AxisIndex = 2, LoadDirection = LoadDirection6.X, Value = 10 };
      var load5 = new GsaLoadNode() { Index = 5, ApplicationId = (baseAppId1 + "_XX"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = LoadDirection6.XX, Value = 12 };
      var load6 = new GsaLoadNode() { Index = 6, ApplicationId = (baseAppId1 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = LoadDirection6.YY, Value = 13 };
      var load7 = new GsaLoadNode() { Index = 7, ApplicationId = (baseAppId2 + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = LoadDirection6.YY, Value = 14 };
      var load8 = new GsaLoadNode() { Index = 8, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = LoadDirection6.Z, Value = -10 };  //Test global without application ID

      Assert.IsTrue(ExtractAndValidateGwa(new GsaRecord[] { loadCase1, loadCase2, node1, node2, axis1, axis2, load1, load2, load3, load4, load5, load6, load7, load8 }, 
        out var gwaCommands, out var mismatchByKw));

      Assert.IsTrue(UpsertGwaIntoCache(gwaCommands));

      //Ensure the prerequisite objects are in the send objects collection
      //Note: don't need Axis here as the GWA from the cache is used instead of GSA__ objects
      Conversions.ToSpeckle(new GSANode());

      var dummy = new GsaLoadNode();
      SchemaConversion.GsaLoadNodeToSpeckle.ToSpeckle(dummy);

      var sos = Initialiser.GSASenderObjects.Get<GSA0DLoad>().Select(g => g.Value).Cast<Structural0DLoad>().ToList();

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
        Initialiser.Interface.ParseGeneralGwa(gwaC, out var keyword, out var index, out var streamId, out var applicationId, out var gwaWithoutSet, out var gwaSetCommandType);
        if (!Initialiser.Cache.Upsert(keyword, index.Value, gwaWithoutSet, streamId, applicationId, gwaSetCommandType.Value))
        {
          return false;
        }
      }
      return true;
    }

    private bool ExtractAndValidateGwa(IEnumerable<GsaRecord> records, out List<string> gwaLines, out Dictionary<string, int> mismatchByKw)
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

      return ModelValidation(gwaLines, numByKw, out mismatchByKw);
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

    #region model_validation_fns
    //It's assumed the gwa comands are in the correct order
    private bool ModelValidation(string gwaCommand, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false)
    {
      var result = ModelValidation(new string[] { gwaCommand }, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly);
      mismatch = (mismatchByKw == null || mismatchByKw.Keys.Count() == 0) ? 0 : mismatchByKw[keyword];
      return result;
    }

    private bool ModelValidation(IEnumerable<string> gwaCommands, string keyword, int expectedCount, out int mismatch, bool nodesWithAppIdOnly = false)
    {
      var result = ModelValidation(gwaCommands, new Dictionary<string, int>() { { keyword, expectedCount } }, out var mismatchByKw, nodesWithAppIdOnly);
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
