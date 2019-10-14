using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using AutoMapper;
using SpeckleGSAInterfaces;
using Moq;
using SpeckleStructuralClasses;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  [TestFixture]
  public class MergingTests
  {
    [Test]
    public void Merging()
    {
      var config = new MapperConfiguration(cfg =>
      {
        //cfg.CreateMap<List<double>, List<double>>().ConvertUsing<DoubleArrayTypeConverter>();
        //cfg.CreateMap<double, double?>().ConvertUsing<ZeroDoubleNullTypeConverter>();
        //cfg.CreateMap<double, double>().ForAllMembers()
        //cfg.CreateMap<StructuralVectorSix, StructuralVectorSix>().ForAllMembers(dest => dest.PreCondition)

        //cfg.CreateMap<double, double>().ForAllMembers(x => x.Condition((d, b) => d > 0));
        //cfg.CreateMap<double, double>().ForAllMembers(opt => opt.PreCondition(src => (src > 0d)));
        //cfg.CreateMap<double, double>().ConvertUsing<ZeroDoubleTypeConverter>();

        cfg.CreateMap<double, double>().BeforeMap((src, dest) => dest = -1);

        cfg.CreateMap<StructuralVectorSix, StructuralVectorSix>();
        //cfg.ForAllPropertyMaps(pm => pm.SourceType == typeof(double) && pm.DestinationType == typeof(double), (pm, c) => c.PreCondition(o => (double) o > 0d));

        //cfg.ForAllPropertyMaps(pm => pm.TypeMap.SourceType == typeof(double), (pm, c) => c.MapFrom(new IgnoreNullResolver(), pm.SourceMember.Name));

        //cfg.CreateMap<StructuralSpringProperty, StructuralSpringProperty>().ForMember(dest => dest.ApplicationId, opt => opt.MapFrom(src => string.Join(" ", src.ApplicationId, "-Test")));
        //cfg.CreateMap<StructuralSpringProperty, StructuralSpringProperty>().ForAllMembers(o => o.Condition((src, dest, value) => (double)value != 0d));
        //cfg.CreateMap<StructuralSpringProperty, StructuralSpringProperty>().ForMember(d => d.DampingRatio, o => o.MapFrom<CustomerResolver>());
      }
      );
      config.AssertConfigurationIsValid();
      var mapper = new Mapper(config);

      var vSrc = new StructuralVectorSix(new double[] { 0, 2, 3, 4, 5, 6 });
      var vDest = new StructuralVectorSix(new double[] { 10, 20, 30, 40, 50, 60 });

      var testResult = mapper.Map(vSrc, vDest);
      
      var ls1 = new object[] { "PROP_SPR.3:{speckle_app_id:gh/a}", 1, "LSPxGeneral", "NO_RGB", "GLOBAL", "GENERAL", 0, 16, 0, 17, 0, 18, 0, 19, 0, 20, 0, 21, 0.21 };
      var gwa1 = string.Join("\t", ls1.Select(l => l.ToString()));

      PrepareGwaToSpeckle<GSASpringProperty>(gwa1, "PROP_SPR.3", "gh/a");

      Conversions.ToSpeckle(new GSASpringProperty());

      var existing = ((IGSASpeckleContainer)Initialiser.GSASenderObjects[typeof(GSASpringProperty)].First()).Value;

      var ls2 = new object[] { "PROP_SPR.3:{speckle_app_id:gh/a}", 1, "", "NO_RGB", "GLOBAL", "GENERAL", 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0.5 };
      var gwa2 = string.Join("\t", ls2.Select(l => l.ToString()));

      PrepareGwaToSpeckle<GSASpringProperty>(gwa2, "PROP_SPR.3", "gh/a");

      Conversions.ToSpeckle(new GSASpringProperty());

      var newToMerge = ((IGSASpeckleContainer)Initialiser.GSASenderObjects[typeof(GSASpringProperty)].First()).Value;

      var resultingObject = mapper.Map(newToMerge, existing);

      /*
        Mapper.Initialize(cfg =>
        {
          // necessary if you are mapping parent to a parent
          cfg.CreateMap<Parent, Parent>()
              .ForAllMembers(options =>
              {
                options.Condition(src => src.DestinationValue == null);
              });
          // necessary if you are mapping your child to a child
          cfg.CreateMap<Child, Child>()
              .ForAllMembers(options =>
              {
                options.Condition(src => src.DestinationValue == null);
              });
        });
        */
    }

    private void PrepareGwaToSpeckle<T>(string gwaCommand, string keyword, string sid)
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

  class IgnoreNullResolver : IMemberValueResolver<object, object, object, object>
  {
    public object Resolve(object source, object destination, object sourceMember, object destinationMember, ResolutionContext context)
    {
      return sourceMember ?? destinationMember;
    }
  }

  public class CustomResolver : IValueResolver<object, object, double>
  {
    public double Resolve(object source, object dest, double destMember, ResolutionContext context)
    {
      return destMember * 2;
    }
  }

  public class ZeroToNullIntTypeConverter : ITypeConverter<int, int?>
  {
    public int? Convert(int source, int? destination, ResolutionContext context)
    {
      if (source == 0)
      {
        return destination;
      }
      else
      {
        return source;
      }
    }
  }

  public class ZeroDoubleTypeConverter : ITypeConverter<double, double>
  {
    public double Convert(double source, double destination, ResolutionContext context)
    {
      if (source == 0)
      {
        return destination;
      }
      else
      {
        return source;
      }
    }
  }

  public class ZeroDoubleNullTypeConverter : ITypeConverter<double, double?>
  {
    public double? Convert(double source, double? destination, ResolutionContext context)
    {
      if (source == 0)
      {
        return null;
      }
      else
      {
        return source;
      }
    }
  }
}
