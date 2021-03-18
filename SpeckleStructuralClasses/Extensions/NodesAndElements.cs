using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using MathNet.Spatial.Euclidean;

namespace SpeckleStructuralClasses
{
  public partial class Structural0DSpring
  {
    public Structural0DSpring() { }

    public override void Scale(double factor)
    {
      if (Value != null)
      {
        for (var i = 0; i < Value.Count(); i++)
        {
          Value[i] *= factor;
        }
      }
      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class StructuralNode
  {
    public StructuralNode() { }

    public StructuralNode(double[] value, StructuralAxis axis, StructuralVectorBoolSix restraint, StructuralVectorSix stiffness, double mass, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Value = value.ToList();
      Axis = axis;
      Restraint = restraint;
      Stiffness = stiffness;
      Mass = mass;
      ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (Value != null)
      {
        for (var i = 0; i < Value.Count(); i++)
        {
          Value[i] *= factor;
        }
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class Structural1DElement
  {
    public Structural1DElement() { }

    public Structural1DElement(double[] value, Structural1DElementType elementType, string propertyRef, StructuralVectorThree zAxis, StructuralVectorBoolSix[] endRelease, StructuralVectorThree[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Value = value.ToList();
      ElementType = elementType;
      PropertyRef = propertyRef;
      ZAxis = zAxis;
      EndRelease = endRelease == null ? null : endRelease.ToList();
      Offset = offset == null ? null : offset.ToList();
      ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (Value != null)
      {
        for (var i = 0; i < Value.Count(); i++)
        {
          Value[i] *= factor;
        }
      }

      if (Offset != null)
      {
        for (var i = 0; i < Offset.Count(); i++)
        {
          Offset[i].Scale(factor);
        }
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class Structural1DElementPolyline
  {
    public Structural1DElementPolyline() { }

    public Structural1DElementPolyline(double[] value, Structural1DElementType elementType, string propertyRef, StructuralVectorThree[] zAxis, StructuralVectorBoolSix[] endRelease, StructuralVectorThree[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Value = value.ToList();
      ElementType = elementType;
      PropertyRef = propertyRef;
      ZAxis = zAxis == null ? null : zAxis.ToList();
      EndRelease = endRelease == null ? null : endRelease.ToList();
      Offset = offset == null ? null : offset.ToList();
      ApplicationId = applicationId;

      GenerateHash();
    }

    public Structural1DElementPolyline(Structural1DElement[] elements, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (elements.Length == 0)
        throw new Exception("No elements specified.");

      if (properties != null)
      {
        Properties = properties;
      }
      Value = new List<double>(elements[0].Value.Take(3).ToArray());
      ElementType = elements[0].ElementType;
      PropertyRef = elements[0].PropertyRef;
      var zAxes = new List<StructuralVectorThree>();
      var endReleases = new List<StructuralVectorBoolSix>();
      var offsets = new List<StructuralVectorThree>();

      foreach (var element in elements)
      {
        if (ElementType != element.ElementType)
          throw new Exception("Different ElementTypes.");

        if (PropertyRef != element.PropertyRef)
          throw new Exception("Different PropertyRef.");

        if (!Value.Skip(Value.Count() - 3).Take(3).SequenceEqual(element.Value.Take(3)))
          throw new Exception("Elements not continuous.");

        Value.AddRange(element.Value.Skip(3).Take(3));
        zAxes.Add(element.ZAxis);
        endReleases.AddRange(element.EndRelease);
        offsets.AddRange(element.Offset);
      }
      EndRelease = endReleases;
      ZAxis = zAxes;
      Offset = offsets;

      ApplicationId = applicationId;

      GenerateHash();
    }

    public Structural1DElement[] Explode()
    {
      var elements = new List<Structural1DElement>();

      for (var i = 0; i < Value.Count() / 3 - 1; i++)
      {
        var element = new Structural1DElement(
            Value.Skip(i * 3).Take(6).ToArray(),
            ElementType,
            PropertyRef,
            ZAxis == null || ZAxis.Count() <= i ? null : ZAxis[i],
            EndRelease == null || EndRelease.Count() < i * 2 + 2 ? null : EndRelease.Skip(i * 2).Take(2).ToArray(),
            Offset == null || Offset.Count() < i * 2 + 2 ? null : Offset.Skip(i * 2).Take(2).ToArray(),
            ElementApplicationId != null && ElementApplicationId.Count() > i ? ElementApplicationId[i] : Helper.CreateChildApplicationId(i, ApplicationId)
        )
        {
          GSADummy = GSADummy,
          GSAMeshSize = GSAMeshSize
        };
        elements.Add(element);
      }

      return elements.ToArray();
    }

    public override void Scale(double factor)
    {
      if (Value != null)
      {
        for (var i = 0; i < Value.Count(); i++)
        {
          Value[i] *= factor;
        }
      }

      if (Offset != null)
      {
        for (var i = 0; i < Offset.Count(); i++)
        {
          Offset[i].Scale(factor);
        }
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class Structural2DElement
  {
    public Structural2DElement() { }

    public Structural2DElement(double[] vertices, int[] faces, int[] colors, Structural2DElementType elementType, string propertyRef, StructuralAxis axis, double offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Vertices = vertices.ToList();
      Faces = faces.ToList();
      Colors = colors == null ? null : colors.ToList();
      ElementType = elementType;
      PropertyRef = propertyRef;
      Axis = axis;
      Offset = offset;
      ApplicationId = applicationId;

      TextureCoordinates = null;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      if (Vertices != null)
      {
        for (var i = 0; i < Vertices.Count(); i++)
        {
          Vertices[i] *= factor;
        }
      }
      if (Offset != null)
      {
        Offset *= factor;
      }
      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class Structural2DElementMesh
  {
    public Structural2DElementMesh() { }

    public Structural2DElementMesh(IEnumerable<double> vertices, IEnumerable<int> faces, IEnumerable<int> colors, 
      Structural2DElementType elementType, string propertyRef, IEnumerable<StructuralAxis> axes, IEnumerable<double> offsets, 
      string applicationId = null, double meshSize = 0, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Vertices = vertices.ToList();
      Faces = faces.ToList();
      Colors = colors == null ? null : colors.ToList();
      ElementType = elementType;
      PropertyRef = propertyRef;
      Axis = axes.ToList();
      if (offsets != null && offsets.Count() > 0)
      {
        Offset = offsets.ToList();
      }
      GSAMeshSize = meshSize;
      ApplicationId = applicationId;
      TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DElementMesh(IEnumerable<double> edgeVertices, int? color, Structural2DElementType elementType, string propertyRef,
      IEnumerable<StructuralAxis> axes, IEnumerable<double> offsets, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }

      var pm = new PolygonMesher.PolygonMesher();
      pm.Init(edgeVertices);
      Faces = pm.Faces().ToList();
      Vertices = pm.Coordinates.ToList();

      Colors = (color == null) ? new List<int>() : Enumerable.Repeat(color.Value, Vertices.Count() / 3).ToList();

      ElementType = elementType;
      PropertyRef = propertyRef;
      if (axes != null)
      {
        Axis = axes.ToList();
      }
      if (offsets != null && offsets.Count() > 0)
      {
        Offset = offsets.ToList();
      }
      ApplicationId = applicationId;

      TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DElement[] Explode()
    {
      var elements = new List<Structural2DElement>();

      var faceCounter = 0;

      for (var i = 0; i < Faces.Count(); i++)
      {
        var vertices = new List<double>();
        var colors = new List<int>();

        var numVertices = Faces[i++] + 3;
        for (var j = 0; j < numVertices; j++)
        {
          if (Colors != null && Colors.Count() > Faces[i])
          {
            colors.Add(Colors[Faces[i]]);
          }
          vertices.AddRange(Vertices.Skip(Faces[i++] * 3).Take(3));
        }
        i--;

        var element = new Structural2DElement(
            vertices.ToArray(),
            (new List<int>() { numVertices - 3 }).Concat(Enumerable.Range(0, numVertices)).ToArray(),
            colors.Count() == vertices.Count() / 3 ? colors.ToArray() : new int[0],
            ElementType,
            PropertyRef,
            Axis != null && Axis.Count() > faceCounter ? Axis[faceCounter] : null,
            Offset != null && Offset.Count() > faceCounter ? Offset[faceCounter] : 0,
            ElementApplicationId != null && ElementApplicationId.Count() > faceCounter
              ? ElementApplicationId[faceCounter]
              : Helper.CreateChildApplicationId(i, ApplicationId + "-" + faceCounter.ToString())
        )
        {
          GSADummy = GSADummy,
          GSAMeshSize = GSAMeshSize
        };
        elements.Add(element);

        faceCounter++;
      }

      return elements.ToArray();
    }

    public override void Scale(double factor)
    {
      if (Vertices != null)
      {
        for (var i = 0; i < Vertices.Count(); i++)
        {
          Vertices[i] *= factor;
        }
      }

      if (Offset != null && Offset.Count() > 0)
      {
        for (var i = 0; i < Offset.Count(); i++)
        {
          Offset[i] *= factor;
        }
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }

  }

  public partial class Structural2DVoid
  {
    public Structural2DVoid() { }

    public Structural2DVoid(double[] vertices, int[] faces, int[] colors, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Vertices = vertices.ToList();
      Faces = faces.ToList();
      Colors = colors == null ? null : colors.ToList();
      ApplicationId = applicationId;

      TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DVoid(double[] edgeVertices, int? color, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }

      var pm = new PolygonMesher.PolygonMesher();
      pm.Init(edgeVertices);
      Faces = pm.Faces().ToList();
      Vertices = pm.Coordinates.ToList();

      if (color != null)
      {
        Colors = Enumerable.Repeat(color.Value, Vertices.Count() / 3).ToList();
      }
      else
      {
        Colors = new List<int>();
      }

      ApplicationId = applicationId;

      TextureCoordinates = null;

      GenerateHash();
    }

    public List<int[]> Edges()
    {
      var edgeConnectivities = new List<int[]>();

      // Get face connectivities and close loop
      var faceConnnectivities = new List<int[]>();
      for (var i = 0; i < Faces.Count(); i++)
      {
        var numVertices = Faces[i] + 3;
        i++;
        faceConnnectivities.Add(Faces.Skip(i).Take(numVertices).Concat(Faces.Skip(i).Take(1)).ToArray());
        i += numVertices - 1;
      }

      // Get distinct edges
      var edges = new List<Tuple<int, int, string, string, double>>();

      foreach (var conn in faceConnnectivities)
      {
        for (var i = 0; i < conn.Length - 1; i++)
        {
          var c1 = string.Join(",", Vertices.Skip(conn[i] * 3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          var c2 = string.Join(",", Vertices.Skip(conn[i + 1] * 3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          var length = Math.Pow(Vertices.Skip(conn[i] * 3).Take(1).First() - Vertices.Skip(conn[i + 1] * 3).Take(1).First(), 2) +
            Math.Pow(Vertices.Skip(conn[i] * 3 + 1).Take(1).First() - Vertices.Skip(conn[i + 1] * 3 + 1).Take(1).First(), 2) +
            Math.Pow(Vertices.Skip(conn[i] * 3 + 2).Take(1).First() - Vertices.Skip(conn[i + 1] * 3 + 2).Take(1).First(), 2);
          length = Math.Sqrt(length);

          if (edges.Any(e => (e.Item3 == c1 && e.Item4 == c2) |
              (e.Item3 == c2 && e.Item4 == c1)))
          {
            edges.RemoveAll(x => (x.Item3 == c1 && x.Item4 == c2) || (x.Item3 == c2 && x.Item4 == c1));
          }
          else
          {
            if (conn[i] < conn[i + 1])
              edges.Add(new Tuple<int, int, string, string, double>(conn[i], conn[i + 1], c1, c2, length));
            else
              edges.Add(new Tuple<int, int, string, string, double>(conn[i + 1], conn[i], c2, c1, length));
          }
        }
      }

      // Reorder the edges
      var lengthsOfEdges = new List<double>();
      double currentLength = 0;

      var currentLoop = new List<int>();
      var flatCoor = new List<string>();
      currentLoop.Add(edges[0].Item1);
      currentLoop.Add(edges[0].Item2);
      flatCoor.Add(edges[0].Item3);
      flatCoor.Add(edges[0].Item4);

      edges.RemoveAt(0);

      while (edges.Count > 0)
      {
        var commonVertex = flatCoor.Last();

        var nextEdge = edges.Where(e => e.Item3 == commonVertex | e.Item4 == commonVertex).ToList();

        if (nextEdge.Count > 0)
        {
          currentLoop.Add(nextEdge[0].Item3 == commonVertex ? nextEdge[0].Item2 : nextEdge[0].Item1);
          flatCoor.Add(nextEdge[0].Item3 == commonVertex ? nextEdge[0].Item4 : nextEdge[0].Item3);
          edges.Remove(nextEdge[0]);
          currentLength += nextEdge[0].Item5;
        }
        else
        {
          // Next edge not found. Stop looking for more
          break;
        }

        if (currentLoop[0] == currentLoop.Last())
        {
          currentLoop.RemoveAt(0);

          edgeConnectivities.Add(currentLoop.ToArray());
          lengthsOfEdges.Add(currentLength);

          currentLength = 0;
          currentLoop = new List<int>();
          faceConnnectivities = new List<int[]>();

          if (edges.Count > 0)
          {
            currentLoop.Add(edges[0].Item1);
            currentLoop.Add(edges[0].Item2);
            flatCoor.Add(edges[0].Item3);
            flatCoor.Add(edges[0].Item4);

            edges.RemoveAt(0);
          }
        }
      }

      // Sort based on length
      var ordered = lengthsOfEdges
        .Select((x, i) => new KeyValuePair<double, int>(x, i))
        .OrderBy(x => x.Key)
        .Select(x => x.Value)
        .Reverse();

      var sortedEdgeConnectivities = new List<int[]>();

      foreach (var i in ordered)
        sortedEdgeConnectivities.Add(edgeConnectivities[i]);

      return sortedEdgeConnectivities;
    }

    public override void Scale(double factor)
    {
      if (Vertices != null)
      {
        for (var i = 0; i < Vertices.Count(); i++)
        {
          Vertices[i] *= factor;
        }
      }

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }
}
