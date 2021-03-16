using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public partial class StructuralLoadCase
  {
    public StructuralLoadCase() { }

    public StructuralLoadCase(StructuralLoadCaseType caseType, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.CaseType = caseType;
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

  public partial class StructuralLoadTask
  {
    public StructuralLoadTask() { }

    public StructuralLoadTask(StructuralLoadTaskType taskType, string[] loadCaseRefs, double[] loadFactors, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.TaskType = taskType;
      this.LoadCaseRefs = loadCaseRefs.ToList();
      this.LoadFactors = loadFactors.ToList();
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

  public partial class StructuralLoadTaskBuckling
  {
    public StructuralLoadTaskBuckling() { }

    public StructuralLoadTaskBuckling(int numModes, int maxNumIterations, string resultCaseRef, string applicationId = null, Dictionary<string, object> properties = null, string stageDefinitionRef = null)
    {
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.NumModes = numModes;
      this.MaxNumIterations = maxNumIterations;
      this.ResultCaseRef = resultCaseRef;
      this.StageDefinitionRef = stageDefinitionRef;
      GenerateHash();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralLoadCombo
  {
    public StructuralLoadCombo() { }

    public StructuralLoadCombo(StructuralLoadComboType comboType, string[] loadTaskRefs, double[] loadTaskFactors, string[] loadComboRefs, double[] loadComboFactors, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.ComboType = comboType;
      this.LoadTaskRefs = loadTaskRefs.ToList();
      this.LoadTaskFactors = loadTaskFactors.ToList();
      this.LoadComboRefs = loadComboRefs.ToList();
      this.LoadComboFactors = loadComboFactors.ToList();
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

  public partial class StructuralGravityLoading
  {
    public StructuralGravityLoading() { }

    public StructuralGravityLoading(StructuralVectorThree gravityFactors, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.GravityFactors = gravityFactors;
      this.LoadCaseRef = loadCaseRef;
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
  public partial class StructuralLoadPlane
  {
    public StructuralLoadPlane() { }

    public StructuralLoadPlane(StructuralAxis loadPlaneAxis, int elementDimension, double tolerance, int span, double spanAngle, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (loadPlaneAxis != null)
      {
        this.Axis.Origin = loadPlaneAxis.Origin;
        this.Axis.Xdir = loadPlaneAxis.Xdir;
        this.Axis.Ydir = loadPlaneAxis.Ydir;
        this.Axis.Normal = loadPlaneAxis.Normal;
      }
      this.ElementDimension = elementDimension;
      this.Tolerance = tolerance;
      this.Span = span;
      this.SpanAngle = spanAngle;
      this.ApplicationId = applicationId;

      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      this.Tolerance *= factor;

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class StructuralStorey
  {
    public StructuralStorey() { }

    public StructuralStorey(StructuralAxis axis, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (axis != null)
      {
        this.Axis.Origin = axis.Origin;
        this.Axis.Xdir = axis.Xdir;
        this.Axis.Ydir = axis.Ydir;
        this.Axis.Normal = axis.Normal;
      }
      this.ApplicationId = applicationId;

      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Origin != null)
      {
        this.Origin.Scale(factor);
      }
      this.Elevation *= factor;
      this.ToleranceAbove *= factor;
      this.ToleranceBelow *= factor;
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  //[Serializable]
  public partial class Structural0DLoad
  {
    public Structural0DLoad() { }

    public Structural0DLoad(StructuralVectorSix loading, string[] nodeRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.NodeRefs = nodeRefs == null ? null : nodeRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
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

  public partial class Structural0DLoadPoint
  {
    public Structural0DLoadPoint() { }

    public Structural0DLoadPoint(double[] value, string loadPlaneRef, StructuralVectorSix loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.LoadPlaneRef = loadPlaneRef;
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (Value != null)
      {
        for (var i = 0; i < Value.Count(); i++)
        {
          Value[i] *= factor;
        }
      }

      if (Loading.Value != null)
      {
        for (var i = 0; i < Loading.Value.Count(); i++)
        {
          Loading.Value[i] *= factor;
        }
      }

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DLoad
  {
    public Structural1DLoad() { }

    public Structural1DLoad(StructuralVectorSix loading, string[] elementRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.ElementRefs = elementRefs == null ? null : elementRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
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

  public partial class Structural1DLoadLine
  {
    public Structural1DLoadLine() { }

    public Structural1DLoadLine(double[] value, StructuralVectorSix loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.Value = value.ToList();
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;

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

      Helper.ScaleProperties(Properties, factor);

      if (Loading != null && Loading.Value != null)
      {
        for (var i = 0; i < Loading.Value.Count(); i++)
        {
          Loading.Value[i] *= factor;
        }
      }

      if (LoadingEnd != null && LoadingEnd.Value != null)
      {
        for (var i = 0; i < LoadingEnd.Value.Count(); i++)
        {
          LoadingEnd.Value[i] *= factor;
        }
      }

      this.GenerateHash();
    }
  }

  public partial class Structural2DLoad
  {
    public Structural2DLoad() { }

    public Structural2DLoad(StructuralVectorThree loading, string[] elementRefs, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Loading = loading;
      this.ElementRefs = elementRefs == null ? null : elementRefs.ToList();
      this.LoadCaseRef = loadCaseRef;
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

  public partial class Structural2DLoadPanel
  {
    public Structural2DLoadPanel() { }

    public Structural2DLoadPanel(double[] value, StructuralVectorThree loading, string loadCaseRef, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        this.Properties = properties;
      }
      this.Value = value.ToList();
      this.Loading = loading;
      this.LoadCaseRef = loadCaseRef;
      this.ApplicationId = applicationId;

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

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DThermalLoad
  {
    public Structural2DThermalLoad() { }

    public Structural2DThermalLoad(double topTemperature, double bottomTemperature, string loadCaseRef, string[] elementRefs, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.TopTemperature = topTemperature;
      this.BottomTemperature = bottomTemperature;
      this.LoadCaseRef = loadCaseRef;
      this.ElementRefs = elementRefs.ToList();
    }

    public override void Scale(double factor)
    {
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }
  }
}
