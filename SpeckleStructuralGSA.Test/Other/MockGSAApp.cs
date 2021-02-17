using System;
using Moq;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public class MockGSAApp : IGSAAppResources
  {
    public IGSASettings Settings { get; set; }

    public IGSAProxy Proxy { get; set; }

    public IGSACacheForKit Cache { get; set; }

    public IGSAMessenger Messenger { get; set; }

    //Default test implementations
    public MockGSAApp(IGSASettings settings = null, IGSAProxy proxy = null, IGSACacheForKit cache = null, IGSAMessenger messenger = null)
    {
      Cache = cache ?? new GSACache();
      Settings = settings ?? new MockSettings();
      if (proxy == null)
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
        mockGSAObject.Setup(x => x.GetUnits()).Returns("m");

        Proxy = mockGSAObject.Object;
      }
      else
      {
        Proxy = proxy;
      }
      Messenger = messenger ?? new MockGSAMessenger();
    }
  }
}
