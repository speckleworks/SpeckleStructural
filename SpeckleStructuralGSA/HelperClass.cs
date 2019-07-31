using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
      double cos = Math.Cos(angle);
      double sin = Math.Sin(angle);

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
        string rgbString = str.Split(new char[] { '(', ')' })[1];
        if (rgbString.Contains(","))
        {
          string[] rgbValues = rgbString.Split(',');
          int hexVal = Convert.ToInt32(rgbValues[0])
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

      string colStr = str.Replace('_', ' ').ToLower();
      colStr = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(colStr);
      colStr = Regex.Replace(colStr, " ", "");

      Color col = Color.FromKnownColor((KnownColor)Enum.Parse(typeof(KnownColor), colStr));
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
      Color col = Color.FromArgb(color);
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
      foreach (char c in str)
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
      try
      {
        if (t is Type)
        {
          GSAObject attObj = (GSAObject)Attribute.GetCustomAttribute((Type)t, typeof(GSAObject));
          return typeof(GSAObject).GetProperty(attribute).GetValue(attObj);
        }
        else
        {
          GSAObject attObj = (GSAObject)Attribute.GetCustomAttribute(t.GetType(), typeof(GSAObject));
          return typeof(GSAObject).GetProperty(attribute).GetValue(attObj);
        }
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
      List<double> coordinates = new List<double>();

      foreach (Match m in Regex.Matches(desc, @"(?<=\()(.+?)(?=\))"))
      {
        string[] pieces = m.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
      List<Tuple<string, double>> ret = new List<Tuple<string, double>>();

      list = list.Replace(" ", "");

      double multiplier = 1;
      bool negative = false;

      for (int pos = 0; pos < list.Count(); pos++)
      {
        char currChar = list[pos];

        if (currChar >= '0' && currChar <= '9')
        {
          string mult = "";
          mult += currChar.ToString();

          pos++;
          while (pos < list.Count() && ((list[pos] >= '0' && list[pos] <= '9') || list[pos] == '.'))
            mult += list[pos++].ToString();
          pos--;

          multiplier = Convert.ToDouble(mult);
        }
        else if (currChar >= 'A' && currChar <= 'Z')
        {
          string loadDesc = "";
          loadDesc += currChar.ToString();

          pos++;
          while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
            loadDesc += list[pos++].ToString();
          pos--;

          double actualFactor = multiplier == 0 ? 1 : multiplier;
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
            Tuple<string, double> prevDesc = ret.Last();

            string type = prevDesc.Item1[0].ToString();
            int start = Convert.ToInt32(prevDesc.Item1.Substring(1)) + 1;

            string endDesc = "";

            pos++;
            pos++;
            while (pos < list.Count() && list[pos] >= '0' && list[pos] <= '9')
              endDesc += list[pos++].ToString();
            pos--;

            int end = Convert.ToInt32(endDesc);

            for (int i = start; i <= end; i++)
              ret.Add(new Tuple<string, double>(type + i.ToString(), prevDesc.Item2));
          }
        }
        else if (currChar == '(')
        {
          double actualFactor = multiplier == 0 ? 1 : multiplier;
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
  }
}
