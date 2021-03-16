using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using SpeckleCoreGeometryClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeckleStructuralClasses
{
  public partial class StructuralVectorThree
  {
    public StructuralVectorThree() { }

    public StructuralVectorThree(IEnumerable<double> value, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public StructuralVectorThree(double x, double y, double z, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = new List<double>(new double[] { x, y, z });
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public void Normalise()
    {
      if (this.Value != null || this.Value.Count() == 3)
      {
        var currLength = Math.Sqrt(Math.Pow(this.Value[0], 2) + Math.Pow(this.Value[1], 2) + Math.Pow(this.Value[2], 2));
        this.Value[0] /= currLength;
        this.Value[1] /= currLength;
        this.Value[2] /= currLength;
      }
    }

    public override void Scale(double factor)
    {
      if (this.Value != null && this.Value.Count() > 0)
      {
        for (var i = 0; i < this.Value.Count(); i++)
        {
          this.Value[i] *= factor;
        }
      }
      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }

    public void TransformOntoAxis(StructuralAxis axis)
    {
      double x = 0, y = 0, z = 0;

      x += axis.Xdir.Value[0] * Value[0];
      y += axis.Xdir.Value[1] * Value[0];
      z += axis.Xdir.Value[2] * Value[0];

      x += axis.Ydir.Value[0] * Value[1];
      y += axis.Ydir.Value[1] * Value[1];
      z += axis.Ydir.Value[2] * Value[1];

      x += axis.Normal.Value[0] * Value[2];
      y += axis.Normal.Value[1] * Value[2];
      z += axis.Normal.Value[2] * Value[2];

      this.Value = new List<double>(new double[] { x, y, z });
      GenerateHash();
    }
  }

  public partial class StructuralVectorBoolThree
  {
    public StructuralVectorBoolThree() { }

    public StructuralVectorBoolThree(IEnumerable<bool> value, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public StructuralVectorBoolThree(bool x, bool y, bool z, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = new List<bool>(new bool[] { x, y, z });
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      return;
    }
  }

  public partial class StructuralVectorSix
  {
    public StructuralVectorSix() { }

    public StructuralVectorSix(IEnumerable<double> value, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public StructuralVectorSix(double x, double y, double z, double xx, double yy, double zz, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = new List<double>(new double[] { x, y, z, xx, yy, zz });
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (this.Value != null)
      {
        for (var i = 0; i < this.Value.Count(); i++)
          this.Value[i] *= factor;
      }

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }

    public void TransformOntoAxis(StructuralAxis axis)
    {
      double x = 0, y = 0, z = 0, xx = 0, yy = 0, zz = 0;

      x += axis.Xdir.Value[0] * Value[0];
      y += axis.Xdir.Value[1] * Value[0];
      z += axis.Xdir.Value[2] * Value[0];
      xx += axis.Xdir.Value[0] * Value[0];
      yy += axis.Xdir.Value[1] * Value[0];
      zz += axis.Xdir.Value[2] * Value[0];

      x += axis.Ydir.Value[0] * Value[1];
      y += axis.Ydir.Value[1] * Value[1];
      z += axis.Ydir.Value[2] * Value[1];
      xx += axis.Ydir.Value[0] * Value[0];
      yy += axis.Ydir.Value[1] * Value[0];
      zz += axis.Ydir.Value[2] * Value[0];

      x += axis.Normal.Value[0] * Value[2];
      y += axis.Normal.Value[1] * Value[2];
      z += axis.Normal.Value[2] * Value[2];
      xx += axis.Normal.Value[0] * Value[0];
      yy += axis.Normal.Value[1] * Value[0];
      zz += axis.Normal.Value[2] * Value[0];

      this.Value = new List<double>(new double[] { x, y, z, xx, yy, zz });
      GenerateHash();
    }
  }

  public partial class StructuralVectorBoolSix
  {
    public StructuralVectorBoolSix() { }

    public StructuralVectorBoolSix(IEnumerable<bool> value, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = value.ToList();
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public StructuralVectorBoolSix(bool x, bool y, bool z, bool xx, bool yy, bool zz, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Value = new List<bool>(new bool[] { x, y, z, xx, yy, zz });
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      return;
    }
  }

  public partial class StructuralAxis
  {
    public StructuralAxis() { }

    public StructuralAxis(StructuralVectorThree xdir, StructuralVectorThree ydir, StructuralVectorThree normal, string applicationId = null, Dictionary<string, object> properties = null)
    {
      //this.Origin = new SpecklePoint(0, 0, 0);
      this.Normal = normal;
      this.Xdir = xdir;
      this.Ydir = ydir;
      this.ApplicationId = applicationId;
      if (properties != null)
      {
        this.Properties = properties;
      }

      GenerateHash();
    }

    public StructuralAxis(StructuralVectorThree xdir, StructuralVectorThree ydir, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Normal = new StructuralVectorThree(new double[]
      {
                xdir.Value[1] * ydir.Value[2] - xdir.Value[2] * ydir.Value[1],
                xdir.Value[2] * ydir.Value[0] - xdir.Value[0] * ydir.Value[2],
                xdir.Value[0] * ydir.Value[1] - xdir.Value[1] * ydir.Value[0],
      });
      this.Xdir = xdir;
      this.Ydir = ydir;
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
      //this.Normal.Scale(factor);
      //this.Xdir.Scale(factor);
      //this.Ydir.Scale(factor);

      Helper.ScaleProperties(Properties, factor);
      this.GenerateHash();
    }

    public void Normalize()
    {
      double mag;

      // X
      mag = Math.Sqrt(Xdir.Value.Sum(x => x * x));
      Xdir.Value[0] /= mag;
      Xdir.Value[1] /= mag;
      Xdir.Value[2] /= mag;

      // Y
      mag = Math.Sqrt(Ydir.Value.Sum(y => y * y));
      Ydir.Value[0] /= mag;
      Ydir.Value[1] /= mag;
      Ydir.Value[2] /= mag;

      // Normal
      mag = Math.Sqrt(Normal.Value.Sum(n => n * n));
      Normal.Value[0] /= mag;
      Normal.Value[1] /= mag;
      Normal.Value[2] /= mag;
    }

    public void RotateAxisAboutX(double angle)
    {
      if (angle == 0) return;

      var unitV = (new Vector3D(Xdir.Value[0], Xdir.Value[1], Xdir.Value[2])).Normalize();
      var rotationMatrix = RotationMatrix(unitV, angle);

      var Y = (new Vector3D(Ydir.Value[0], Ydir.Value[1], Ydir.Value[2])).TransformBy(rotationMatrix);
      var Z = (new Vector3D(Normal.Value[0], Normal.Value[1], Normal.Value[2])).TransformBy(rotationMatrix);

      Ydir = new SpeckleVector(Y.X, Y.Y, Y.Z);
      Normal = new SpeckleVector(Z.X, Z.Y, Z.Z);
    }

    public void RotateAxisAboutY(double angle)
    {
      if (angle == 0) return;

      var unitV = (new Vector3D(Ydir.Value[0], Ydir.Value[1], Ydir.Value[2])).Normalize();
      var rotationMatrix = RotationMatrix(unitV, angle);

      var X = (new Vector3D(Xdir.Value[0], Xdir.Value[1], Xdir.Value[2])).TransformBy(rotationMatrix);
      var Z = (new Vector3D(Normal.Value[0], Normal.Value[1], Normal.Value[2])).TransformBy(rotationMatrix);

      Xdir = new SpeckleVector(X.X, X.Y, X.Z);
      Normal = new SpeckleVector(Z.X, Z.Y, Z.Z);
    }

    public void RotateAxisAboutZ(double angle)
    {
      if (angle == 0) return;

      var unitV = (new Vector3D(Normal.Value[0], Normal.Value[1], Normal.Value[2])).Normalize();
      var rotationMatrix = RotationMatrix(unitV, angle);

      var X = (new Vector3D(Xdir.Value[0], Xdir.Value[1], Xdir.Value[2])).TransformBy(rotationMatrix);
      var Y = (new Vector3D(Ydir.Value[0], Ydir.Value[1], Ydir.Value[2])).TransformBy(rotationMatrix);

      Xdir = new SpeckleVector(X.X, X.Y, X.Z);
      Ydir = new SpeckleVector(Y.X, Y.Y, Y.Z);
    }

    private static Matrix<double> RotationMatrix(UnitVector3D zUnitVector, double angle)
    {
      return Matrix3D.RotationAroundArbitraryVector(zUnitVector, Angle.FromRadians(angle));
      /*
      var cos = Math.Cos(angle);
      var sin = Math.Sin(angle);

      // TRANSPOSED MATRIX TO ACCOMODATE MULTIPLY FUNCTION
      return new Matrix3D(
          cos + Math.Pow(zUnitVector.X, 2) * (1 - cos),
          zUnitVector.Y * zUnitVector.X * (1 - cos) + zUnitVector.Z * sin,
          zUnitVector.Z * zUnitVector.X * (1 - cos) - zUnitVector.Y * sin,
          0,

          zUnitVector.X * zUnitVector.Y * (1 - cos) - zUnitVector.Z * sin,
          cos + Math.Pow(zUnitVector.Y, 2) * (1 - cos),
          zUnitVector.Z * zUnitVector.Y * (1 - cos) + zUnitVector.X * sin,
          0,

          zUnitVector.X * zUnitVector.Z * (1 - cos) + zUnitVector.Y * sin,
          zUnitVector.Y * zUnitVector.Z * (1 - cos) - zUnitVector.X * sin,
          cos + Math.Pow(zUnitVector.Z, 2) * (1 - cos),
          0,

          0, 0, 0, 1
      );
      */
    }
  }
}
