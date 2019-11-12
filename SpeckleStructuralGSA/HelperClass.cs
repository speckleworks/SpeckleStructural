using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace SpeckleStructuralGSA
{
  /// <summary>
  /// Static class containing helper functions used throughout SpeckleGSA
  /// </summary>
  public static class HelperClass
  {
    #region Math
    /// <summary>
    /// Convert radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians</param>
    /// <returns>Angle in degrees</returns>
    public static double ToDegrees(this int radians)
    {
      return ((double)radians).ToDegrees();
    }

    /// <summary>
    /// Convert radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians</param>
    /// <returns>Angle in degrees</returns>
    public static double ToDegrees(this double radians)
    {
      return radians * (180 / Math.PI);
    }

    /// <summary>
    /// Convert degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees</param>
    /// <returns>Angle in radians</returns>
    public static double ToRadians(this int degrees)
    {
      return ((double)degrees).ToRadians();
    }

    /// <summary>
    /// Convert degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees</param>
    /// <returns>Angle in radians</returns>
    public static double ToRadians(this double degrees)
    {
      return degrees * (Math.PI / 180);
    }

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
    public static Matrix3D RotationMatrix(Vector3D zUnitVector, double angle)
    {
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
    }
    #endregion

    #region Lists
    /// <summary>
    /// Splits lists, keeping entities encapsulated by "" together.
    /// </summary>
    /// <param name="list">String to split</param>
    /// <param name="delimiter">Delimiter</param>
    /// <returns>Array of strings containing list entries</returns>
    public static string[] ListSplit(this string list, string delimiter)
    {
      return Regex.Split(list, delimiter + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

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

    #region Color
    /// <summary>
    /// Converts GSA color description into hex color.
    /// </summary>
    /// <param name="str">GSA color description</param>
    /// <returns>Hex color</returns>
    public static int? ParseGSAColor(this string str)
    {
      if (str.Contains("NO_RGB"))
        return null;

      if (str.Contains("RGB"))
      {
        var rgbString = str.Split(new char[] { '(', ')' })[1];
        if (rgbString.Contains(","))
        {
          var rgbValues = rgbString.Split(',');
          var hexVal = Convert.ToInt32(rgbValues[0])
              + Convert.ToInt32(rgbValues[1]) * 256
              + Convert.ToInt32(rgbValues[2]) * 256 * 256;
          return hexVal;
        }
        else
        {
          return Int32.Parse(
          rgbString.Remove(0, 2).PadLeft(6, '0'),
          System.Globalization.NumberStyles.HexNumber);
        }
      }

      var colStr = str.Replace('_', ' ').ToLower();
      colStr = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(colStr);
      colStr = Regex.Replace(colStr, " ", "");

      var col = Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), colStr));
      return col.R + col.G * 256 + col.B * 256 * 256;
    }

    /// <summary>
    /// Converts hex color to ARGB.
    /// </summary>
    /// <param name="str">Hex color</param>
    /// <returns>ARGB color</returns>
    public static int? HexToArgbColor(this int? color)
    {
      if (color == null)
        return null;

      return Color.FromArgb(255,
                     (int)color % 256,
                     ((int)color / 256) % 256,
                     ((int)color / 256 / 256) % 256).ToArgb();
    }

    /// <summary>
    /// Converts ARGB to hex color
    /// </summary>
    /// <param name="str">ARGB color</param>
    /// <returns>Hex color</returns>
    public static int ArgbToHexColor(this int color)
    {
      var col = Color.FromArgb(color);
      return col.R + col.G * 256 + col.B * 256 * 256;
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
        if (c < '0' || c > '9')
          return false;

      return true;
    }
    #endregion

    #region Miscellaneous
    /// <summary>
    /// Returns the GWA keyword from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <returns>GWA keyword</returns>
    public static string GetGSAKeyword(this object t)
    {
      return (string)t.GetAttribute("GSAKeyword");
    }

    /// <summary>
    /// Returns the sub GWA keyword from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <returns>Sub GWA keyword</returns>
    public static string[] GetSubGSAKeyword(this object t)
    {
      return (string[])t.GetAttribute("SubGSAKeywords");
    }

    /// <summary>
    /// Extract attribute from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <param name="attribute">Attribute to extract</param>
    /// <returns>Attribute value</returns>
    public static object GetAttribute(this object t, string attribute)
    {
      var attributeType = typeof(GSAObject);
      try
      {
        var attObj = (t is Type) ? Attribute.GetCustomAttribute((Type)t, attributeType) : Attribute.GetCustomAttribute(t.GetType(), attributeType);
        return attributeType.GetProperty(attribute).GetValue(attObj);
      }
      catch { return null; }
    }

    /// <summary>
    /// Returns number of nodes of the GSA element type
    /// </summary>
    /// <param name="type">GSA element type</param>
    /// <returns>Number of nodes</returns>
    public static int ParseElementNumNodes(this string type)
    {
      return (int)((ElementNumNodes)Enum.Parse(typeof(ElementNumNodes), type));
    }

    /// <summary>
    /// Check if GSA member type is 1D
    /// </summary>
    /// <param name="type">GSA member type</param>
    /// <returns>True if member is 1D</returns>
    public static bool MemberIs1D(this string type)
    {
      if (type == "1D_GENERIC" | type == "COLUMN" | type == "BEAM")
        return true;
      else
        return false;
    }

    /// <summary>
    /// Check if GSA member type is 2D
    /// </summary>
    /// <param name="type">GSA member type</param>
    /// <returns>True if member is 2D</returns>
    public static bool MemberIs2D(this string type)
    {
      if (type == "2D_GENERIC" | type == "SLAB" | type == "WALL")
        return true;
      else
        return false;
    }

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

    public static double? LineLength(this double[] coordinates)
    {
      if (coordinates.Count() < 6)
      {
        return null;
      }
      var x = Math.Abs(coordinates[3] - coordinates[0]);
      var y = Math.Abs(coordinates[4] - coordinates[1]);
      var z = Math.Abs(coordinates[5] - coordinates[2]);
      return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
    }

    public static double ToDouble(this object o)
    {
      try
      {
        var d = Convert.ToDouble(o);
        return d;
      }
      catch
      {
        return 0d;
      }
    }
    #endregion

    #region MovedFromInterfacer
    private const string SID_TAG = "speckle_app_id";

    public static string GenerateSID(SpeckleObject obj)
    {
      var sid = "";

      if (!string.IsNullOrEmpty(obj.ApplicationId))
        sid += "{" + SID_TAG + ":" + obj.ApplicationId + "}";

      return sid;
    }

    public static void SetAxis(StructuralAxis axis, out int index, out string gwa, string name = "")
    {
      var gwaAxisName = name ?? "";
      index = 0;
      gwa = "";

      if (axis == null 
        || (axis.Xdir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          axis.Ydir.Value.SequenceEqual(new double[] { 0, 1, 0 }) &&
          axis.Normal.Value.SequenceEqual(new double[] { 0, 0, 1 })))
      {
        return;
      }
      var res = Initialiser.Indexer.ResolveIndex("AXIS.1", "");

      var ls = new List<string>
      {
        "SET",
        "AXIS.1",
        res.ToString(),
        gwaAxisName,
        "CART",

        "0",
        "0",
        "0",

        axis.Xdir.Value[0].ToString(),
        axis.Xdir.Value[1].ToString(),
        axis.Xdir.Value[2].ToString(),

        axis.Ydir.Value[0].ToString(),
        axis.Ydir.Value[1].ToString(),
        axis.Ydir.Value[2].ToString()
      };

      gwa = string.Join("\t", ls);

      index = res;
    }

    public static void SetAxis(SpeckleVector xVector, SpeckleVector xyVector, SpecklePoint origin, out int index, out string gwaCommand, string name = "")
    {
      gwaCommand = "";
      index = Initialiser.Indexer.ResolveIndex("AXIS.1", "");

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

      gwaCommand = (string.Join("\t", ls));
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
      Vector3D x, y, z;

      x = new Vector3D(coor[3] - coor[0], coor[4] - coor[1], coor[5] - coor[2]);
      x.Normalize();

      if (orientationNode == null)
      {
        if (x.X == 0 && x.Y == 0)
        {
          //Column
          y = new Vector3D(0, 1, 0);
          z = Vector3D.CrossProduct(x, y);
        }
        else
        {
          //Non-Vertical
          var Z = new Vector3D(0, 0, 1);
          y = Vector3D.CrossProduct(Z, x);
          y.Normalize();
          z = Vector3D.CrossProduct(x, y);
          z.Normalize();
        }
      }
      else
      {
        var Yp = new Vector3D(orientationNode[0], orientationNode[1], orientationNode[2]);
        z = Vector3D.CrossProduct(x, Yp);
        z.Normalize();
        y = Vector3D.CrossProduct(z, x);
        y.Normalize();
      }

      //Rotation
      var rotMat = HelperClass.RotationMatrix(x, rotationAngle.ToRadians());
      y = Vector3D.Multiply(y, rotMat);
      z = Vector3D.Multiply(z, rotMat);

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
    public static double[] MapPointsGlobal2Local(double[] values, StructuralAxis axis)
    {
      var newVals = new List<double>();

      for (var i = 0; i < values.Length; i += 3)
      {
        var coor = values.Skip(i).Take(3).ToList();

        double x = 0;
        double y = 0;
        double z = 0;

        x += axis.Xdir.Value[0] * coor[0];
        y += axis.Ydir.Value[0] * coor[0];
        z += axis.Normal.Value[0] * coor[0];

        x += axis.Xdir.Value[1] * coor[1];
        y += axis.Ydir.Value[1] * coor[1];
        z += axis.Normal.Value[1] * coor[1];

        x += axis.Xdir.Value[2] * coor[2];
        y += axis.Ydir.Value[2] * coor[2];
        z += axis.Normal.Value[2] * coor[2];

        newVals.Add(x);
        newVals.Add(y);
        newVals.Add(z);
      }

      return newVals.ToArray();
    }

    /// <summary>
    /// Maps a flat array of coordinates from a local coordinate system to the global Cartesian coordinate system.
    /// </summary>
    /// <param name="values">Flat array of coordinates</param>
    /// <param name="axis">Local coordinate system</param>
    /// <returns>Transformed array of coordinates</returns>
    public static double[] MapPointsLocal2Global(double[] values, StructuralAxis axis)
    {
      var newVals = new List<double>();

      for (var i = 0; i < values.Length; i += 3)
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
      var axisY = Vector3D.CrossProduct(axisZ, axisX);

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
    public static StructuralAxis Parse2DAxis(double[] coor, double rotationAngle = 0, bool isLocalAxis = false)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      var nodes = new List<Vector3D>();

      for (var i = 0; i < coor.Length; i += 3)
        nodes.Add(new Vector3D(coor[i], coor[i + 1], coor[i + 2]));

      if (isLocalAxis)
      {
        if (nodes.Count == 3)
        {
          x = Vector3D.Subtract(nodes[1], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[2], nodes[0]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
        else if (nodes.Count == 4)
        {
          x = Vector3D.Subtract(nodes[2], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[3], nodes[1]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
        else
        {
          // Default to QUAD method
          x = Vector3D.Subtract(nodes[2], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[3], nodes[1]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
      }
      else
      {
        x = Vector3D.Subtract(nodes[1], nodes[0]);
        x.Normalize();
        z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[2], nodes[0]));
        z.Normalize();

        x = new Vector3D(1, 0, 0);
        x = Vector3D.Subtract(x, Vector3D.Multiply(Vector3D.DotProduct(x, z), z));

        if (x.Length == 0)
          x = new Vector3D(0, z.X > 0 ? -1 : 1, 0);

        y = Vector3D.CrossProduct(z, x);

        x.Normalize();
        y.Normalize();
      }

      //Rotation
      var rotMat = HelperClass.RotationMatrix(z, rotationAngle * (Math.PI / 180));
      x = Vector3D.Multiply(x, rotMat);
      y = Vector3D.Multiply(y, rotMat);

      return new StructuralAxis(
          new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
          new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
          new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
      );
    }

    /// <summary>
    /// Calculates the local axis of a point from a GSA node axis.
    /// </summary>
    /// <param name="axis">ID of GSA node axis</param>
    /// <param name="gwaRecord">GWA record of AXIS if used</param>
    /// <param name="evalAtCoor">Coordinates to evaluate axis at</param>
    /// <returns>Axis</returns>
    public static StructuralAxis Parse0DAxis(int axis, IGSAProxy interfacer, out string gwaRecord, double[] evalAtCoor = null)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      gwaRecord = null;

      switch (axis)
      {
        case 0:
          // Global
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 1, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 })
          );
        case -11:
          // X elevation
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 0, -1, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { -1, 0, 0 })
          );
        case -12:
          // Y elevation
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { 0, -1, 0 })
          );
        case -14:
          // Vertical
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 1, 0 })
          );
        case -13:
          // Global cylindrical
          x = new Vector3D(evalAtCoor[0], evalAtCoor[1], 0);
          x.Normalize();
          z = new Vector3D(0, 0, 1);
          y = Vector3D.CrossProduct(z, x);

          return new StructuralAxis(
              new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
              new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
              new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
          );
        default:
          //string res = Initialiser.Interface.GetGWARecords("GET\tAXIS\t" + axis.ToString()).FirstOrDefault();
          var res = Initialiser.Indexer.GetGwa("AXIS.1", axis).First();
          gwaRecord = res;

          var pieces = res.Split(new char[] { '\t' });
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
          var Z = Vector3D.CrossProduct(X, Yp);
          Z.Normalize();

          var Y = Vector3D.CrossProduct(Z, X);

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
              y = Vector3D.CrossProduct(Z, x);
              y.Normalize();

              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
                  new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
                  new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
              );
            case "SPH":
              x = pos;
              x.Normalize();
              z = Vector3D.CrossProduct(Z, x);
              z.Normalize();
              y = Vector3D.CrossProduct(z, x);
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

      if (axisX.X == 0 & axisX.Y == 0)
      {
        // Column
        var Yglobal = new Vector3D(0, 1, 0);

        var angle = Math.Acos(Vector3D.DotProduct(Yglobal, axisY) / (Yglobal.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) return 0;

        var signVector = Vector3D.CrossProduct(Yglobal, axisY);
        var sign = Vector3D.DotProduct(signVector, axisX);

        return sign >= 0 ? angle : -angle;
      }
      else
      {
        var Zglobal = new Vector3D(0, 0, 1);
        var Y0 = Vector3D.CrossProduct(Zglobal, axisX);
        var angle = Math.Acos(Vector3D.DotProduct(Y0, axisY) / (Y0.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) angle = 0;

        var signVector = Vector3D.CrossProduct(Y0, axisY);
        var sign = Vector3D.DotProduct(signVector, axisX);

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
      x0 = Vector3D.Subtract(nodes[1], nodes[0]);
      x0.Normalize();
      z0 = Vector3D.CrossProduct(x0, Vector3D.Subtract(nodes[2], nodes[0]));
      z0.Normalize();

      x0 = new Vector3D(1, 0, 0);
      x0 = Vector3D.Subtract(x0, Vector3D.Multiply(Vector3D.DotProduct(x0, z0), z0));

      if (x0.Length == 0)
        x0 = new Vector3D(0, z0.X > 0 ? -1 : 1, 0);

      x0.Normalize();

      // Find angle
      var angle = Math.Acos(Vector3D.DotProduct(x0, axisX) / (x0.Length * axisX.Length)).ToDegrees();
      if (double.IsNaN(angle)) return 0;

      var signVector = Vector3D.CrossProduct(x0, axisX);
      var sign = Vector3D.DotProduct(signVector, axisZ);

      return sign >= 0 ? angle : -angle;
    }

    public static StructuralLoadTaskType GetLoadTaskType(string taskGwaCommand)
    {
      var taskPieces = taskGwaCommand.ListSplit("\t");
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

    public static string GetApplicationId(string keyword, int id)
    {
      var savedApplicationId = Initialiser.Indexer.GetApplicationId(keyword, id);
      return (string.IsNullOrEmpty(savedApplicationId)) ? ("gsa/" + keyword + "_" + id.ToString()) : savedApplicationId;
    }

    public static int NodeAt(double x, double y, double z, double coincidentNodeAllowance, string applicationId = null)
    {
      var index = Initialiser.Interface.NodeAt(x, y, z, coincidentNodeAllowance);
      
      if (applicationId != null)
      {
        //Only needs to be added to the cache if there is an application ID
        var gwa = Initialiser.Interface.GetGwaForNode(index);
        gwa = Initialiser.Interface.SetApplicationId(gwa, applicationId);
        Initialiser.Indexer.Upsert(gwa);
      }

      return index;
    }
    
    #endregion
  }
}

