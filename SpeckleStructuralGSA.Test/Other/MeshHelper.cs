using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralGSA.Test
{
  public static class MeshHelper
  {
    public static bool Intersects(this Line2D candidate, Line2D other)
    {
      var intPt = candidate.IntersectWith(other);
      if (!intPt.HasValue)
      { 
        return false; 
      }
      //By default MathNet defines lines with infinite length so determine if the intersection point is actually within the original bounds of the line

      var intPtOnCandidateLine = candidate.ClosestPointTo(intPt.Value, true);
      var intPtOnOtherLine = other.ClosestPointTo(intPt.Value, true);

      return (intPtOnCandidateLine.DistanceTo(intPtOnOtherLine).Equals(0));
    }

    //Between does not include being parallel to either vectors - the value returned will be zer0
    public static bool IsBetweenVectors(this Vector2D candidate, Vector2D vFrom, Vector2D vTo)
    {
      var candidateDia = candidate.DiamondAngle();
      var fromDia = vFrom.DiamondAngle();
      var toDia = vTo.DiamondAngle();

      if(fromDia > toDia)
      {
        toDia += 4; //4 is the maximum diamond angle, so this will add a revolution
      }

      return (candidateDia > fromDia && candidateDia < toDia);
    }

    //Alternative to the expensive calculation of vector angles using trigonometry.
    //Sourced from: https://stackoverflow.com/questions/1427422/cheap-algorithm-to-find-measure-of-angle-between-vectors
    public static double DiamondAngle(this Vector2D v)
    {
      var x = v.X;
      var y = v.Y;
      if (y >= 0)
      {
        return (x >= 0 ? y / (x + y) : 1 - x / (-x + y));
      }
      else
      {
        return (x < 0 ? 2 - y / (-x - y) : 3 + x / (x - y));
      }
    }
  }
}
