using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;
using SpeckleStructuralClasses;
using MathNet.Spatial;
using MathNet.Spatial.Euclidean;
using System.IO;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class MeshTests : TestBase
  {
    public MeshTests() : base(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\TestData\") { }

    [OneTimeSetUp]
    public void SetupTests()
    {
      //This uses the installed SpeckleKits - when SpeckleStructural is built, the built files are copied into the 
      // %LocalAppData%\SpeckleKits directory, so therefore this project doesn't need to reference the projects within in this solution
      SpeckleInitializer.Initialize();
      Initialiser.AppResources = new MockGSAApp(proxy: new GSAProxy());
    }

    [SetUp]
    public void SetUp()
    {
      ((GSACache)Initialiser.AppResources.Cache).Clear();
      //Clear out all sender objects that might be there from the last test preparation
      Initialiser.GsaKit.GSASenderObjects.Clear();
    }

    ///[Ignore("Just used for debugging at this stage, will be finished in the future as a test")]
    [TestCase(GSATargetLayer.Design, "AgBSsoCjO-uk.json")]
    //[TestCase(GSATargetLayer.Analysis, "S5pNxjmUH.json")]
    public void ReceiveFullMesh(GSATargetLayer layer, string fileName)
    {
      var json = Helper.ReadFile(fileName, TestDataDirectory);

      var mockGsaCom = SetupMockGsaCom();
      Initialiser.AppResources.Proxy.OpenFile("", false, mockGsaCom.Object);

      var receiverProcessor = new ReceiverProcessor(TestDataDirectory, Initialiser.AppResources);

      var rxObjs = ExtractObjects(fileName, TestDataDirectory);
      var rx2DElements = rxObjs.Select(t => t.Item2).Where(o => o is Structural2DElement).Cast<Structural2DElement>().ToList();
      Assert.IsTrue(rx2DElements.All(o => o.Faces != null && o.Faces.Count() > 0 && o.Vertices != null && o.Vertices.Count() > 0));

    }

    private List<Tuple<string, SpeckleObject>> ExtractObjects(string fileName, string directory)
    {
      return ExtractObjects(new string[] { fileName }, directory);
    }

      private List<Tuple<string, SpeckleObject>> ExtractObjects(string[] fileNames, string directory)
    {
      var speckleObjects = new List<Tuple<string, SpeckleObject>>();
      foreach (var fileName in fileNames)
      {
        var json = Helper.ReadFile(fileName, directory);
        var streamId = fileName.Split(new[] { '.' }).First();

        var response = ResponseObject.FromJson(json);
        for (int i = 0; i < response.Resources.Count(); i++)
        {
          speckleObjects.Add(new Tuple<string, SpeckleObject>(streamId, response.Resources[i]));
        }
      }
      return speckleObjects;
    }


    [Test]
    public void MeshTest()
    {
      Initialiser.AppResources.Proxy.OpenFile(Path.Combine(TestDataDirectory, "sjc.gwb"), false);

      var data = Initialiser.AppResources.Proxy.GetGwaData(new[] { "NODE.2", "MEMB.7" }, false);
      Assert.IsNotNull(data);
      Assert.Greater(data.Count(), 0);

      var slabIndicesToTry = new[] { 359 };

      //The ToSpeckle methods use the cache, so they need to be inserted here
      for (int i = 0; i < data.Count(); i++)
      {
        Initialiser.AppResources.Cache.Upsert(
          data[i].Keyword,
          data[i].Index,
          data[i].GwaWithoutSet,
          //This needs to be revised as this logic is in the kit too
          applicationId: (string.IsNullOrEmpty(data[i].ApplicationId))
            ? SpeckleStructuralClasses.Helper.CreateChildApplicationId(data[i].Index, SpeckleStructuralGSA.Helper.GetApplicationId(data[i].Keyword, data[i].Index)) 
            : data[i].ApplicationId,
          gwaSetCommandType: data[i].GwaSetType,
          streamId: data[i].StreamId
          );
      }

      var dummy = Activator.CreateInstance(typeof(GSANode));
      var retObject = Conversions.ToSpeckle((GSANode)dummy);

      dummy = Activator.CreateInstance(typeof(GSA2DMember));
      retObject = Conversions.ToSpeckle((GSA2DMember)dummy);

      var meshes = Initialiser.GsaKit.GSASenderObjects.Get<GSA2DMember>();
      Assert.AreEqual(50, meshes.Count);

      var noFaceMeshes = meshes.Where(m => ((Structural2DElementMesh)m.Value).Faces == null || ((Structural2DElementMesh)m.Value).Faces.Count() == 0).ToList();
      Assert.AreEqual(0, noFaceMeshes.Count());
    }

    [OneTimeTearDown]
    public void TearDown()
    {
      Initialiser.AppResources.Proxy.Close();
    }
  }
}
