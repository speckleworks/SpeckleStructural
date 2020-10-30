using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA.Test.Tests
{
  [TestFixture]
  public class GsaSchemaTests
  {
    [SetUp]
    public void SetUp()
    {
      var mockGSAObject = new Mock<IGSAProxy>();
      mockGSAObject.Setup(x => x.ParseGeneralGwa(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<int?>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<GwaSetCommandType?>.IsAny))
       .Callback(new MockGSAProxy.ParseCallback(MockGSAProxy.ParseGeneralGwa));
      mockGSAObject.Setup(x => x.NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
        .Returns(new Func<double, double, double, double, int>(MockGSAProxy.NodeAt));
      mockGSAObject.Setup(x => x.ConvertGSAList(It.IsAny<string>(), It.IsAny<GSAEntity>()))
        .Returns(new Func<string, GSAEntity, int[]>(MockGSAProxy.ConvertGSAList));

      Initialiser.Interface = mockGSAObject.Object;
      Initialiser.Cache = new GSACache();
    }

    [Test]
    public void Gsa0dLoad()
    {
      var gsaObjRx = new Gsa0dLoad()
      {
        ApplicationId = "AppId",
        Name = "Zero Dee Lode",
        Index = 1,
        NodeIndices = new List<int>() { 3, 4 },
        LoadCaseIndex = 3,
        GlobalAxis = true,
        LoadDirection = LoadDirection.XX,
        Value = 23
      };

      Assert.IsTrue(gsaObjRx.Gwa(out var gwa, true));
      Assert.IsNotEmpty(gwa);
      Assert.IsTrue(ModelValidation(gwa, GsaRecord.Keyword<Gsa0dLoad>(), 1, out var _));

      var gsaObjTx = new Gsa0dLoad();
      gsaObjTx.FromGwa(gwa);
    }

    [Test]
    public void Gsa0dLoadComplex()
    {
      var baseAppId = "Load3";
      var loadCase1 = new GsaLoadCase() { Index = 1, CaseType = StructuralLoadCaseType.Dead };
      var loadCase2 = new GsaLoadCase() { Index = 2, CaseType = StructuralLoadCaseType.Live };
      var node1 = new GsaNode() { Index = 1, X = 10, Y = 10, Z = 0 };
      var node2 = new GsaNode() { Index = 2, X = 30, Y = -10, Z = 10 };
      var axis1 = new GsaAxis() { Index = 1, OriginX = 0, OriginY = 0, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var axis2 = new GsaAxis() { Index = 2, OriginX = 20, OriginY = -20, OriginZ = 0, XDirX = 1, XDirY = 2, XDirZ = 0, XYDirX = -1, XYDirY = 1, XYDirZ = 0 };
      var load1 = new Gsa0dLoad() { Index = 1, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 1, AxisIndex = 1, LoadDirection = LoadDirection.X, Value = 10 };
      var load2 = new Gsa0dLoad() { Index = 2, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 1, AxisIndex = 2, LoadDirection = LoadDirection.X, Value = 10 };
      var load3 = new Gsa0dLoad() { Index = 3, NodeIndices = new List<int> { 1 }, LoadCaseIndex = 2, AxisIndex = 1, LoadDirection = LoadDirection.X, Value = 10 };
      var load4 = new Gsa0dLoad() { Index = 4, NodeIndices = new List<int> { 2 }, LoadCaseIndex = 2, AxisIndex = 2, LoadDirection = LoadDirection.X, Value = 10 };
      var load5 = new Gsa0dLoad() { Index = 5, ApplicationId = (baseAppId + "_XX"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = LoadDirection.XX, Value = 12 };
      var load6 = new Gsa0dLoad() { Index = 6, ApplicationId = (baseAppId + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 1, GlobalAxis = true, LoadDirection = LoadDirection.YY, Value = 13 };
      var load7 = new Gsa0dLoad() { Index = 7, ApplicationId = (baseAppId + "_YY"), NodeIndices = new List<int> { 1, 2 }, LoadCaseIndex = 2, GlobalAxis = true, LoadDirection = LoadDirection.YY, Value = 14 };

      var gwaCommands = CollateGwaCommands(new GsaRecord[] { loadCase1, loadCase2, node1, node2, axis1, axis2, load1, load2, load3, load4, load5, load6 });
      Assert.IsTrue(UpsertGwaIntoCache(gwaCommands));

      //Ensure the prerequisite objects are in the send objects collection
      //Note: don't need Axis here as the GWA from the cache is used instead of GSA__ objects
      Conversions.ToSpeckle(new GSANode());

      var dummy = new Gsa0dLoad();
      SchemaConversions.ToSpeckle(dummy);

      var sos = Initialiser.GSASenderObjects.Get<GSA0DLoad>().Select(g => g.Value).ToList();

      Assert.AreEqual(4, sos.Count());
      Assert.AreEqual(1, sos.Count(o => o.ApplicationId.Equals(baseAppId, StringComparison.InvariantCultureIgnoreCase) && o.Loading.Value.SequenceEqual(new double[] { 0, 0, 0, 12, 13, 0 })));
      Assert.AreEqual(3, sos.Count(o => string.IsNullOrEmpty(o.ApplicationId)));
      Assert.AreEqual(1, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE.3-1", "gsa/NODE.3-2" }) && o.Loading.Value.SequenceEqual(new double[] { 10, 0, 0, 0, 0, 0 })));
      Assert.AreEqual(1, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE.3-1" }) && o.Loading.Value.SequenceEqual(new double[] { 0, 10, 0, 0, 0, 0 })));
      Assert.AreEqual(1, sos.Count(o => o.NodeRefs.SequenceEqual(new[] { "gsa/NODE.3-2" }) && o.Loading.Value.SequenceEqual(new double[] { 0, -10, 0, 0, 0, 0 })));

      
      
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

    private List<string> CollateGwaCommands(IEnumerable<GsaRecord> records)
    {
      var gwaLines = new List<string>();
      foreach (var r in records)
      {
        if (r.Gwa(out string gwa, true))
        {
          gwaLines.Add(gwa);
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
    private bool ModelValidation(IEnumerable<string> gwaCommands, Dictionary<string, int> expectedCountByKw, out Dictionary<string, int> mismatchByKw, bool nodesWithAppIdOnly = false)
    {
      mismatchByKw = new Dictionary<string, int>();

      //Use a real proxy, not the mock one used elsewhere in tests
      var gsaProxy = new GSAProxy();
      gsaProxy.NewFile(false);
      foreach (var gwaC in gwaCommands)
      {
        gsaProxy.SetGwa(gwaC);
      }
      gsaProxy.Sync();
      //gsaProxy.UpdateViews();
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
