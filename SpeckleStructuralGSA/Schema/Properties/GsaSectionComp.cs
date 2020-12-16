using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;


namespace SpeckleStructuralGSA.Schema.Properties
{
  //The term "section component" here is a name applied to both the group as a whole as well as one member of the group, 
  //but the latter is shortened to SectionComp to distinguish them here
  [GsaType(GwaKeyword.SECTION_COMP, GwaSetCommandType.Set, false, StreamBucket.Model, true, true)]
  public class SectionComp : SectionComponent
  {
    public string Name { get => name; set { name = value; } }
    //The GWA specifies ref (i.e. record index) and name, but when a SECTION_COMP is inside a SECTION command, 
    //the ref is absent and name is blank (empty string) - so they'll be left out here
    public int? MatAnalIndex;
    public Section1dMaterialType MaterialType;
    public int? MaterialIndex;
    public double? OffsetY;
    public double? OffsetZ;
    public double? Rotation;
    public ComponentReflection Reflect;
    public Section1dTaperType TaperType;
    public int? Pool;

    public SectionComp() : base()
    {
      Version = 4;
    }

    public override bool FromGwa(string gwa)
    {
      //SECTION_COMP | ref | name | matAnal | matType | matRef | desc | offset_y | offset_z | rotn | reflect | pool
      //Note: the ref argument is missing when the GWA was embedded within a SECTION command, so need to detect this case
      //This also means the BasicFromGwa can't be called here because that does assume an index parameter
      var items = Split(gwa);

      if (items[0].StartsWith("set", StringComparison.OrdinalIgnoreCase))
      {
        items.Remove(items[0]);
      }
      if (!ParseKeywordVersionSid(items[0]))
      {
        return false;
      }
      items = items.Skip(1).ToList();

      //Detect presence or absense of ref (record index) argument based on number of items
      if (items.Count() == 12 && int.TryParse(items[0], out var foundIndex))
      {
        Index = foundIndex;
        items = items.Skip(1).ToList();
      }

      if (!FromGwaByFuncs(items, out var remainingItems, AddName, (v) => AddNullableIndex(v, out MatAnalIndex),
        (v) => Enum.TryParse(v, true, out MaterialType), (v) => AddNullableIndex(v, out MaterialIndex)))
      {
        return false;
      }
      items = remainingItems;

      if (!ProcessDesc(items[0]))
      {
        return false;
      }
      items = remainingItems;

      return (FromGwaByFuncs(items, out _, (v) => AddNullableDoubleValue(v, out OffsetY), (v) => AddNullableDoubleValue(v, out OffsetZ),
        (v) => AddNullableDoubleValue(v, out Rotation), (v) => Enum.TryParse(v, true, out Reflect), (v) => Enum.TryParse(v, true, out TaperType),
        (v) => AddNullableIntValue(v, out Pool)));
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = (GwaItems(out var items, includeSet) && Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return (gwa.Count() > 0);
    }

    //Note: the ref argument is missing when the GWA was embedded within a SECTION command, hence the addition of the additional boolean argument
    public override bool GwaItems(out List<string> items, bool includeSet = false, bool includeRef = false)
    {
      items = new List<string>();
      if (includeSet)
      {
        items.Add("SET");
      }
      if ((bool)GetType().GetAttribute<GsaType>("SelfContained"))
      {
        items.Add(Index.ToString());
      }

      //SECTION_COMP | ref | name | matAnal | matType | matRef | desc | offset_y | offset_z | rotn | reflect | pool
      if (includeRef && !AddItems(ref items, Index ?? 0))
      {
        return false;
      }
      return AddItems(ref items, Name, MatAnalIndex ?? 0, MaterialType.ToString(), MaterialIndex ?? 0, ProfileDetails.ToDesc(),
        OffsetY ?? 0, OffsetZ ?? 0, Rotation.ToString(), Reflect.ToString(), Pool ?? 0);
    }

    #region from_gwa_fns
    private bool ProcessDesc(string v)
    {
      var pieces = v.ListSplit(" ");
      if (!pieces[0].TryParseStringValue(out Section1dProfileGroup sectionProfileGroup))
      {
        return false;
      }

      if (sectionProfileGroup == Section1dProfileGroup.Explicit)
      {
        ProfileDetails = new ProfileDetailsExplicit();
        ProfileDetails.FromDesc(v);
      }
      else if (sectionProfileGroup == Section1dProfileGroup.Perimeter)
      {
        ProfileDetails = new ProfileDetailsPerimeter();
        ProfileDetails.FromDesc(v);
      }
      else if (sectionProfileGroup == Section1dProfileGroup.Catalogue)
      {
        ProfileDetails = new ProfileDetailsCatalogue();
        ProfileDetails.FromDesc(v);
      }
      else
      {
        //Standard
        if (!pieces[1].TryParseStringValue(out Section1dStandardProfileType profileType))
        {
          return false;
        }
        switch (profileType)
        {
          case Section1dStandardProfileType.Rectangular:
          case Section1dStandardProfileType.RectoCircular:
            ProfileDetails = new ProfileDetailsRectangular();
            break;

          case Section1dStandardProfileType.Circular:
            ProfileDetails = new ProfileDetailsCircular();
            break;

          case Section1dStandardProfileType.CircularHollow:
            ProfileDetails = new ProfileDetailsCircularHollow();
            break;

          case Section1dStandardProfileType.Taper:
            ProfileDetails = new ProfileDetailsTaper();
            break;

          case Section1dStandardProfileType.Ellipse:
            ProfileDetails = new ProfileDetailsEllipse();
            break;

          case Section1dStandardProfileType.GeneralISection:
            ProfileDetails = new ProfileDetailsGeneralI();
            break;

          case Section1dStandardProfileType.TaperTSection:
          case Section1dStandardProfileType.TaperAngleSection:
            ProfileDetails = new ProfileDetailsTaperTAngle();
            break;

          case Section1dStandardProfileType.RectoEllipse:
            ProfileDetails = new ProfileDetailsRectoEllipse();
            break;

          case Section1dStandardProfileType.TaperISection:
            ProfileDetails = new ProfileDetailsTaperI();
            break;

          case Section1dStandardProfileType.SecantPileSection:
          case Section1dStandardProfileType.SecantPileWall:
            ProfileDetails = new ProfileDetailsSecant();
            break;

          case Section1dStandardProfileType.Oval:
            ProfileDetails = new ProfileDetailsOval();
            break;

          default:
            ProfileDetails = new ProfileDetailsTwoThickness();
            break;
        }
        ProfileDetails.FromDesc(v);
      }
      return true;
    }
    #endregion
  }

