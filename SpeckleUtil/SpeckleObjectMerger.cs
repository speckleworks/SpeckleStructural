using System;
using System.Collections.Generic;
using AutoMapper;
using SpeckleCore;

namespace SpeckleUtil
{
  public class SpeckleObjectMerger : ISpeckleObjectMerger
  {
    private IMapper mapper;

    public void Initialise(List<Type> typesToRecognise)
    {
      var config = new MapperConfiguration(cfg =>
      {
        foreach (var t in typesToRecognise)
        {
          cfg.CreateMap(t, t);
        }
        cfg.ForAllPropertyMaps(pm => true, (pm, c) => c.ResolveUsing(new IgnoreNullResolver(), pm.SourceMember.Name));
        cfg.AddGlobalIgnore(nameof(SpeckleObject.Properties));
      });

      config.AssertConfigurationIsValid();

      mapper = config.CreateMapper();
    }

    public SpeckleObject Merge(SpeckleObject src, SpeckleObject dest)
    {
      var resultingObject = mapper.Map(src, dest);
      return resultingObject;
    }
  }
}
