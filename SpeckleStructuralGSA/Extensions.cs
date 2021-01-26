using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  public static class Extensions
  {
    /// <summary>
    /// Will get the string value for a given enums value, this will
    /// only work if you assign the StringValue attribute to
    /// the items in your enum.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetStringValue(this IConvertible value)
    {
      // Get the type
      var type = value.GetType();

      // Get fieldinfo for this type
      var fieldInfo = type.GetField(value.ToString());

      // Get the stringvalue attributes
      var attribs = fieldInfo.GetCustomAttributes(
          typeof(StringValue), false) as StringValue[];

      // Return the first if there was a match.
      return attribs.Length > 0 ? attribs[0].Value : null;
    }

    public static bool TryParseStringValue<T>(this string v, out T value) where T :  IConvertible
    {
      if (!typeof(T).IsEnum)
      {
        throw new ArgumentException("T must be an enumerated type");
      }
      var enumValues = typeof(T).GetEnumValues().OfType<T>().ToDictionary(ev => GetStringValue(ev), ev => ev);
      if (enumValues.Keys.Any(k => k.Equals(v, StringComparison.InvariantCultureIgnoreCase)))
      {
        value = enumValues[v];
        return true;
      }
      value = default(T);
      return false;
    }

    public static bool ValidNonZero(this double? v)
    {
      return v.HasValue && v > 0;
    }

    public static bool ValidNonZero(this StructuralAxis axis)
    {
      return (axis != null && axis.Xdir != null && axis.Ydir != null && axis.Xdir.Value.Any(v => v != 0) && axis.Ydir.Value.Any(v => v != 0));
    }

    public static bool ValidNonZero(this int? v)
    {
      return v.HasValue && v > 0;
    }

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

    public static string[] ListSplit(this string list, char delimiter)
    {
      return Regex.Split(list, delimiter + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
    }

    /// <summary>
    /// Returns the GWA keyword from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <returns>GWA keyword</returns>
    public static string GetGSAKeyword(this object t)
    {
      return (string)t.GetAttribute<GSAObject>("GSAKeyword");
    }

    /// <summary>
    /// Returns the sub GWA keyword from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <returns>Sub GWA keyword</returns>
    public static string[] GetSubGSAKeyword(this object t)
    {
      return (string[])t.GetAttribute<GSAObject>("SubGSAKeywords");
    }

    /// <summary>
    /// Extract attribute from GSAObject objects or type.
    /// </summary>
    /// <param name="t">GSAObject objects or type</param>
    /// <param name="attribute">Attribute to extract</param>
    /// <returns>Attribute value</returns>
    public static object GetAttribute<T>(this object t, string attribute)
    {
      try
      {
        var attObj = (t is Type) ? Attribute.GetCustomAttribute((Type)t, typeof(T)) : Attribute.GetCustomAttribute(t.GetType(), typeof(T));
        return typeof(T).GetProperty(attribute).GetValue(attObj);
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
    public static bool Is1DMember(this string type)
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
    public static bool Is2DMember(this string type)
    {
      if (type == "2D_GENERIC" | type == "SLAB" | type == "WALL")
        return true;
      else
        return false;
    }

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
  }
}
