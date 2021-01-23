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
    /*
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

      Initialiser.Instance.Cache = new GSACache();
      Initialiser.Instance.Interface = mockGSAObject.Object;
      Initialiser.Instance.AppUI = new SpeckleAppUI();
   
      Initialiser.Instance.Settings = new MockSettings();
    }
    */

    [Test]
    public void DependencyTest()
    {
      var mockSettings = new Mock<IGSASettings>();
      Initialiser.AppResources = new MockGSAApp(settings: mockSettings.Object, cache: new GSACache());
      //Initialiser.AppResources.Settings = settings.Object;

      mockSettings.SetupGet(x => x.TargetLayer).Returns(GSATargetLayer.Design);
      var rxDep = Initialiser.GsaKit.RxTypeDependencies;

      mockSettings.SetupGet(x => x.TargetLayer).Returns(GSATargetLayer.Analysis);
      rxDep = Initialiser.GsaKit.RxTypeDependencies;
    }

    //Just for the unusual ones - where there is no 1:1 relationship between GWA line and Speckle object
    

  }
}
