using System;
using System.Collections.Generic;
using System.Linq;
using Interop.Gsa_10_1;
using Moq;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  /*
  public class TestProxy : IGSAProxy
  {
    
    private readonly Dictionary<int, List<double>> nodes = new Dictionary<int, List<double>>();
    private readonly Mock<IComAuto> mockGSAObject = new Mock<IComAuto>();
    private readonly List<ProxyGwaLine> data = new List<ProxyGwaLine>();

    public TestProxy()
    {
      //So far only these methods are actually called
      mockGSAObject.Setup(x => x.GwaCommand(It.IsAny<string>())).Returns((string x) => { return x.Contains("GET") ? (object)"" : (object)1; });
      mockGSAObject.Setup(x => x.VersionString()).Returns("Test\t1");
      mockGSAObject.Setup(x => x.LogFeatureUsage(It.IsAny<string>()));
      mockGSAObject.Setup(x => x.SetLocale(It.IsAny<Locale>()));
      mockGSAObject.Setup(x => x.SetLocale(It.IsAny<Locale>()));
      mockGSAObject.Setup(x => x.NewFile());
      mockGSAObject.Setup(x => x.DisplayGsaWindow(It.IsAny<bool>()));
    }

    public new string GetUnits() => "m";

    
    public new void NewFile(bool showWindow = true, object gsaInstance = null)
    {
      base.NewFile(showWindow, gsaInstance: mockGSAObject.Object);
    }

    public new void OpenFile(string path, bool showWindow = true, object gsaInstance = null)
    {
      base.OpenFile(path, showWindow, gsaInstance: mockGSAObject.Object);
    }
    
    public new int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      return ResolveIndex(x, y, z, coincidenceTol);
    }

    
    public void AddDataLine(string keyword, int index, string streamId, string applicationId, string gwaWithoutSet, GwaSetCommandType gwaSetType)
    {
      var line = new ProxyGwaLine() { Keyword = keyword, Index = index, StreamId = streamId, ApplicationId = applicationId, GwaWithoutSet = gwaWithoutSet, GwaSetType = gwaSetType };
      ExecuteWithLock(() => data.Add(line));
    }

    public new List<ProxyGwaLine> GetGwaData(IEnumerable<string> keywords, bool nodeApplicationIdFilter)
    {
      return data;
    }

    private int ResolveIndex(double x, double y, double z, double tol)
    {
      return ExecuteWithLock(() =>
      {
        int currMaxIndex = 1;
        if (nodes.Keys.Count() == 0)
        {
          nodes.Add(currMaxIndex, new List<double> { x, y, z });
          return currMaxIndex;
        }
        foreach (var i in nodes.Keys)
        {
          if ((WithinTol(x, nodes[i][0], tol)) && (WithinTol(y, nodes[i][1], tol)) && (WithinTol(z, nodes[i][2], tol)))
          {
            return i;
          }
          currMaxIndex = i;
        }
        for (int i = 1; i <= (currMaxIndex + 1); i++)
        {
          if (!nodes.Keys.Contains(i))
          {
            nodes.Add(i, new List<double> { x, y, z });
            return i;
          }
        }
        nodes.Add(currMaxIndex + 1, new List<double> { x, y, z });
        return (currMaxIndex + 1);
      });
    }

    public new void UpdateCasesAndTasks() { }
    public new void UpdateViews() { }

    private bool WithinTol(double x, double y, double tol)
    {
      return (Math.Abs(x - y) <= tol);
    }
  }
  */
}
