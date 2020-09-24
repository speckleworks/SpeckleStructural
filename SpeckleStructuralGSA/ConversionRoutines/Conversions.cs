using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    #region nodal_result_csv_conversion
    public static List<string> ToNodeResultCsvColumns(this string selectedNodalResultTypes)
    {
      switch (selectedNodalResultTypes)
      {
        case "Nodal Displacements": return new List<string> { "translation", "rotation" };
        case "Nodal Velocity": return new List<string> { "velocity", "rotational_velocity" };
        case "Nodal Acceleration": return new List<string> { "acceleration", "rotational_acceleration" };
        case "Nodal Reaction": return new List<string> { "reaction_force", "reaction_moment" };
        case "Constraint Forces": return new List<string> { "constraint_force", "constraint_moment" };
        case "Nodal Forces":
        case "Nodal Mass":
        case "Nodal Soil":
        case "0D Element Displacement":
        case "0D Element Force":
        default:
          //These are not supported yet
          return null;
      }
    }

    public static Dictionary<string, object> ToNodeResultValue(this object[,] results, string resultType, List<string> colNames)
    {
      switch (resultType)
      {
        case "Nodal Displacements": return results.ToResultValueNodalDisplacement(colNames);
        case "Nodal Velocity": return results.ToResultValueNodalVelocity(colNames);
        case "Nodal Acceleration": return results.ToResultValueNodalAcceleration(colNames);
        case "Nodal Reaction": return results.ToResultValueNodalReaction(colNames);
        case "Constraint Forces": return results.ToResultValueNodalConstraintForce(colNames);
        case "Nodal Forces":
        case "Nodal Mass":
        case "Nodal Soil":
        case "0D Element Displacement":
        case "0D Element Force":
        default:
          //These are not supported yet as they aren't part of the export-csv output
          return null;
      }
    }
    private static Dictionary<string, object> ToResultValueNodalConstraintForce(this object[,] results, List<string> colNames)
    {
      var retDict = new Dictionary<string, object>();

      //Going down the rows once for each field
      for (var r = 0; r < results.GetLength(0); r++)
      {
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("constraint_force")].ToString(), 0.001), new List<string> { "fx", "fy", "fz", "|f|" });
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("constraint_moment")].ToString(), 0.001), new List<string> { "mxx", "myy", "mzz", "|m|" });
      }
      return retDict;
    }

    private static Dictionary<string, object> ToResultValueNodalReaction(this object[,] results, List<string> colNames)
    {
      var retDict = new Dictionary<string, object>();

      //Going down the rows once for each field
      for (var r = 0; r < results.GetLength(0); r++)
      {
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("reaction_force")].ToString(), 0.001), new List<string> { "fx", "fy", "fz", "|f|" });
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("reaction_moment")].ToString(), 0.001), new List<string> { "mxx", "myy", "mzz", "|m|" });
      }
      return retDict;
    }

    private static Dictionary<string, object> ToResultValueNodalAcceleration(this object[,] results, List<string> colNames)
    {
      var retDict = new Dictionary<string, object>();

      //Going down the rows once for each field
      for (var r = 0; r < results.GetLength(0); r++)
      {
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("acceleration")].ToString()), new List<string> { "ax", "ay", "az", "|a|" });
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("rotational_acceleration")].ToString()), new List<string> { "axx", "ayy", "azz", "|r|" });
      }
      return retDict;
    }

    private static Dictionary<string, object> ToResultValueNodalVelocity(this object[,] results, List<string> colNames)
    {
      var retDict = new Dictionary<string, object>();

      //Going down the rows once for each field
      for (var r = 0; r < results.GetLength(0); r++)
      {
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("velocity")].ToString()), new List<string> { "vx", "vy", "vz", "|v|" });
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("rotational_velocity")].ToString()), new List<string> { "axx", "ayy", "azz", "|r|" });
      }
      return retDict;
    }

    private static Dictionary<string, object> ToResultValueNodalDisplacement(this object[,] results, List<string> colNames)
    {
      var retDict = new Dictionary<string, object>();

      for (var r = 0; r < results.GetLength(0); r++)
      {
        var translationVectorValues = StrVectorValues(results[r, colNames.IndexOf("translation")].ToString(), 1000);
        retDict.AppendVectorMagSubset(translationVectorValues, new List<string> { "ux", "uy", "uz", "|u|" });
        retDict.AppendVectorMagSubset(StrVectorValues(results[r, colNames.IndexOf("rotation")].ToString()), new List<string> { "rxx", "ryy", "rzz", "|r|" });
        if (!retDict.ContainsKey("uxy"))
        {
          retDict.Add("uxy", new List<double>());
        }
        ((List<double>)retDict["uxy"]).Add(translationVectorValues.Take(2).VectorMagnitude() * 1000);
      }

      return retDict;
    }

    private static void AppendVectorMagSubset(this Dictionary<string, object> retDict, List<double> values, List<string> fields)
    {
      var numFieldsExceptMagnitude = fields.Count() - 1;  //The last one is assumed to be magnitude
      for (var i = 0; i < numFieldsExceptMagnitude; i++)
      {
        if (!retDict.ContainsKey(fields[i]))
        {
          retDict.Add(fields[i], new List<double>());
        }
        ((List<double>)retDict[fields[i]]).Add(values[i]);
      }
      if (!retDict.ContainsKey(fields.Last()))
      {
        retDict.Add(fields.Last(), new List<double>());
      }
      ((List<double>)retDict[fields.Last()]).Add(values.Take(numFieldsExceptMagnitude).VectorMagnitude());
    }

    private static List<double> StrVectorValues(string v, double scaleFactor = 1)
    {
      var vector = v.Trim();
      if (vector.First() != '[' || vector.Last() != ']')
      {
        return null;
      }
      var strVals = vector.Substring(1, vector.Length - 2).Split(new[] { ',' });
      var values = new List<double>();
      foreach (var s in strVals)
      {
        if (double.TryParse(s, out var val))
        {
          values.Add(val * scaleFactor);
        }
        else
        {
          values.Add(0);
        }
      }

      return values;
    }

    public static double VectorMagnitude(this IEnumerable<double> values)
    {
      return Math.Sqrt(values.Select(v => Math.Pow(v, 2)).Sum());
    }
