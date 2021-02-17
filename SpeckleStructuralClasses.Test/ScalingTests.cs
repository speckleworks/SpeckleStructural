using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;

namespace SpeckleStructuralClasses.Test
{
  [TestFixture]
  public class ScalingTests
  {
    private StructuralAxis p1;
    private StructuralAxis p2;

    [SetUp]
    public void SetTestData()
    {
      p1 = new StructuralAxis() { Origin = new SpecklePoint(5, 6, 7) };
      p2 = new StructuralAxis() { Origin = new SpecklePoint(8, 9, 10) };
  }

    [Test]
    public void TestScaling1()
    {
      var d = new Dictionary<string, object>
      {
        { "a", (double)100 }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling2()
    {
      var d = new Dictionary<string, object>
      {
        { "a", "100" }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling3()
    {
      var d = new Dictionary<string, object>
      {
        { "a", p1 }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling4()
    {
      var d = new Dictionary<string, object>
      {
        { "a", new [] {p1, p2 } }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling5()
    {
      var d = new Dictionary<string, object>
      {
        { "a", new List<object> {p1, p2 } }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling6()
    {
      var d = new Dictionary<string, object>
      {
        { "l1", new Dictionary<string, object>() { { "l2-a", (double) 25 }, { "l2-b", "hello" }, { "c", new object[] { p1, p2 } } } }
      };
      ScaleProperties(ref d, 0.02);
    }

    [Test]
    public void TestScaling7()
    {
      var d = new Dictionary<string, object>
      {
        { "l1", new Dictionary<string, object>() { { "l2", new Dictionary<string, object>() { { "l3", p1 } } } } }
      };
      ScaleProperties(ref d, 0.02);
    }

    private bool ScaleProperties(ref Dictionary<string, object> dict, double factor)
    {
      var keys = dict.Keys.ToList();
      foreach (var k in keys)
      {
        var v = dict[k];
        if (ScaleValue(ref v, factor))
        {
          dict[k] = v;
        }
      }
      return true;
    }

    private bool ScaleValue(ref object o, double factor)
    {
      if (ScalePrimitive(ref o, factor))
      {
        return true;
      }
      else
      {
        if (o is Dictionary<string, object>)
        {
          var d = (Dictionary<string, object>)o;
          if (ScaleProperties(ref d, factor))
          {
            return true;
          }
        }
        else if (o is Array || o is List<object>)
        {
          var list = ((IEnumerable<object>)o).ToList();
          for (var i = 0; i < list.Count(); i++)
          {
            var candidate = list[i];
            ScaleValue(ref candidate, factor);
          }
        }
        else
        {
          return ScaleObject(ref o, factor);
        }
      }
      return false;
    }

    private bool ScaleObject(ref object o, double factor)
    {
      try
      {
        var scaleMethod = o.GetType().GetMethod("Scale");
        scaleMethod.Invoke(o, new object[] { factor });
        return true;
      }
      catch
      {
        return false;
      }
    }

    private bool ScalePrimitive(ref object p, double factor)
    {
      if (p is double)
      {
        p = (double)p * factor;
        return true;
      }
      else if (p is float)
      {
        p = (float)p * factor;
        return true;
      }
      else if (p is decimal)
      {
        p = (decimal)p * (decimal)factor;
        return true;
      }
      return (p is string);
    }
  }
}