  #region profile_details
  public abstract class ProfileDetails
  {
    public abstract string ToDesc();
    public abstract bool FromDesc(string desc);
    public Section1dProfileGroup Group;

    protected List<string> Split(string v)
    {
      try
      {
        return v.ListSplit(' ').ToList();
      }
      catch
      {
        return new List<string>();
      }
    }
  }

  public class ProfileDetailsCatalogue : ProfileDetails
  {
    public ProfileDetailsCatalogue()
    {
      Group = Section1dProfileGroup.Catalogue;
    }

    public override bool FromDesc(string desc)
    {
      throw new NotImplementedException();
    }

    public override string ToDesc()
    {
      throw new NotImplementedException();
    }
  }

  public class ProfileDetailsPerimeter : ProfileDetails
  {
    public ProfileDetailsPerimeter()
    {
      Group = Section1dProfileGroup.Perimeter;
    }

    public override bool FromDesc(string desc)
    {
      throw new NotImplementedException();
    }

    public override string ToDesc()
    {
      throw new NotImplementedException();
    }

  }

  public class ProfileDetailsExplicit : ProfileDetails
  {
    public ProfileDetailsExplicit()
    {
      Group = Section1dProfileGroup.Explicit;
    }

    public override bool FromDesc(string desc)
    {
      throw new NotImplementedException();
    }

