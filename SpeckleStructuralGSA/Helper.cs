using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using MathNet.Spatial.Units;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SpeckleStructuralGSA
{
  /// <summary>
  /// Static class containing helper functions used throughout SpeckleGSA
  /// </summary>
  public static class Helper
  {
    #region Reflection
    public static IEnumerable<Type> GetEnumerableOfType<T>() where T : class
    {
      return Assembly.GetAssembly(typeof(T)).GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)));
    }
    #endregion

    #region Math


    /// <summary>
    /// Calculates the mean of two numbers.
    /// </summary>
    /// <param name="n1">First number</param>
    /// <param name="n2">Second number</param>
    /// <returns>Mean</returns>
    public static double Mean(double n1, double n2)
    {
      return (n1 + n2) * 0.5;
    }

    /// <summary>
    /// Generates a rotation matrix about a given Z unit vector.
    /// </summary>
    /// <param name="zUnitVector">Z unit vector</param>
    /// <param name="angle">Angle of rotation in radians</param>
    /// <returns>Rotation matrix</returns>
    public static Matrix<double> RotationMatrix(UnitVector3D zUnitVector, double angle)
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
    #endregion

    #region Lists

    /// <summary>
    /// Extracts and return the group indicies in the list.
    /// </summary>
    /// <param name="list">List</param>
    /// <returns>Array of group indices</returns>
    public static int[] GetGroupsFromGSAList(string list)
    {
      var pieces = list.ListSplit(" ");

      var groups = new List<int>();

      foreach (var p in pieces)
      {
        if (p.Length > 0 && p[0] == 'G')
        {
          groups.Add(Convert.ToInt32(p.Substring(1)));
        }
      }

      return groups.ToArray();
    }
    #endregion

    #region Unit Conversion
    /// <summary>
    /// Converts value from one unit to another.
    /// </summary>
    /// <param name="value">Value to scale</param>
    /// <param name="originalDimension">Original unit</param>
    /// <param name="targetDimension">Target unit</param>
    /// <returns></returns>
    public static double ConvertUnit(this double value, string originalDimension, string targetDimension)
    {
      if (originalDimension == targetDimension)
        return value;

      if (targetDimension == "m")
      {
        switch (originalDimension)
        {
          case "mm":
            return value / 1000;
          case "cm":
            return value / 100;
          case "ft":
            return value / 3.281;
          case "in":
            return value / 39.37;
          default:
            return value;
        }
      }
      else if (originalDimension == "m")
      {
        switch (targetDimension)
        {
          case "mm":
            return value * 1000;
          case "cm":
            return value * 100;
          case "ft":
            return value * 3.281;
          case "in":
            return value * 39.37;
          default:
            return value;
        }
      }
      else
        return value.ConvertUnit(originalDimension, "m").ConvertUnit("m", targetDimension);
    }

    /// <summary>
    /// Converts short unit name to long unit name
    /// </summary>
    /// <param name="unit">Short unit name</param>
    /// <returns>Long unit name</returns>
    public static string LongUnitName(this string unit)
    {
      switch (unit.ToLower())
      {
        case "m":
          return "meters";
        case "mm":
          return "millimeters";
        case "cm":
          return "centimeters";
        case "ft":
          return "feet";
        case "in":
          return "inches";
        default:
          return unit;
      }
    }

    /// <summary>
    /// Converts long unit name to short unit name
    /// </summary>
    /// <param name="unit">Long unit name</param>
    /// <returns>Short unit name</returns>
    public static string ShortUnitName(this string unit)
    {
      switch (unit.ToLower())
      {
        case "meters":
          return "m";
        case "millimeters":
          return "mm";
        case "centimeters":
          return "cm";
        case "feet":
          return "ft";
        case "inches":
          return "in";
        default:
          return unit;
      }
    }
    #endregion

    #region Comparison
    /// <summary>
    /// Checks if the string contains only digits.
    /// </summary>
    /// <param name="str">String</param>
    /// <returns>True if string contails only digits</returns>
    public static bool IsDigits(this string str)
    {
      foreach (var c in str)
      {
        if (c < '0' || c > '9')
        {
          return false;
        }
      }
      return true;
    }
    #endregion

    #region Miscellaneous
    /// <summary>
    /// Parses GSA polyline description. Projects all points onto XY plane.
    /// </summary>
    /// <param name="desc">GSA polyline description</param>
    /// <returns>Flat array of coordinates</returns>
    public static double[] ParsePolylineDesc(string desc)
    {
      var coordinates = new List<double>();

      foreach (Match m in Regex.Matches(desc, @"(?<=\()(.+?)(?=\))"))
      {
        var pieces = m.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        try
        {
          coordinates.AddRange(pieces.Take(2).Select(p => Convert.ToDouble(p)));
          coordinates.Add(0);
        }
        catch { }
      }
      return coordinates.ToArray();
    }

    /// <summary>
    /// Seperates the load description into tuples of the case/task/combo identifier and their factors.
    /// </summary>
    /// <param name="list">Load description.</param>
    /// <param name="currentMultiplier">Factor to multiply the entire list by.</param>
    /// <returns></returns>
    public static List<Tuple<string, double>> ParseLoadDescription(string list, double currentMultiplier = 1)
    {
      var ret = new List<Tuple<string, double>>();

      list = list.Replace(" ", "");

      double multiplier = 1;
      var negative = false;

      for (var pos = 0; pos < list.Count(); pos++)
      {
        var currChar = list[pos];

        if (currChar >= '0' && currChar <= '9')
        {
          var mult = "";
          mult += currChar.ToString();

          pos++;
          while (pos < list.Count() && ((list[pos] >= '0' && list[pos] <= '9') || list[pos] == '.'))
            mult += list[pos++].ToString();
          pos--;

          multiplier = Convert.ToDouble(mult);
        }
        else if (currChar >= 'A' && currChar <= 'Z')
        {
          var loadDesc = "";
          loadDesc += currChar.ToString();

          pos++;
          while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
            loadDesc += list[pos++].ToString();
          pos--;

          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.Add(new Tuple<string, double>(loadDesc, actualFactor));

          multiplier = 0;
          negative = false;
        }
        else if (currChar == '-')
          negative = !negative;
        else if (currChar == 't')
        {
          if (list[++pos] == 'o')
          {
            var prevDesc = ret.Last();

            var type = prevDesc.Item1[0].ToString();
            var start = Convert.ToInt32(prevDesc.Item1.Substring(1)) + 1;

            var endDesc = "";

            pos++;
            pos++;
            while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
              endDesc += list[pos++].ToString();
            pos--;

            var end = Convert.ToInt32(endDesc);

            for (var i = start; i <= end; i++)
              ret.Add(new Tuple<string, double>(type + i.ToString(), prevDesc.Item2));
          }
        }
        else if (currChar == '(')
        {
          var actualFactor = multiplier == 0 ? 1 : multiplier;
          actualFactor *= currentMultiplier;
          actualFactor = negative ? -1 * actualFactor : actualFactor;

          ret.AddRange(ParseLoadDescription(string.Join("", list.Skip(pos + 1)), actualFactor));

          pos++;
          while (pos < list.Count() && list[pos] != ')')
            pos++;

          multiplier = 0;
          negative = false;
        }
        else if (currChar == ')')
          return ret;
      }

      return ret;
    }

    
    #endregion

    #region MovedFromInterfacer

    public static string GenerateSID(SpeckleObject obj)
    {
      return Initialiser.AppResources.Proxy.FormatApplicationIdSidTag(obj.ApplicationId);
    }

    public static string GenerateSID(string applicationId)
    {
      return Initialiser.AppResources.Proxy.FormatApplicationIdSidTag(applicationId);
    }

    public static void SetAxis(StructuralAxis axis, out int index, out string gwa, string name = "")
    {
      var gwaAxisName = name ?? "";
      index = 0;
      gwa = "";
      double[] globalOrigin = { 0, 0, 0 };
      double[] globalXdir = { 1, 0, 0 };
      double[] globalYdir = { 0, 1, 0 };
      double[] globalZdir = { 0, 0, 1 };

      if (axis == null || (axis.Xdir == null && axis.Ydir == null)
        || (
          axis.Xdir.Value.SequenceEqual(globalXdir) &&
          axis.Ydir.Value.SequenceEqual(globalYdir) &&
          axis.Normal.Value.SequenceEqual(globalZdir)))
      {
        return;
      }
      var res = Initialiser.AppResources.Cache.ResolveIndex("AXIS");

      var originCoords = (axis.Origin == null || axis.Origin.Value == null) ? new List<double> { 0, 0, 0 } : axis.Origin.Value;

      var ls = new List<string>
      {
        "SET",
        "AXIS.1",
        res.ToString(),
        gwaAxisName,
        "CART",

        originCoords[0].ToString(),
        originCoords[1].ToString(),
        originCoords[2].ToString(),

        axis.Xdir.Value[0].ToString(),
        axis.Xdir.Value[1].ToString(),
        axis.Xdir.Value[2].ToString(),

        axis.Ydir.Value[0].ToString(),
        axis.Ydir.Value[1].ToString(),
        axis.Ydir.Value[2].ToString()
      };

      gwa = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);

      index = res;
    }
    public static void SetAxis(StructuralAxis axis, int Index, out string gwa, string name = "")
    {
      var gwaAxisName = name ?? "";

      gwa = "";
      double[] globalOrigin = { 0, 0, 0 };
      double[] globalXdir = { 1, 0, 0 };
      double[] globalYdir = { 0, 1, 0 };
      double[] globalZdir = { 0, 0, 1 };

      if (axis == null
        || (
          axis.Xdir.Value.SequenceEqual(globalXdir) &&
          axis.Ydir.Value.SequenceEqual(globalYdir) &&
          axis.Normal.Value.SequenceEqual(globalZdir)))
      {
        return;
      }

      var ls = new List<string>
      {
        "SET",
        "AXIS.1",
        Index.ToString(),
        gwaAxisName,
        "CART",

        axis.Origin.Value[0].ToString(),
        axis.Origin.Value[1].ToString(),
        axis.Origin.Value[2].ToString(),

        axis.Xdir.Value[0].ToString(),
        axis.Xdir.Value[1].ToString(),
        axis.Xdir.Value[2].ToString(),

        axis.Ydir.Value[0].ToString(),
        axis.Ydir.Value[1].ToString(),
        axis.Ydir.Value[2].ToString()
      };

      gwa = string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls);

    }

    public static void SetAxis(SpeckleVector xVector, SpeckleVector xyVector, SpecklePoint origin, int Index, out string gwaCommand, string name = "")
    {
      gwaCommand = "";

      var gwaCommands = new List<string>();

      var ls = new List<string>()
        {
          "SET",
          "AXIS.1",
          Index.ToString(),
          name ?? "",
          "CART",

          origin.Value[0].ToString(),
          origin.Value[1].ToString(),
          origin.Value[2].ToString(),

          xVector.Value[0].ToString(),
          xVector.Value[1].ToString(),
          xVector.Value[2].ToString(),

          xyVector.Value[0].ToString(),
          xyVector.Value[1].ToString(),
          xyVector.Value[2].ToString(),
        };

      gwaCommand = (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }
    public static void SetAxis(SpeckleVector xVector, SpeckleVector xyVector, SpecklePoint origin, out int index, out string gwaCommand, string name = "")
    {
      gwaCommand = "";
      index = Initialiser.AppResources.Cache.ResolveIndex("AXIS");

      var gwaCommands = new List<string>();

      var ls = new List<string>()
        {
          "SET",
          "AXIS.1",
          index.ToString(),
          name ?? "",
          "CART",

          origin.Value[0].ToString(),
          origin.Value[1].ToString(),
          origin.Value[2].ToString(),

          xVector.Value[0].ToString(),
          xVector.Value[1].ToString(),
          xVector.Value[2].ToString(),

          xyVector.Value[0].ToString(),
          xyVector.Value[1].ToString(),
          xyVector.Value[2].ToString(),
        };

      gwaCommand = (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }

    /// <summary>
    /// Calculates the local axis of a 1D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="rotationAngle">Angle of rotation from default axis</param>
    /// <param name="orientationNode">Node to orient axis to</param>
    /// <returns>Axis</returns>
    public static StructuralAxis Parse1DAxis(double[] coor, double rotationAngle = 0, double[] orientationNode = null)
    {
      UnitVector3D x, y, z;

      x = (new Vector3D(coor[3] - coor[0], coor[4] - coor[1], coor[5] - coor[2])).Normalize();

      if (orientationNode == null)
      {
        if (x.X == 0 && x.Y == 0)
        {
          //Column
          y = (new Vector3D(0, 1, 0)).Normalize();
          z = x.CrossProduct(y);
        }
        else
        {
          //Non-Vertical
          var Z = new Vector3D(0, 0, 1);
          y = Z.CrossProduct(x).Normalize();
          z = x.CrossProduct(y);
        }
      }
      else
      {
        var Yp = (new Vector3D(orientationNode[0], orientationNode[1], orientationNode[2])).Normalize();
        z = x.CrossProduct(Yp);
        y = z.CrossProduct(x);
      }

      //Rotation
      var rotMat = Helper.RotationMatrix(x, rotationAngle.ToRadians());
      y = y.TransformBy(rotMat).Normalize();
      z = z.TransformBy(rotMat).Normalize();

      return new StructuralAxis(
          new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
          new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
          new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
      );
    }

    /// <summary>
    /// Maps a flat array of coordinates from the global Cartesian coordinate system to a local coordinate system.
    /// </summary>
    /// <param name="values">Flat array of coordinates</param>
    /// <param name="axis">Local coordinate system</param>
    /// <returns>Transformed array of coordinates</returns>
    public static double[] MapPointsGlobal2Local(IEnumerable<double> values, StructuralAxis axis)
    {
      var newVals = new List<double>();

      for (var i = 0; i < values.Count(); i += 3)
      {
        var coor = values.Skip(i).Take(3).ToList();
        var translated = coor.MapGlobal2Local(axis);
        newVals.AddRange(translated);
      }

      return newVals.ToArray();
    }

    public static double[] MapGlobal2Local(this IEnumerable<double> globalCoords, StructuralAxis axis)
    {
      var coords = globalCoords.ToArray();
      if (axis == null)
      {
        return coords;
      }
      var cartesianDifference = (axis.Origin == null || axis.Origin.Value == null || axis.Origin.Value.Count != 3)
        ? coords
        : new double[] { coords[0] - axis.Origin.Value[0], coords[1] - axis.Origin.Value[1], coords[2] - axis.Origin.Value[2] };

      var A = Matrix<double>.Build.DenseOfArray(new double[,] {
        { axis.Xdir.Value[0] , axis.Xdir.Value[1], axis.Xdir.Value[2] },
        { axis.Ydir.Value[0] , axis.Ydir.Value[1], axis.Ydir.Value[2] },
        { axis.Normal.Value[0] , axis.Normal.Value[1], axis.Normal.Value[2] }
      });
      A = A.Transpose();
      var b = Vector<double>.Build.Dense(cartesianDifference);
      var coefficients = A.Solve(b);

      return coefficients.Select(c => Math.Round(c, 10)).ToArray();
    }

    /// <summary>
    /// Maps a flat array of coordinates from a local coordinate system to the global Cartesian coordinate system.
    /// </summary>
    /// <param name="values">Flat array of coordinates</param>
    /// <param name="axis">Local coordinate system</param>
    /// <returns>Transformed array of coordinates</returns>
    public static double[] MapPointsLocal2Global(IEnumerable<double> values, StructuralAxis axis)
    {
      var newVals = new List<double>();

      for (var i = 0; i < values.Count(); i += 3)
      {
        var coor = values.Skip(i).Take(3).ToList();

        double x = 0;
        double y = 0;
        double z = 0;

        x += axis.Xdir.Value[0] * coor[0];
        y += axis.Xdir.Value[1] * coor[0];
        z += axis.Xdir.Value[2] * coor[0];

        x += axis.Ydir.Value[0] * coor[1];
        y += axis.Ydir.Value[1] * coor[1];
        z += axis.Ydir.Value[2] * coor[1];

        x += axis.Normal.Value[0] * coor[2];
        y += axis.Normal.Value[1] * coor[2];
        z += axis.Normal.Value[2] * coor[2];

        if (axis.Origin != null && axis.Origin.Value != null && axis.Origin.Value.Count == 3)
        {
          x += axis.Origin.Value[0];
          y += axis.Origin.Value[1];
          z += axis.Origin.Value[2];
        }
        newVals.Add(x);
        newVals.Add(y);
        newVals.Add(z);
      }

      return newVals.ToArray();
    }

    /// <summary>
    /// Calculates the local axis of a 1D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="zAxis">Z axis of the 1D entity</param>
    /// <returns>Axis</returns>
    public static StructuralAxis LocalAxisEntity1D(double[] coor, StructuralVectorThree zAxis)
    {
      var axisX = new Vector3D(coor[3] - coor[0], coor[4] - coor[1], coor[5] - coor[2]);
      var axisZ = new Vector3D(zAxis.Value[0], zAxis.Value[1], zAxis.Value[2]);
      var axisY = axisZ.CrossProduct(axisX);

      var axis = new StructuralAxis(
          new StructuralVectorThree(new double[] { axisX.X, axisX.Y, axisX.Z }),
          new StructuralVectorThree(new double[] { axisY.X, axisY.Y, axisY.Z }),
          new StructuralVectorThree(new double[] { axisZ.X, axisZ.Y, axisZ.Z })
      );
      axis.Normalize();
      return axis;
    }

    /// <summary>
    /// Calculates the local axis of a 2D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="rotationAngle">Angle of rotation from default axis</param>
    /// <param name="isLocalAxis">Is axis calculated from local coordinates?</param>
    /// <returns>Axis</returns>
    public static StructuralAxis Parse2DAxis(double[] fullCoords, double rotationAngle = 0, bool isLocalAxis = false)
    {
      UnitVector3D x;
      UnitVector3D y;
      UnitVector3D z;

      var nodes = new List<Vector3D>();

      var coor = fullCoords.Essential();

      for (var i = 0; i < coor.Length; i += 3)
      {
        nodes.Add(new Vector3D(coor[i], coor[i + 1], coor[i + 2]));
      }

      if (isLocalAxis)
      {
        if (nodes.Count == 3)
        {
          x = (nodes[1] - nodes[0]).Normalize();
          z = x.CrossProduct(nodes[2] - nodes[0]).Normalize();
          y = z.CrossProduct(x);
        }
        else
        {
          // Default to QUAD method
          x = (nodes[2] - nodes[0]).Normalize();
          z = x.CrossProduct(nodes[3] - nodes[1]).Normalize();
          y = z.CrossProduct(x);
        }
      }
      else
      {
        x = (nodes[1] - nodes[0]).Normalize();
        //The z is the normal to the plane of the coordinates
        z = x.CrossProduct(nodes[2] - nodes[0]).Normalize();

        if ((x - (x.DotProduct(z) * z)).Length == 0)
        {
          //Z is parallel to z, which happens when nodes[2] is in line with [0] and [1]
          x = (new Vector3D(0, z.X > 0 ? -1 : 1, 0)).Normalize();
        }
        else if (!z.IsParallelTo(UnitVector3D.XAxis))
        {
          x = UnitVector3D.XAxis;
          //This ensures that the x vector is right-angles to the z vector
          x = (x - (x.DotProduct(z) * z)).Normalize();
        }

        y = z.CrossProduct(x);
      }

      //Rotation
      var rotMat = Helper.RotationMatrix(z, rotationAngle * (Math.PI / 180));
      x = x.TransformBy(rotMat).Normalize();
      y = y.TransformBy(rotMat).Normalize();

      return new StructuralAxis(
          new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
          new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
          new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
      );
    }

    public static StructuralAxis Global = new StructuralAxis(
                                              new StructuralVectorThree(new double[] { 1, 0, 0 }),
                                              new StructuralVectorThree(new double[] { 0, 1, 0 }),
                                              new StructuralVectorThree(new double[] { 0, 0, 1 })
                                          );

    public static StructuralAxis XElevation = new StructuralAxis(
                                                new StructuralVectorThree(new double[] { 0, -1, 0 }),
                                                new StructuralVectorThree(new double[] { 0, 0, 1 }),
                                                new StructuralVectorThree(new double[] { -1, 0, 0 })
                                            );

    public static StructuralAxis YElevation = new StructuralAxis(
                                                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                                                new StructuralVectorThree(new double[] { 0, 0, 1 }),
                                                new StructuralVectorThree(new double[] { 0, -1, 0 })
                                            );

    public static StructuralAxis Vertical = new StructuralAxis(
                                                new StructuralVectorThree(new double[] { 0, 0, 1 }),
                                                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                                                new StructuralVectorThree(new double[] { 0, 1, 0 })
                                            );

    /// <summary>
    /// Calculates the local axis of a point from a GSA node axis.
    /// </summary>
    /// <param name="axis">ID of GSA node axis</param>
    /// <param name="gwaRecord">GWA record of AXIS if used</param>
    /// <param name="evalAtCoor">Coordinates to evaluate axis at</param>
    /// <returns>Axis</returns>
    public static StructuralAxis Parse0DAxis(int axis, out string gwaRecord, double[] evalAtCoor = null)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      gwaRecord = null;

      switch (axis)
      {
        case 0:
          // Global
          return Global;
        case -11:
          // X elevation
          return XElevation;
        case -12:
          // Y elevation
          return YElevation;
        case -14:
          // Vertical
          return Vertical;
        case -13:
          // Global cylindrical
          x = new Vector3D(evalAtCoor[0], evalAtCoor[1], 0);
          x.Normalize();
          z = new Vector3D(0, 0, 1);
          y = z.CrossProduct(x);

          return new StructuralAxis(
              new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
              new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
              new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
          );
        default:
          var res = Initialiser.AppResources.Cache.GetGwa("AXIS", axis).First();
          gwaRecord = res;

          var pieces = res.Split(Initialiser.AppResources.Proxy.GwaDelimiter);
          if (pieces.Length < 13)
          {
            return new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })
            );
          }
          var origin = new Vector3D(Convert.ToDouble(pieces[4]), Convert.ToDouble(pieces[5]), Convert.ToDouble(pieces[6]));

          var X = new Vector3D(Convert.ToDouble(pieces[7]), Convert.ToDouble(pieces[8]), Convert.ToDouble(pieces[9]));
          X.Normalize();


          var Yp = new Vector3D(Convert.ToDouble(pieces[10]), Convert.ToDouble(pieces[11]), Convert.ToDouble(pieces[12]));
          var Z = X.CrossProduct(Yp);
          Z.Normalize();

          var Y = Z.CrossProduct(X);

          var pos = new Vector3D(0, 0, 0);

          if (evalAtCoor == null)
            pieces[3] = "CART";
          else
          {
            pos = new Vector3D(evalAtCoor[0] - origin.X, evalAtCoor[1] - origin.Y, evalAtCoor[2] - origin.Z);
            if (pos.Length == 0)
              pieces[3] = "CART";
          }

          switch (pieces[3])
          {
            case "CART":
              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { X.X, X.Y, X.Z }),
                  new StructuralVectorThree(new double[] { Y.X, Y.Y, Y.Z }),
                  new StructuralVectorThree(new double[] { Z.X, Z.Y, Z.Z })
              );
            case "CYL":
              x = new Vector3D(pos.X, pos.Y, 0);
              x.Normalize();
              z = Z;
              y = Z.CrossProduct(x);
              y.Normalize();

              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
                  new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
                  new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
              );
            case "SPH":
              x = pos;
              x.Normalize();
              z = Z.CrossProduct(x);
              z.Normalize();
              y = z.CrossProduct(x);
              z.Normalize();

              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
                  new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
                  new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
              );
            default:
              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { 1, 0, 0 }),
                  new StructuralVectorThree(new double[] { 0, 1, 0 }),
                  new StructuralVectorThree(new double[] { 0, 0, 1 })
              );
          }
      }
    }

    /// <summary>
    /// Calculates rotation angle of 1D entity to align with axis.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="zAxis">Z axis of entity</param>
    /// <returns>Rotation angle</returns>
    public static double Get1DAngle(double[] coor, StructuralVectorThree zAxis)
    {
      return Get1DAngle(LocalAxisEntity1D(coor, zAxis));
    }

    /// <summary>
    /// Calculates rotation angle of 1D entity to align with axis.
    /// </summary>
    /// <param name="axis">Axis of entity</param>
    /// <returns>Rotation angle</returns>
    public static double Get1DAngle(StructuralAxis axis)
    {
      var axisX = new Vector3D(axis.Xdir.Value[0], axis.Xdir.Value[1], axis.Xdir.Value[2]);
      var axisY = new Vector3D(axis.Ydir.Value[0], axis.Ydir.Value[1], axis.Ydir.Value[2]);
      var axisZ = new Vector3D(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);

      if (axisX.X == 0 && axisX.Y == 0)
      {
        // Column
        var Yglobal = new Vector3D(0, 1, 0);

        var angle = Math.Acos(Yglobal.DotProduct(axisY) / (Yglobal.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) return 0;

        var signVector = Yglobal.CrossProduct(axisY);
        var sign = signVector.DotProduct(axisX);

        return sign >= 0 ? angle : -angle;
      }
      else
      {
        var Zglobal = new Vector3D(0, 0, 1);
        var Y0 = Zglobal.CrossProduct(axisX);
        var angle = Math.Acos(Y0.DotProduct(axisY) / (Y0.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) angle = 0;

        var signVector = Y0.CrossProduct( axisY);
        var sign = signVector.DotProduct(axisX);

        return sign >= 0 ? angle : 360 - angle;
      }
    }

    /// <summary>
    /// Calculates rotation angle of 2D entity to align with axis
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="axis">Axis of entity</param>
    /// <returns>Rotation angle</returns>
    public static double Get2DAngle(double[] coor, StructuralAxis axis)
    {
      var axisX = new Vector3D(axis.Xdir.Value[0], axis.Xdir.Value[1], axis.Xdir.Value[2]);
      var axisY = new Vector3D(axis.Ydir.Value[0], axis.Ydir.Value[1], axis.Ydir.Value[2]);
      var axisZ = new Vector3D(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);

      Vector3D x0;
      Vector3D z0;

      var nodes = new List<Vector3D>();

      for (var i = 0; i < coor.Length; i += 3)
        nodes.Add(new Vector3D(coor[i], coor[i + 1], coor[i + 2]));

      // Get 0 angle axis in GLOBAL coordinates
      x0 = nodes[1] - nodes[0];
      x0.Normalize();
      z0 = x0.CrossProduct(nodes[2] - nodes[0]);
      z0.Normalize();

      x0 = new Vector3D(1, 0, 0);
      x0 = x0 - (x0.DotProduct(z0) * z0);

      if (x0.Length == 0)
        x0 = new Vector3D(0, z0.X > 0 ? -1 : 1, 0);

      x0.Normalize();

      // Find angle
      var angle = Math.Acos(x0.DotProduct(axisX) / (x0.Length * axisX.Length)).ToDegrees();
      if (double.IsNaN(angle)) return 0;

      var signVector = x0.CrossProduct(axisX);
      var sign = signVector.DotProduct(axisZ);

      return sign >= 0 ? angle : -angle;
    }

    public static StructuralLoadTaskType GetLoadTaskType(string taskGwaCommand)
    {
      var taskPieces = taskGwaCommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      var taskType = StructuralLoadTaskType.LinearStatic;

      if (taskPieces[4] == "GSS")
      {
        if (taskPieces[5] == "STATIC")
          taskType = StructuralLoadTaskType.LinearStatic;
        else if (taskPieces[5] == "MODAL")
          taskType = StructuralLoadTaskType.Modal;
      }
      else if (taskPieces[4] == "GSRELAX")
      {
        if (taskPieces[5] == "BUCKLING_NL")
          taskType = StructuralLoadTaskType.NonlinearStatic;
      }

      return taskType;
    }

    public static bool GetElementParentIdFromGwa(string gwa, out int id)
    {
      var pieces = gwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      var dummyIndex = pieces.Count() - 2;
      if (pieces.Length >= 18 && (pieces[dummyIndex] == "" || pieces[dummyIndex] == "DUMMY"))
      {
        return int.TryParse(pieces.Last(), out id);
      }
      id = 0;
      return false;
    }

    public static string FormatApplicationId(string keyword, int index, int parentIndex = 0)
    {
      return FormatApplicationId(keyword, new int[] { index }, parentIndex);
    }

    public static string FormatApplicationId(string keyword, IEnumerable<int> indices, int parentIndex = 0)
    {
      return ("gsa/" + keyword + "-" + string.Join("-", indices) + ((parentIndex > 0) ? "_" + parentIndex : ""));
    }

    public static string GetApplicationId(string keyword, int id)
    {
      //Ensure keyword version is left out
      keyword = keyword.Split('.').First();
      //Fill with SID
      var applicationId = Initialiser.AppResources.Cache.GetApplicationId(keyword, id);
      return (string.IsNullOrEmpty(applicationId)) ? FormatApplicationId(keyword, id) : applicationId;
    }

    public static void GetGridPlaneData(int gridPlaneIndex, out int gridPlaneAxisIndex, out double gridPlaneElevation, out string gwa)
    {
      var gwas = Initialiser.AppResources.Cache.GetGwa("GRID_PLANE", gridPlaneIndex);
      if (gwas == null || gwas.Count() == 0)
      {
        gridPlaneAxisIndex = 0;
        gridPlaneElevation = 0;
        gwa = "";
        return;
      }
      gwa = gwas.First();
      var pieces = gwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      gridPlaneAxisIndex = Convert.ToInt32(pieces[4]);
      gridPlaneElevation = Convert.ToDouble(pieces[5]);
      return;
    }

    public static void GetGridPlaneRef(int gridSurfaceIndex, out int gridPlaneIndex, out string gwa)
    {
      var gwas = Initialiser.AppResources.Cache.GetGwa("GRID_SURFACE", gridSurfaceIndex);
      if (gwas == null || gwas.Count() == 0)
      {
        gridPlaneIndex = 0;
        gwa = "";
        return;
      }
      gwa = gwas.First();
      var pieces = gwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      gridPlaneIndex = Convert.ToInt32(pieces[3]);
    }

    public static void GetPolylineDesc(int polylineIndex, out string desc, out string gwa)
    {
      var gwas = Initialiser.AppResources.Cache.GetGwa("tPOLYLINE", polylineIndex);
      if (gwas == null || gwas.Count() == 0)
      {
        desc = "";
        gwa = "";
        return;
      }
      gwa = gwas.First();

      var pieces = gwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      desc = pieces[6];
    }

    public static StructuralVectorBoolSix RestraintFromCode(string code)
    {
      return new StructuralVectorBoolSix(RestraintBoolArrayFromCode(code));
    }

    public static bool[] RestraintBoolArrayFromCode(string code)
    {
      if (code == "free")
      {
        return new bool[] { false, false, false, false, false, false };
      }
      else if (code == "pin")
      {
        return new bool[] { true, true, true, false, false, false };
      }
      else if (code == "fix")
      {
        return new bool[] { true, true, true, true, true, true };
      }
      else
      {
        var fixities = new bool[6];

        var codeRemaining = code;
        int prevLength;
        do
        {
          prevLength = codeRemaining.Length;
          if (codeRemaining.Contains("xxx"))
          {
            fixities[0] = true;
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xxx", "");
          }
          else if (codeRemaining.Contains("xx"))
          {
            fixities[3] = true;
            codeRemaining = codeRemaining.Replace("xx", "");
          }
          else if (codeRemaining.Contains("x"))
          {
            fixities[0] = true;
            codeRemaining = codeRemaining.Replace("x", "");
          }

          if (codeRemaining.Contains("yyy"))
          {
            fixities[1] = true;
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yyy", "");
          }
          else if (codeRemaining.Contains("yy"))
          {
            fixities[4] = true;
            codeRemaining = codeRemaining.Replace("yy", "");
          }
          else if (codeRemaining.Contains("y"))
          {
            fixities[1] = true;
            codeRemaining = codeRemaining.Replace("y", "");
          }

          if (codeRemaining.Contains("zzz"))
          {
            fixities[2] = true;
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zzz", "");
          }
          else if (codeRemaining.Contains("zz"))
          {
            fixities[5] = true;
            codeRemaining = codeRemaining.Replace("zz", "");
          }
          else if (codeRemaining.Contains("z"))
          {
            fixities[2] = true;
            codeRemaining = codeRemaining.Replace("z", "");
          }
        } while (codeRemaining.Length > 0 && (codeRemaining.Length < prevLength));

        return fixities;
      }
    }

    #endregion
  }
}

