using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpeckleGSAInterfaces;
using Moq;
using SpeckleStructuralClasses;
using SpeckleCore;
using System.Reflection;
using AutoMapper;
using SpeckleUtil;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class MergingTests
  {
    [Test]
    public void MergeTestAutomapper()
    {
      var newObj = new mTest { d = 5, e1 = eTest.NotSet, e2 = eTest.AVal };
      var existingStiffness = new StructuralVectorSix(new double[] { 10, 20, 30, 40, 50, 60 });
      var existingObj = new mTest { s = "Test", e1 = eTest.BVal, dArr = new double[] { 1, 2, 3 }, v = existingStiffness };

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
    public void MergeTestGSA_WithMerger()
    {
      var ls1 = new object[] { "PROP_SPR.3:{speckle_app_id:gh/a}", 1, "LSPxGeneral", "NO_RGB", "GLOBAL", "GENERAL", 0, 16, 0, 17, 0, 18, 0, 19, 0, 20, 0, 21, 0.21 };
      var gwa1 = string.Join("\t", ls1.Select(l => l.ToString()));

      PrepareInterfacerForGwaToSpeckle<GSASpringProperty>(gwa1, "PROP_SPR.3", "gh/a");

      //Call the ToSpeckle method, which just adds to the GSASenderObjects collection
      Conversions.ToSpeckle(new GSASpringProperty());
      var existing = (StructuralSpringProperty)((IGSASpeckleContainer)Initialiser.GSASenderObjects[typeof(GSASpringProperty)].First()).Value;

      var newToMerge = new StructuralSpringProperty() { DampingRatio = 1.5 };

      var speckleTypes = SpeckleUtil.Helper.GetLoadedSpeckleTypes();

      var mappableTypes = new List<Type>();
      var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => typeof(ISpeckleInitializer).IsAssignableFrom(t))).ToList();
      var relevantAssemblies =  assemblies.Where(a => a.FullName.Contains("GSA"));
      foreach (var assembly in relevantAssemblies)
      {
        foreach (var t in speckleTypes)
        {
          var methods = SpeckleUtil.Helper.GetExtensionMethods(assembly, t, "ToNative");
          if (methods != null && methods.Count() > 0)
          {
            mappableTypes.Add(t);
          }
        }
      }

      var merger = new SpeckleObjectMerger();
      merger.Initialise(mappableTypes);

      var resultingObject = merger.Merge(newToMerge, existing);
    }


    [Test]
    public void MergeTestGSA()
    {
      var ls1 = new object[] { "PROP_SPR.3:{speckle_app_id:gh/a}", 1, "LSPxGeneral", "NO_RGB", "GLOBAL", "GENERAL", 0, 16, 0, 17, 0, 18, 0, 19, 0, 20, 0, 21, 0.21 };
      var gwa1 = string.Join("\t", ls1.Select(l => l.ToString()));

      PrepareInterfacerForGwaToSpeckle<GSASpringProperty>(gwa1, "PROP_SPR.3", "gh/a");

      //Call the ToSpeckle method, which just adds to the GSASenderObjects collection
      Conversions.ToSpeckle(new GSASpringProperty());
      var existing = (StructuralSpringProperty) ((IGSASpeckleContainer)Initialiser.GSASenderObjects[typeof(GSASpringProperty)].First()).Value;

      var newToMerge = new StructuralSpringProperty() { DampingRatio = 1.5 };

      //Find all structural types which have a ToNative static method
      var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => typeof(ISpeckleInitializer).IsAssignableFrom(t))).ToList();
      var speckleTypes = new List<Type>();
      foreach (var assembly in assemblies)
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

      var mappableTypes = new List<Type>();
      foreach (var assembly in assemblies.Where(a => a.FullName.Contains("GSA")))
      {
        foreach (var t in speckleTypes)
        {
          var methods = GetExtensionMethods(assembly, t, "ToNative");
          if (methods != null && methods.Count() > 0)
          {
            mappableTypes.Add(t);
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

      var resultingObject = m.Map(newToMerge, existing);
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
      
      var query = from type in assembly.GetTypes()
                  where type.IsSealed && !type.IsGenericType && !type.IsNested
                  from method in type.GetMethods(BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic)
                  where method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                  where method.GetParameters()[0].ParameterType == extendedType
                  where method.Name == methodName
                  select method;
      return query;
    }

    private void PrepareInterfacerForGwaToSpeckle<T>(string gwaCommand, string keyword, string sid)
    {
      var testType = typeof(T);

      var mockGsaInterfacer = new Mock<IGSAInterfacer>();
      mockGsaInterfacer.Setup(x => x.GetGWARecords(It.IsAny<string>())).Returns((string x) =>
          {
            if (x.ToLower().StartsWith("get_all\tprop_spr"))
            {
              return new[] { gwaCommand };
            }
            return new[] { "" };
          }
        );
      mockGsaInterfacer.Setup(x => x.GetSID(It.IsAny<string>(), It.IsAny<int>())).Returns((string x, int y) =>
          {
            var xLower = x.ToLower();
            if (xLower.StartsWith("prop_spr"))
            {
              if (y == 1) return "gh/a";
            }
            return "";
          }
        );
      mockGsaInterfacer.Setup(x => x.GetDeletedGWARecords(It.IsAny<string>())).Returns((string x) => { return new[] { "" }; });

      Initialiser.GSASenderObjects = new Dictionary<Type, List<object>>
      {
        { testType, new List<object>() }
      };
      Initialiser.Interface = mockGsaInterfacer.Object;
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

    public double[] dArr { get; set; }

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
}