    public override string ToDesc()
    {
      throw new NotImplementedException();
    }
  }

  public abstract class ProfileDetailsStandard : ProfileDetails
  {
    public Section1dStandardProfileType ProfileType;

    //This one happens to be common among all standard profile details
    public double? d => GetValue(0);

    protected List<double?> values = new List<double?>();

    protected ProfileDetailsStandard()
    {
      Group = Section1dProfileGroup.Standard;
    }

    public override bool FromDesc(string desc)
    {
      var items = Split(desc);

      //Assume first is the STD string and second is the type
      if (!items[1].TryParseStringValue(out ProfileType))
      {
        return false;
      }
      items = items.Skip(2).ToList();
      for (var i = 0; i < items.Count(); i++)
      {
        values.Add((double.TryParse(items[i], out var dVal)) ? (double?)dVal : 0);
      }
      return true;
    }

    public override string ToDesc()
    {
      var strItems = new List<string>() { Group.GetStringValue(), ProfileType.GetStringValue() };
      strItems.AddRange(values.Select(v => (v.HasValue ? v : 0).ToString()));
      return string.Join(" ", strItems);
    }

    protected double? GetValue(int index)
    {
      return (index < values.Count() ? values[index] : 0);
    }
  }

  public class ProfileDetailsRectangular : ProfileDetailsStandard
  {
    public double? b => GetValue(1);
  }

  public class ProfileDetailsTwoThickness : ProfileDetailsStandard 
  {
    public double? b => GetValue(1);
    public double? tw => GetValue(2);
    public double? tf => GetValue(3);
  }

  public class ProfileDetailsCircular : ProfileDetailsStandard 
  {
  }

  public class ProfileDetailsCircularHollow : ProfileDetailsStandard 
  {
    public double? t => GetValue(1); 
  }
  public class ProfileDetailsTaper : ProfileDetailsStandard
  {
    public double? bt => GetValue(1); 
    public double? bb => GetValue(2); 
  }
  
  public class ProfileDetailsEllipse : ProfileDetailsStandard 
  { 
    public double? b => GetValue(1); 
    public double? k => GetValue(2); 
  }
  
  public class ProfileDetailsGeneralI : ProfileDetailsStandard 
  { 
    public double? bt => GetValue(1); 
    public double? bb => GetValue(2); 
    public double? tw => GetValue(3); 
    public double? tft => GetValue(4); 
    public double? tfb => GetValue(5); 
  }
  
  public class ProfileDetailsTaperTAngle : ProfileDetailsStandard 
  { 
    public double? b => GetValue(1); 
    public double? twt => GetValue(2); 
    public double? twb => GetValue(3); 
    public double? tf => GetValue(4); 
  }
  
  public class ProfileDetailsRectoEllipse : ProfileDetailsStandard 
  { 
    public double? b => GetValue(1); 
    public double? df => GetValue(2); 
    public double? bf => GetValue(3); 
    public double? k => GetValue(4);    
  }
  
  public class ProfileDetailsTaperI : ProfileDetailsStandard 
  { 
    public double? b => GetValue(1); 
    public double? bt => GetValue(2); 
    public double? bb => GetValue(3); 
    public double? twt => GetValue(4); 
    public double? twb => GetValue(5); 
    public double? tft => GetValue(6); 
    public double? tfb => GetValue(7); 
  }
  
  public class ProfileDetailsSecant : ProfileDetailsStandard 
  { 
    public double? c => GetValue(1); 
    public double? n => GetValue(2); 
  }

  public class ProfileDetailsOval : ProfileDetailsStandard 
  { 
    public double? b => GetValue(1); 
    public double? t => GetValue(2); 
  }
  #endregion
}
