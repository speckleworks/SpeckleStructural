using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.SchemaConversion
{
  public static class Structural1DPropertyExplicitToNative
  {
    private enum MaterialType
    {
      Generic = 0,
      Concrete = 1,
      Steel = 2
    }

    public static string ToNative(this Structural1DPropertyExplicit prop)
    {
      if (string.IsNullOrEmpty(prop.ApplicationId))
      {
        return "";
      }

      return Helper.ToNativeTryCatch(prop, () =>
      {
        var gsaSectionDict = new Dictionary<MaterialType, Func<Structural1DPropertyExplicit, int, GsaSection>>
        {
          { MaterialType.Concrete, ToGsaSectionConcrete },
          { MaterialType.Steel, ToGsaSectionSteel},
          { MaterialType.Generic, ToGsaSectionGeneric }
        };

        var keyword = GsaRecord.GetKeyword<GsaSection>();
        var streamId = Initialiser.AppResources.Cache.LookupStream(prop.ApplicationId);
        var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, prop.ApplicationId);

        var materialIndex = 0;
        var materialType = MaterialType.Generic;

        var materialSteelKeyword = typeof(GSAMaterialSteel).GetGSAKeyword();
        var materialConcKeyword = typeof(GSAMaterialConcrete).GetGSAKeyword();

        var res = Initialiser.AppResources.Cache.LookupIndex(materialSteelKeyword, prop.MaterialRef);
        if (res.HasValue)
        {
          materialIndex = res.Value;
          materialType = MaterialType.Steel;
        }
        else
        {
          res = Initialiser.AppResources.Cache.LookupIndex(materialConcKeyword, prop.MaterialRef);
          if (res.HasValue)
          {
            materialIndex = res.Value;
            materialType = MaterialType.Concrete;
          }
          else
          {
            //For generic, set index to 1 as a default
            materialIndex = 1;
          }
        }

        var gsaSection = gsaSectionDict[materialType](prop, materialIndex);
        gsaSection.Index = index;
        gsaSection.ApplicationId = prop.ApplicationId;
        gsaSection.Name = prop.Name;

        if (gsaSection.Gwa(out var gwaLines, false))
        {
          Initialiser.AppResources.Cache.Upsert(keyword, index, gwaLines.First(), streamId, prop.ApplicationId, GsaRecord.GetGwaSetCommandType<GsaSection>());
        }

        return "";
      });
    }

    private static GsaSection ToGsaSectionConcrete(Structural1DPropertyExplicit prop, int materialIndex)
    {
      return new GsaSection
      {
        Components = new List<GsaSectionComponentBase>()
        {
          CreateSectionComp(Section1dMaterialType.CONCRETE, materialIndex, prop.Area, prop.Iyy, prop.Izz, prop.J, prop.Ky, prop.Kz),
          //If any of the below have no properties being set, it's because they basically contain fixed values at this stage
          new SectionConc(),
          new SectionLink(),
          new SectionCover(),
          new SectionTmpl()
        }
      };
    }

    private static GsaSection ToGsaSectionSteel(Structural1DPropertyExplicit prop, int materialIndex)
    {
      return new GsaSection
      {
        Components = new List<GsaSectionComponentBase>()
        {
          CreateSectionComp(Section1dMaterialType.STEEL, materialIndex, prop.Area, prop.Iyy, prop.Izz, prop.J, prop.Ky, prop.Kz),
          new SectionSteel()
          {
            PlasElas = 1,
            NetGross = 1,
            Exposed = 1,
            Beta = 0.4,
            Locked = false,
            Type = SectionSteelSectionType.Undefined,
            Plate = SectionSteelPlateType.Undefined
          }
        }
      };
    }

    private static GsaSection ToGsaSectionGeneric(Structural1DPropertyExplicit prop, int materialIndex)
    {
      return new GsaSection
      {
        Components = new List<GsaSectionComponentBase>()
        {
          CreateSectionComp(Section1dMaterialType.GENERIC, materialIndex, prop.Area, prop.Iyy, prop.Izz, prop.J, prop.Ky, prop.Kz)
        }
      };
    }

    private static SectionComp CreateSectionComp(Section1dMaterialType matType, int matIndex, double? area, double? iyy, double? izz, double? j, double? ky, double? kz)
    {
      return new SectionComp()
      {
        MaterialType = matType,
        MaterialIndex = matIndex,
        ProfileGroup = Section1dProfileGroup.Explicit,
        ProfileDetails = new ProfileDetailsExplicit
        {
          Area = area,
          Iyy = iyy,
          Izz = izz,
          J = j,
          Ky = ky,
          Kz = kz
        }
      };
    }
  }
}
