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
      mockGSAObject.SetupGet(x => x.GwaDelimiter).Returns(GSAProxy.GwaDelimiter);

      Initialiser.Cache = new GSACache();
      Initialiser.Interface = mockGSAObject.Object;
      Initialiser.AppUI = new SpeckleAppUI();
   
      Initialiser.Settings = new MockSettings();
    }

    //Just for the unusual ones - where there is no 1:1 relationship between GWA line and Speckle object
    

  }
}