#endregion

    private static Dictionary<int, string> ToSpeckleBase<T>()
    {
      var objType = typeof(T);
      var keyword = objType.GetGSAKeyword();

      //These are all the as-yet-unserialised GWA lines keyword, which could map to other GSA types, but the ParseGWACommand will quickly exit
      //as soon as it notices that the GWA isn't relevant to this class
      return Initialiser.Cache.GetGwaToSerialise(keyword);
    }

    public static double[] Essential(this IEnumerable<double> coords)
    {
      var pts = coords.ToPoints();
      var reducedPts = pts.Essential();
      var retCoords = new double[reducedPts.Count() * 3];
      for (var i = 0; i < reducedPts.Count(); i++)
      {
        retCoords[i * 3] = reducedPts[i].X;
        retCoords[(i * 3) + 1] = reducedPts[i].Y;
        retCoords[(i * 3) + 2] = reducedPts[i].Z;
      }
      return retCoords;
    }

    public static List<Point3D> Essential(this List<Point3D> origPts)
    {
      var origPtsExtended = new List<Point3D>() { origPts.Last() };
      origPtsExtended.AddRange(origPts);
      origPtsExtended.Add(origPts.First());
      var numPtsExtended = origPtsExtended.Count();
      var retList = new List<Point3D>();

      for (var i = 1; i < (numPtsExtended - 1); i++)
      {
        var prev = origPtsExtended[i - 1];
        var next = origPtsExtended[i + 1];
        if (!origPtsExtended[i].IsOnLineBetween(prev, next))
        {
          retList.Add(origPtsExtended[i]);
        }
      }

      return retList;
    }

    public static bool IsOnLineBetween(this Point3D p, Point3D start, Point3D end)
    {
      var l = new Line3D(start, end);
      return l.IsOnLine(p);
    }

    public static List<Point3D> ToPoints(this IEnumerable<double> coords)
    {
      var numPts = (int)(coords.Count() / 3);
      var pts = new List<Point3D>();

      var coordsArray = coords.ToArray();
      for (var i = 0; i < numPts; i++)
      {
        pts.Add(new Point3D(coordsArray[i * 3], coordsArray[(i * 3) + 1], coordsArray[(i * 3) + 2]));
      }
      return pts;
    }

    public static int ToInt(this string v)
    {
      try
      {
        return Convert.ToInt32(v);
      }
      catch
      {
        return 0;
      }
    }

    public static bool IsOnLine(this Line3D l, Point3D p)
    {
      var closest = l.ClosestPointTo(p, true);
      var ret = (closest.Equals(p, 0.001));
      return ret;
    }
  }
}
