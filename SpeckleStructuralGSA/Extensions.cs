using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using MathNet.Spatial.Euclidean;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SpeckleStructuralGSA.Schema;

namespace SpeckleStructuralGSA
{
  public static class Extensions
  {
    #region mesh
    //This is to cater for situations where the mesh has duplicate points; these are when the same combination of x/y/z values are repeated
    //in the vertices collection
    public static void Consolidate(this SpeckleMesh mesh)
    {
      var vPts = Enumerable.Range(0, mesh.NumVertices()).Select(v => new Point3D(mesh.Vertices[v * 3], mesh.Vertices[(v * 3) + 1], mesh.Vertices[(v * 3) + 2])).ToList();

      //This algorithm is O(N^2) at the moment
      var indexConsolidateMappings = new Dictionary<int, int>();
      var newPts = new List<Point3D>();
      for (var i = 0; i < vPts.Count(); i++)
      {
        var found = false;
        for (int j = 0; j < newPts.Count(); j++)
        {
          if (vPts[i].Equals(newPts[j], SpeckleStructuralClasses.Helper.PointComparisonEpsilon))
          {
            indexConsolidateMappings.Add(i, j);
            found = true;
            break;
          }
        }
        if (!found)
        {
          var newIndex = newPts.Count();
          newPts.Add(vPts[i]);
          indexConsolidateMappings.Add(i, newIndex);
        }
      }

      var newFaces = mesh.Faces.ToList();
      var f = 0;
      do
      {
        var numInFace = (newFaces[f] == 0) ? 3 : 4;
        if ((f + numInFace) < newFaces.Count())
        {
          f++;
          for (var v = 0; v < numInFace; v++)
          {
            if (indexConsolidateMappings.ContainsKey(newFaces[f + v]))
            {
              newFaces[f + v] = indexConsolidateMappings[newFaces[f + v]];
            }
          }
        }
        f += numInFace;
      } while (f < newFaces.Count());

      mesh.Faces = newFaces;
      mesh.Vertices = newPts.SelectMany(p => new[] { p.X, p.Y, p.Z }).ToList();
    }

    public static List<int[]> Edges(this SpeckleMesh mesh)
    {
      var edgePairs = mesh.FaceEdgePairs();
      var vPts = Enumerable.Range(0, mesh.Vertices.Count() / 3).Select(i => new Point3D(mesh.Vertices[i * 3], mesh.Vertices[(i * 3) + 1], mesh.Vertices[(i * 3) + 2])).ToList();

      //Cap the number of attempts at finding connecting lines to no more than the number of lines overall
      var iterationCount = 0;
      var maxIterations = edgePairs.Count();

      var loops = new List<List<int>>();

      var remainingEdgePairs = edgePairs.ToList();

      do
      {
        var currIndex = remainingEdgePairs.First()[1];
        var endIndex = remainingEdgePairs.First()[0];

        var loop = new List<int>() { currIndex };

        remainingEdgePairs = remainingEdgePairs.Skip(1).ToList();
        var error = false;

        do
        {
          for (var i = 0; i < 2; i++)
          {
            if (FindNextLoopEndIndex(currIndex, remainingEdgePairs, out var nextLoopEndIndex, out var connectingEdgePairIndex))
            {
              currIndex = nextLoopEndIndex.Value;  //Move the end of the loop along this newly-found line to its end
              loop.Add(currIndex);
              remainingEdgePairs.RemoveAt(connectingEdgePairIndex.Value);
            }
            iterationCount++;
          }

        } while (remainingEdgePairs.Count() > 0 && currIndex != endIndex && !error && iterationCount < maxIterations);

        if (!error && loop.Count() > 0)
        {
          loops.Add(loop);
        }
      } while (remainingEdgePairs.Count() > 0 && iterationCount < maxIterations);

      var lengthsOfLoops = new List<double>();
      foreach (var l in loops)
      {
        double length = 0;
        for (var i = 0; i < l.Count(); i++)
        {
          var j = ((i + 1) < l.Count()) ? i + 1 : 0;

          length += (new Line3D(vPts[l[i]], vPts[l[j]])).Length;
        }
        lengthsOfLoops.Add(length);
      }

      //Assumption: the longest length loop is the outer loop
      //Sort by loop length
      var ordered = lengthsOfLoops
        .Select((x, i) => new KeyValuePair<double, int>(x, i))
        .OrderBy(x => x.Key)
        .Select(x => x.Value)
        .Reverse();

      var sortedEdgeConnectivities = new List<int[]>();
      foreach (var i in ordered)
      {
        sortedEdgeConnectivities.Add(loops[i].ToArray()); ;
      }

      return sortedEdgeConnectivities;
    }

    public static int NumVertices(this SpeckleMesh mesh) => (mesh.Vertices == null) ? 0 : (int)(mesh.Vertices.Count() / 3);

    public static int NumFaces(this SpeckleMesh mesh)
    {
      if (mesh.Vertices == null || mesh.Vertices.Count() == 0 || mesh.Faces == null || mesh.Faces.Count() == 0)
      {
        return 0;
      }
      var remainingNumPointsInFace = 0;
      var numFaces = 0;
      for (var i = 0; i < mesh.Faces.Count(); i++)
      {
        if (remainingNumPointsInFace == 0)
        {
          numFaces++;
          remainingNumPointsInFace = mesh.Faces[i] + 3;
        }
        else
        {
          remainingNumPointsInFace--;
        }
      }
      return numFaces;
    }

    private static List<int[]> FaceEdgePairs(this SpeckleMesh mesh)
    {
      var allEdgePairs = new List<int[]>();
      var edgePairCounts = new Dictionary<int, int>();

      var i = 0;
      do
      {
        var numInFace = (mesh.Faces[i] == 0) ? 3 : 4;
        if ((i + numInFace) < mesh.Faces.Count())
        {
          i++;
          for (var v = 0; v < numInFace; v++)
          {
            var pair = (new int[] { mesh.Faces[i + v], mesh.Faces[((v + 1) == numInFace) ? i : i + v + 1] }).OrderBy(n => n).ToArray();
            var foundIndex = allEdgePairs.FindIndex(ep => EqualPair(ep, pair));
            if (foundIndex >= 0)
            {
              edgePairCounts[foundIndex]++;
            }
            else
            {
              allEdgePairs.Add(pair);
              edgePairCounts.Add(allEdgePairs.IndexOf(pair), 1);
            }
          }
        }
        i += numInFace;
      } while (i < mesh.Faces.Count());

      var edgePairIndices = edgePairCounts.Where(kvp => kvp.Value == 1).Select(kvp => kvp.Key).ToList();

      return edgePairIndices.Select(pi => allEdgePairs[pi]).ToList();
    }

    private static bool FindNextLoopEndIndex(int pointIndex, List<int[]> edgePairs, out int? nextLoopEndIndex, out int? connectingEdgePairIndex)
    {
      for (var i = 0; i < edgePairs.Count(); i++)
      {
        if (pointIndex == edgePairs[i][0])
        {
          nextLoopEndIndex = edgePairs[i][1];
          connectingEdgePairIndex = i;
          return true;
        }
        else if (pointIndex == edgePairs[i][1])
        {
          nextLoopEndIndex = edgePairs[i][0];
          connectingEdgePairIndex = i;
          return true;
        }
      }
      nextLoopEndIndex = null;
      connectingEdgePairIndex = null;
      return false;
    }

    private static bool EqualPair(int[] p1, int[] p2)
    {
      return ((p1[0] == p2[0] && p1[1] == p2[1]) || (p1[1] == p2[0] && p1[0] == p2[1]));
    }

    #endregion


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
