using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralGSA.SchemaConversion;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class ConversionTests
  {
    [SetUp]
    public void SetUp()
    {
      var mockGSAObject = new Mock<IGSAProxy>();

      mockGSAObject.Setup(x => x.ParseGeneralGwa(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<int?>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny, out It.Ref<GwaSetCommandType?>.IsAny, It.IsAny<bool>()))
      .Callback(new MockGSAProxy.ParseCallback(MockGSAProxy.ParseGeneralGwa));
      mockGSAObject.Setup(x => x.NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
        .Returns(new Func<double, double, double, double, int>(MockGSAProxy.NodeAt));
      mockGSAObject.Setup(x => x.FormatApplicationIdSidTag(It.IsAny<string>()))
        .Returns(new Func<string, string>(MockGSAProxy.FormatApplicationIdSidTag));
      mockGSAObject.Setup(x => x.FormatSidTags(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(new Func<string, string, string>(MockGSAProxy.FormatSidTags));
      mockGSAObject.Setup(x => x.ConvertGSAList(It.IsAny<string>(), It.IsAny<GSAEntity>()))
        .Returns(new Func<string, GSAEntity, int[]>(MockGSAProxy.ConvertGSAList));

      Initialiser.Cache = new GSACache();
      Initialiser.Interface = mockGSAObject.Object;
      Initialiser.AppUI = new SpeckleAppUI();
   
      Initialiser.Settings = new Settings();
    }

    //Just for the unusual ones - where there is no 1:1 relationship between GWA line and Speckle object
    [Test]
    public void Structural0DLoad()
    {
      var streamID = "testStream";

      //PREREQUISITES/REFERENCES - CONVERT TO GSA

      var node1 = new StructuralNode() { ApplicationId = "Node1", Name = "Node One", basePoint = new SpecklePoint(1, 2, 3) };
      var node2 = new StructuralNode() { ApplicationId = "Node2", Name = "Node Two", basePoint = new SpecklePoint(4, 5, 6) };
      var loadcase = new StructuralLoadCase() { ApplicationId = "LoadCase1", Name = "Load Case One", CaseType = StructuralLoadCaseType.Dead };
      GwaToCache(Conversions.ToNative(node1), streamID);
      GwaToCache(Conversions.ToNative(node2), streamID);
      GwaToCache(Conversions.ToNative(loadcase), streamID);

      //OBJECT UNDER TEST - CONVERT TO GSA

      var loading = new double[] { 10, 20, 30, 40, 50, 60 };
      var receivedObj = new Structural0DLoad()
      {
        ApplicationId = "Test0DLoad",
        Loading = new StructuralVectorSix(loading),
        NodeRefs = new List<string> { "Node1", "Node2" },
        LoadCaseRef = "LoadCase1"
      };
      GwaToCache(Structural0DLoadToNative.ToNative(receivedObj), streamID);

      ((IGSACache)Initialiser.Cache).Snapshot(streamID);

      //PREREQUISITES/REFERENCES - CONVERT TO SPECKLE

      Conversions.ToSpeckle(new GSANode());
      Conversions.ToSpeckle(new GSALoadCase());

      //OBJECT UNDER TEST - CONVERT TO SPECKLE

      Conversions.ToSpeckle(new GSA0DLoad());

      var sentObjectsDict = Initialiser.GSASenderObjects.GetAll();
      Assert.IsTrue(sentObjectsDict.ContainsKey(typeof(GSA0DLoad)));

      var sentObjs = sentObjectsDict[typeof(GSA0DLoad)].Select(o => ((IGSASpeckleContainer)o).Value).Cast<Structural0DLoad>().ToList();
      Assert.AreEqual(1, sentObjs.Count());
      Assert.IsTrue(sentObjs.First().Loading.Value.SequenceEqual(loading));
    }

    //Copied and modified from Receiver in SpeckleGSA - the Speckle object isn't copied to the cache here because that's only used for merging
    private bool GwaToCache(string gwaCommand, string streamId)
    {
      var lines = gwaCommand.Split(new[] { '\n' }).Where(l => !string.IsNullOrEmpty(l)).ToList();
      foreach (var l in lines)
      {
        //At this point the SID will be filled with the application ID
        Initialiser.Interface.ParseGeneralGwa(l, out var keyword, out var foundIndex,
          out var foundStreamId, out var foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);

        var originalSid = Initialiser.Interface.FormatSidTags(foundStreamId, foundApplicationId);
        var newSid = Initialiser.Interface.FormatSidTags(streamId, foundApplicationId);

        //If the SID tag has been set then update it with the stream
        gwaWithoutSet = (string.IsNullOrEmpty(originalSid))
            ? gwaWithoutSet.Replace(keyword, keyword + ":" + newSid)
            : gwaWithoutSet.Replace(originalSid, newSid);

        if (!Initialiser.Cache.Upsert(keyword, foundIndex.Value, gwaWithoutSet, streamId, foundApplicationId, gwaSetCommandType.Value))
        {
          return false;
        }
      }
      return true;
    }

  }
}
