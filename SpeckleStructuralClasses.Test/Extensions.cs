using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace SpeckleStructuralClasses.Test
{
  //Some code is copied here to avoid a reference to SpeckleStructuralGSA
  public static class Extensions
  {
    public static bool ContainsSublist<T>(this IEnumerable<T> allValues, IEnumerable<T> valuesToSearch, int stepNumber = 1)
    {
      var n = valuesToSearch.Count();
      for (var i = 0; i < allValues.Count(); i += stepNumber)
      {
        if (allValues.Skip(i).Take(n).SequenceEqual(valuesToSearch))
        {
          return true;
        }
      }
      return false;
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
  }
}
