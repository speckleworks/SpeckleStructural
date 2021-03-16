using SpeckleCore;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public partial class StructuralMaterialConcrete
  {
    public StructuralMaterialConcrete() { }

    public StructuralMaterialConcrete(double youngsModulus, double shearModulus, double poissonsRatio, double density, double coeffThermalExpansion, double compressiveStrength, double maxStrain, double aggragateSize, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.YoungsModulus = youngsModulus;
      this.ShearModulus = shearModulus;
      this.PoissonsRatio = poissonsRatio;
      this.Density = density;
      this.CoeffThermalExpansion = coeffThermalExpansion;
      this.CompressiveStrength = compressiveStrength;
      this.MaxStrain = maxStrain;
      this.AggragateSize = aggragateSize;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.YoungsModulus *= factor;
      this.CompressiveStrength *= factor;
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralMaterialSteel
  {
    public StructuralMaterialSteel() { }

    public StructuralMaterialSteel(double youngsModulus, double shearModulus, double poissonsRatio, double density, double coeffThermalExpansion, double yieldStrength, double ultimateStrength, double maxStrain, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.YoungsModulus = youngsModulus;
      this.ShearModulus = shearModulus;
      this.PoissonsRatio = poissonsRatio;
      this.Density = density;
      this.CoeffThermalExpansion = coeffThermalExpansion;
      this.YieldStrength = yieldStrength;
      this.UltimateStrength = ultimateStrength;
      this.MaxStrain = maxStrain;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DProperty
  {
    public Structural1DProperty() { }

    public Structural1DProperty(SpeckleObject profile, Structural1DPropertyShape shape, bool hollow, double thickness, string catalogueName, string materialRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Profile = profile;
      this.Shape = shape;
      this.Hollow = hollow;
      this.Thickness = thickness;
      this.CatalogueName = catalogueName;
      this.MaterialRef = materialRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Profile != null)
      {
        this.Profile.Scale(factor);
      }
      if (this.Thickness != null)
      {
        this.Thickness *= factor;
      }
      if (this.Voids != null && this.Voids.Count() > 0)
      {
        for (var i = 0; i < this.Voids.Count(); i++)
        {
          this.Voids[i].Scale(factor);
        }
      }
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DProperty
  {
    public Structural2DProperty() { }

    public Structural2DProperty(double thickness, string materialRef, Structural2DPropertyReferenceSurface referenceSurface, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Thickness = thickness;
      this.MaterialRef = materialRef;
      this.ReferenceSurface = referenceSurface;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Thickness.HasValue)
      {
        this.Thickness *= factor;
      }

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralSpringProperty
  {
    public StructuralSpringProperty() { }

    public StructuralSpringProperty(StructuralAxis axis, StructuralVectorSix stiffness, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Axis = axis;
      this.Stiffness = stiffness;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }
      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Axis != null && this.Axis.Origin != null)
      {
        this.Axis.Origin.Scale(factor);
      }
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }
}
