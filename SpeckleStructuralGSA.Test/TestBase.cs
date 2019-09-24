using System;
using System.Collections.Generic;
using System.Linq;
using Interop.Gsa_10_0;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public abstract class TestBase
  {
    protected IComAuto comAuto;

    protected GSAInterfacer gsaInterfacer;

    protected JsonSerializerSettings jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
    protected string jsonDecSearch = @"(\d*\.\d\d\d\d\d\d\d\d)\d*";
    protected string TestDataDirectory;

    protected TestBase(string directory)
    {
      TestDataDirectory = directory;
    }

    protected Mock<IComAuto> SetupMockGsaCom()
    {
      var mockGsaCom = new Mock<IComAuto>();

      //So far only these methods are actually called
      mockGsaCom.Setup(x => x.Gen_NodeAt(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>())).Returns((double x, double y, double z, double coin) => 1);
      mockGsaCom.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });
      mockGsaCom.Setup(x => x.VersionString()).Returns("Test\t1");
      mockGsaCom.Setup(x => x.LogFeatureUsage(It.IsAny<string>()));
      return mockGsaCom;
    }

    protected List<SpeckleObject> ModelToSpeckleObjects(GSATargetLayer layer, bool resultsOnly, bool embedResults, string[] cases = null, string[] resultsToSend = null)
    {
      gsaInterfacer.FullClearCache();

      //Clear out all sender objects that might be there from the last test preparation
      Initialiser.GSASenderObjects = new Dictionary<Type, List<object>>();

      //Compile all GWA commands with application IDs
      var senderProcessor = new SenderProcessor(TestDataDirectory, gsaInterfacer, layer, resultsOnly, embedResults, cases, resultsToSend);

      senderProcessor.GsaInstanceToSpeckleObjects(out var speckleObjects);

      return speckleObjects;
    }

    protected bool JsonCompareAreEqual(string j1, string j2)
    {
      try
      {
        var jt1 = JToken.Parse(j1);
        var jt2 = JToken.Parse(j2);

        if (!JToken.DeepEquals(jt1, jt2))
        {
          //Required until SpeckleCoreGeometry has an updated such that its constructors create empty dictionaries for the "properties" property by default,
          //which would bring it in line with the default creation of empty dictionaries when they are created by other means
          removeNullEmptyFields(jt1, new[] { "properties" });
          removeNullEmptyFields(jt2, new[] { "properties" });

          var newResult = JToken.DeepEquals(jt1, jt2);
        }

        return JToken.DeepEquals(jt1, jt2);
      }
      catch
      {
        return false;
      }
    }

    protected void removeNullEmptyFields(JToken token, string[] fields)
    {
      var container = token as JContainer;
      if (container == null) return;

      var removeList = new List<JToken>();
      foreach (var el in container.Children())
      {
        var p = el as JProperty;
        if (p != null && fields.Contains(p.Name) && p.Value != null && !p.Value.HasValues)
        {
          removeList.Add(el);
        }
        removeNullEmptyFields(el, fields);
      }

      foreach (var el in removeList)
      {
        el.Remove();
      }
    }
  }
}
