using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpeckleGSAInterfaces;
using Moq;
using SpeckleStructuralClasses;
using SpeckleCore;
using System.Reflection;
using AutoMapper;
using SpeckleUtil;
using SpeckleCoreGeometryClasses;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class MergingTests
  {
    [SetUp]
    public void SetupMergeTests()
    {
      Initialiser.AppResources = new MockGSAApp();

      //Initialiser.Instance.Cache = new GSACache();
      //Initialiser.Instance.Settings = new MockSettings();
    }

    [Test]
    public void MergeTestAutomapperForStackOverlow()
    {
      var newObj = new cTest { dList = new List<double>() { 4, 5, 6 } };
      var existingObj = new cTest { dList = new List<double> { 1, 2, 3 } };

      var config = new MapperConfiguration(cfg =>
      {
        cfg.CreateMap<cTest, cTest>();
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.ResolveUsing(new ValueResolver(), pm.SourceMember.Name));
      });

      config.AssertConfigurationIsValid();

      var m = config.CreateMapper();

      var testResult = m.Map(newObj, existingObj);

      Assert.AreEqual(new List<double>() { 4, 5, 6 }, testResult.dList);
    }

    [Test]
    public void MergeTestAutomapper()
    {
      var newObj = new mTest { d = 5, e1 = eTest.NotSet, e2 = eTest.AVal, dArr = new List<double>(), v = new StructuralVectorSix(new double[] { 10, 20, 30, 40, 50, 60 } ) };
      var existingObj = new mTest { s = "Test", e1 = eTest.BVal, dArr = new List<double> { 1, 2, 3 }};

      var config = new MapperConfiguration(cfg =>
      {        
        cfg.CreateMap<mTest, mTest>();
        //cfg.CreateMap<StructuralVectorSix, StructuralVectorSix>(); This causes issues
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.ResolveUsing(new IgnoreNullResolver(), pm.SourceMember.Name));
      });

      config.AssertConfigurationIsValid();

      var m = config.CreateMapper();

      var testResult = m.Map(newObj, existingObj);

      Assert.AreEqual(5, testResult.d);
      Assert.AreEqual(eTest.BVal, testResult.e1);
      Assert.AreEqual(eTest.AVal, testResult.e2);
      Assert.AreEqual("Test", testResult.s);
    }

    [Test]
    public void MergeTestAutoMapperBaseTypes()
    {
      var newObj = new StructuralNode { Value = new List<double> { 1, 2, 3 } };
      var existingObj = new StructuralNode { Value = new List<double> { 4, 5, 6 } };

      var config = new MapperConfiguration(cfg =>
      {
        cfg.CreateMap<StructuralNode, StructuralNode>();
        cfg.CreateMap<SpecklePoint, SpecklePoint>();
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.ResolveUsing(new IgnoreNullResolver(), pm.SourceMember.Name));
      });

      config.AssertConfigurationIsValid();

      var m = config.CreateMapper();

      var testResult = m.Map(newObj, existingObj);

      Assert.IsTrue(existingObj.Value.SequenceEqual(new List<double> { 1, 2, 3 }));
    }

    [Test]
    public void MergeTestAutoMapperNode()
    {
      var newObj = new StructuralNode { Value = new List<double> { 1, 2, 3 }, Stiffness = new StructuralVectorSix(10, 11, 12, 13, 14, 15) };
      var existingObj = new StructuralNode { Value = new List<double> { 4, 5, 6 } };

      var m = SetupGSAMerger();

      var exceptionThrown = false;
      try
      {
        var testResult = m.Map(newObj, existingObj);
      }
      catch(Exception ex)
      {
        exceptionThrown = true;
      }

      Assert.IsFalse(exceptionThrown);
      Assert.IsNotNull(existingObj.Stiffness);
      Assert.IsTrue(existingObj.Stiffness.Value.SequenceEqual(new List<double> { 10, 11, 12, 13, 14, 15 }));
      Assert.IsTrue(existingObj.Value.SequenceEqual(new List<double> { 1, 2, 3 }));
    }

    [Test]
    public void MergeTestGSA_WithMerger()
    {
      var ls1 = new object[] { "PROP_SPR.4:{speckle_app_id:gh/a}", 1, "LSPxGeneral", "NO_RGB", "GLOBAL", "GENERAL", 0, 16, 0, 17, 0, 18, 0, 19, 0, 20, 0, 21, 0.21 };
      var gwa1 = string.Join(GSAProxy.GwaDelimiter.ToString(), ls1.Select(l => l.ToString()));

      PrepareInterfacerForGwaToSpeckle<GSASpringProperty>(gwa1, "PROP_SPR", "gh/a");

      //Call the ToSpeckle method, which just adds to the GSASenderObjects collection-
      Conversions.ToSpeckle(new GSASpringProperty());
      var existing = (StructuralSpringProperty)Initialiser.GsaKit.GSASenderObjects.Get<GSASpringProperty>().First().Value;

      var newToMerge = new StructuralSpringProperty() { DampingRatio = 1.5 };

      var speckleTypes = SpeckleUtil.Helper.GetLoadedSpeckleTypes();

      var mappingTypes = new List<Type>();
      var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => typeof(ISpeckleInitializer).IsAssignableFrom(t))).ToList();
      var relevantAssemblies =  assemblies.Where(a => a.FullName.Contains("GSA"));
      foreach (var assembly in relevantAssemblies)
      {
        foreach (var t in speckleTypes)
        {
          var methods = SpeckleUtil.Helper.GetExtensionMethods(assembly, t, "ToNative");
          if (methods != null && methods.Count() > 0)
          {
            mappingTypes.Add(t);
          }

          if (t.BaseType != null)
          {
            if (!mappingTypes.Contains(t.BaseType))
            {
              mappingTypes.Add(t.BaseType);
            }
          }
        }
      }

      var merger = new SpeckleObjectMerger();
      merger.Initialise(mappingTypes);

      var resultingObject = merger.Merge(newToMerge, existing);
    }

    [Test]
    public void MergeTestGSA()
    {
      var ls1 = new object[] { "PROP_SPR.4:{speckle_app_id:gh/a}", 1, "LSPxGeneral", "NO_RGB", "GLOBAL", "GENERAL", 0, 16, 0, 17, 0, 18, 0, 19, 0, 20, 0, 21, 0.21 };
      var gwa1 = string.Join(GSAProxy.GwaDelimiter.ToString(), ls1.Select(l => l.ToString()));

      PrepareInterfacerForGwaToSpeckle<GSASpringProperty>(gwa1, "PROP_SPR", "gh/a");

      //Call the ToSpeckle method, which just adds to the GSASenderObjects collection
      Conversions.ToSpeckle(new GSASpringProperty());
      var existing = (StructuralSpringProperty)Initialiser.GsaKit.GSASenderObjects.Get<GSASpringProperty>().First().Value;

      var newToMerge = new StructuralSpringProperty() { DampingRatio = 1.5 };

      var m = SetupGSAMerger();

      var resultingObject = m.Map(newToMerge, existing);
    }

    private IMapper SetupGSAMerger()
    {
      //Find all structural types
      var speckleTypeAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => typeof(IStructural).IsAssignableFrom(t))).ToList();
      var speckleTypes = new List<Type>();
      foreach (var assembly in speckleTypeAssemblies)
      {
        var types = assembly.GetTypes();
        foreach (var t in types)
        {
          if (typeof(SpeckleObject).IsAssignableFrom(t))
          {
            speckleTypes.Add(t);
          }
        }
      }

      var speckleToNativeAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("GSA"));
      var mappableTypes = new List<Type>();
      foreach (var assembly in speckleToNativeAssemblies)
      {
        foreach (var t in speckleTypes)
        {
          var methods = GetExtensionMethods(assembly, t, "ToNative");
          if (methods != null && methods.Count() > 0 && !mappableTypes.Contains(t))
          {
            mappableTypes.Add(t);

            if (t.BaseType != null && t.BaseType != typeof(SpeckleObject))
            {
              if (!mappableTypes.Contains(t.BaseType))
              {
                mappableTypes.Add(t.BaseType);
              }
            }
          }
        }
      }

      var config = new MapperConfiguration(cfg =>
      {
        foreach (var t in mappableTypes)
        {
          cfg.CreateMap(t, t);
        }
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.ResolveUsing(new IgnoreNullResolver(), pm.SourceMember.Name));
        cfg.AddGlobalIgnore(nameof(SpeckleObject.Properties));
      });

      config.AssertConfigurationIsValid();

      var m = config.CreateMapper();

      return m;
    }

    private static IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType, string methodName)
    {
      var methodNameFormatted = methodName.ToLower();
      var returnMethodInfos = new List<MethodInfo>();
      var types = assembly.GetTypes();
      var typesFiltered = types.Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested);
      foreach (var type in typesFiltered)
      {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var methodsFiltered = methods.Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));

        foreach (var method in methodsFiltered)
        {
          var methodParameters = method.GetParameters();
          if (methodParameters != null && methodParameters.Count() > 0)
          if (methodParameters[0].ParameterType == extendedType)
          {
            if (string.Equals(method.Name, methodName, StringComparison.InvariantCultureIgnoreCase))
            {
              returnMethodInfos.Add(method);
            }
          }
        }
      }

      return returnMethodInfos;
    }

    private void PrepareInterfacerForGwaToSpeckle<T>(string gwaCommand, string keyword, string sid)
    {
      var testType = typeof(T);

      var mockGsaInterfacer = new Mock<IGSAProxy>();
      mockGsaInterfacer.SetupGet(x => x.GwaDelimiter).Returns('\t');
      ((MockGSAApp)Initialiser.AppResources).Proxy = mockGsaInterfacer.Object;
      ((IGSACache)Initialiser.AppResources.Cache).Upsert(keyword, 1, gwaCommand, sid);
      Initialiser.GsaKit.GSASenderObjects.Clear();
    }
  }

  public enum eTest
  {
    NotSet,
    AVal,
    BVal
  }
  public class mTest
  {
    private double _secret;
    public double? d { get => _secret; set { _secret = value.HasValue ? value.Value : _secret; } }

    public string s { get; set; }

    public eTest e1 { get; set; }

    public eTest e2 { get; set; }

    public List<double> dArr { get; set; }

    public StructuralVectorSix v { get; set; }
  }

  

  public class IgnoreNullResolver : IMemberValueResolver<object, object, object, object>
  {
    public object Resolve(object source, object destination, object sourceMember, object destinationMember, ResolutionContext context)
    {
      if (sourceMember is Enum && sourceMember.Equals(GetDefaultValue(sourceMember.GetType())))
      {
        return destinationMember;
      }      
      return sourceMember ?? destinationMember;
    }

    private object GetDefaultValue(Type t)
    {
      if (t.IsValueType)
      {
        return Activator.CreateInstance(t);
      }
      else
      {
        return null;
      }
    }
  }
  //For Stack Overlow
  public class cTest
  {
    public List<double> dList { get; set; }
  }

  public class ValueResolver : IMemberValueResolver<object, object, object, object>
  {
    public object Resolve(object source, object destination, object sourceMember, object destinationMember, ResolutionContext context)
    {
      var collection = sourceMember as System.Collections.ICollection;
      if (collection != null && collection.Count == 0)
      {
        return destinationMember;
      }
      return sourceMember ?? destinationMember;
    }
  }
}
