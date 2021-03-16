using System.Collections.Generic;
using System.Linq;
using SpeckleCoreGeometryClasses;

namespace SpeckleStructuralClasses
{
  public partial class StructuralAssembly
  {
    public StructuralAssembly() { }

    public StructuralAssembly(double[] value, string[] elementRefs, SpeckleLine baseLine, SpecklePoint orientationPoint, int numPoints = 0, double width = 0, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.ApplicationId = applicationId;
      this.ElementRefs = elementRefs.ToList();
      this.Value = value.ToList();
      this.OrientationPoint = orientationPoint;
      this.NumPoints = numPoints;
      this.Width = width;
      this.BaseLine = baseLine;
      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Value != null)
      {
        for (var i = 0; i < this.Value.Count(); i++)
        {
          this.Value[i] *= factor;
        }
      }
      if (this.OrientationPoint != null && this.OrientationPoint.Value != null)
      {
        for (var i = 0; i < this.OrientationPoint.Value.Count(); i++)
        {
          this.OrientationPoint.Value[i] *= factor;
        }
      }
      //This is currently not scaled by the ScaleProperties below
      if (this.PointDistances != null && this.PointDistances.Count() > 0)
      {
        this.PointDistances = this.PointDistances.Select(d => d * factor).ToList();
      }

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralConstructionStage
  {
    public StructuralConstructionStage() { }

    public StructuralConstructionStage(string[] elementRefs, int stageDays, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.ElementRefs = elementRefs.ToList();
      this.StageDays = stageDays;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      this.GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralStagedNodalRestraints
  {
    public StructuralStagedNodalRestraints() { }

    public StructuralStagedNodalRestraints(StructuralVectorBoolSix restraint, string[] nodeRefs, string[] constructionStageRefs, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Restraint = restraint;
      this.NodeRefs = nodeRefs.ToList();
      this.ConstructionStageRefs = constructionStageRefs.ToList();
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

  public partial class StructuralRigidConstraints
  {
    public StructuralRigidConstraints() { }

    public StructuralRigidConstraints(StructuralVectorBoolSix constraint, string[] nodeRefs, string masterNodeRef, string[] constructionStageRefs, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.ApplicationId = applicationId;
      this.Constraint = constraint;
      this.NodeRefs = nodeRefs.ToList();
      this.MasterNodeRef = masterNodeRef;
      this.ConstructionStageRefs = constructionStageRefs.ToList();

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralNodalInfluenceEffect
  {
    public StructuralNodalInfluenceEffect() { }

    public StructuralNodalInfluenceEffect(StructuralInfluenceEffectType effectType, string nodeRef, double factor, StructuralAxis axis, StructuralVectorBoolSix directions, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.ApplicationId = applicationId;
      this.EffectType = effectType;
      this.NodeRef = nodeRef;
      this.Factor = factor;
      this.Axis = axis;
      this.Directions = directions;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DInfluenceEffect
  {
    public Structural1DInfluenceEffect() { }

    public Structural1DInfluenceEffect(StructuralInfluenceEffectType effectType, string elementRef, double position, double factor, StructuralVectorBoolSix directions, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.ApplicationId = applicationId;
      this.EffectType = effectType;
      this.ElementRef = elementRef;
      this.Position = position;
      this.Factor = factor;
      this.Directions = directions;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralReferenceLine
  {
    public StructuralReferenceLine() { }

    public StructuralReferenceLine(double[] value, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.ApplicationId = applicationId;
      this.Value = value.ToList();

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (var i = 0; i < Value.Count(); i++)
      {
        Value[i] *= factor;
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }
}
