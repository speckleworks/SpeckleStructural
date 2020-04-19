using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SpeckleStructuralClasses
{
  public partial class Structural0DSpring
  {
    public Structural0DSpring() { }

    public override void Scale(double factor)
    {
      for (var i = 0; i < Value.Count(); i++)
        Value[i] *= factor;

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
      for (var i = 0; i < Value.Count(); i++)
        Value[i] *= factor;

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
      for (var i = 0; i < Value.Count(); i++)
        Value[i] *= factor;

      if (Offset != null)
        for (var i = 0; i < Offset.Count(); i++)
          Offset[i].Scale(factor);

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
      ZAxis = new List<StructuralVectorThree>();
      EndRelease = new List<StructuralVectorBoolSix>();
      Offset = new List<StructuralVectorThree>();

      foreach (var element in elements)
      {
        if (ElementType != element.ElementType)
          throw new Exception("Different ElementTypes.");

        if (PropertyRef != element.PropertyRef)
          throw new Exception("Different PropertyRef.");

        if (!Value.Skip(Value.Count() - 3).Take(3).SequenceEqual(element.Value.Take(3)))
          throw new Exception("Elements not continuous.");

        Value.AddRange(element.Value.Skip(3).Take(3));
        ZAxis.Add(element.ZAxis);
        EndRelease.AddRange(element.EndRelease);
        Offset.AddRange(element.Offset);
      }

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
            ElementApplicationId != null && ElementApplicationId.Count() > i ? ElementApplicationId[i] : ApplicationId + "_" + i.ToString()
        );
        element.GSADummy = GSADummy;
        element.GSAMeshSize = GSAMeshSize;
        elements.Add(element);
      }

      return elements.ToArray();
    }

    public override void Scale(double factor)
    {
      for (var i = 0; i < Value.Count(); i++)
        Value[i] *= factor;

      if (Offset != null)
        for (var i = 0; i < Offset.Count(); i++)
          Offset[i].Scale(factor);

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
      for (var i = 0; i < Vertices.Count(); i++)
        Vertices[i] *= factor;

      Offset *= factor;

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }
  }

  public partial class Structural2DElementMesh
  {
    public Structural2DElementMesh() { }

    public Structural2DElementMesh(double[] vertices, int[] faces, int[] colors, Structural2DElementType elementType, string propertyRef, StructuralAxis[] axis, double[] offset, string applicationId = null, double meshSize = 0, Dictionary<string, object> properties = null)
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
      Axis = axis.ToList();
      if (offset != null && offset.Count() > 0)
        Offset = offset.ToList();
      GSAMeshSize = meshSize;
      ApplicationId = applicationId;
      TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DElementMesh(double[] edgeVertices, int? color, Structural2DElementType elementType, string propertyRef, StructuralAxis[] axis, double[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (properties != null)
      {
        Properties = properties;
      }
      Vertices = edgeVertices.ToList();

      // Perform mesh making
      var faces = SplitMesh(
          edgeVertices,
          (Enumerable.Range(0, edgeVertices.Count() / 3).ToArray()));

      Faces = new List<int>();

      foreach (var face in faces)
      {
        Faces.Add(face.Count() - 3);
        Faces.AddRange(face);
      }

      if (color != null)
        Colors = Enumerable.Repeat(color.Value, Vertices.Count() / 3).ToList();
      else
        Colors = new List<int>();

      ElementType = elementType;
      PropertyRef = propertyRef;
      if (axis != null)
        Axis = axis.ToList();
      if (offset != null && offset.Count() > 0)
        Offset = offset.ToList();
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
            ElementApplicationId != null && ElementApplicationId.Count() > faceCounter ? ElementApplicationId[faceCounter] : ApplicationId + "_" + faceCounter.ToString()
        );
        element.GSADummy = GSADummy;
        element.GSAMeshSize = GSAMeshSize;
        elements.Add(element);

        faceCounter++;
      }

      return elements.ToArray();
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
      for (var i = 0; i < Vertices.Count(); i++)
        Vertices[i] *= factor;

      if (Offset != null && Offset.Count() > 0)
        for (var i = 0; i < Offset.Count(); i++)
          Offset[i] *= factor;

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }

    //TODO: These methods need to be disintegrated 
    #region Mesh Generation Helper Functions
    private static List<List<int>> SplitMesh(double[] coordinates, int[] mesh)
    {
      if (mesh.Length <= 3) return new List<List<int>>() { mesh.ToList() };

      // Need to ensure same area!
      var currArea = IntegrateHasher(coordinates, mesh);

      // Assume area doesn't twist on itself
      if (currArea < 0)
      {
        mesh = mesh.Reverse().ToArray();
        currArea *= -1;
      }

      var indexToCut = 0;
      var numCut = 3;
      var bestCost = currArea * 10; // TODO: figure out a better way
      var newFace1 = new List<int>();
      var newFace2 = new List<int>();

      do
      {
        var face1 = mesh.Take(numCut).ToList();
        var face2 = mesh.Skip(numCut - 1).ToList();
        face2.Add(mesh[0]);

        var cost1 = IntegrateHasher(coordinates, face1.ToArray());
        var cost2 = IntegrateHasher(coordinates, face2.ToArray());

        if (cost1 > 0 && cost2 > 0)
        {
          // Check to make sure that the new region does not encompass the other's points
          var flag = false;
          for (var i = 1; i < face2.Count() - 1; i++)
          {
            if (InTri(coordinates, face1.ToArray(), face2[i]))
            {
              flag = true;
              break;
            }
          }

          if (!flag)
          {
            var cost = Math.Abs(cost1 + cost2 - currArea);
            if (bestCost > cost)
            {
              // Track best solution
              bestCost = cost;
              newFace1 = face1;
              newFace2 = face2;
            }
          }
        }

        mesh = mesh.Skip(1).Take(mesh.Count() - 1).Concat(new int[] { mesh[0] }).ToArray();
        indexToCut++;

        if (indexToCut >= mesh.Count())
          break;

      } while (bestCost > 1e-10);

      var returnVals = new List<List<int>>();
      if (newFace1.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace1.ToArray()));
      if (newFace2.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace2.ToArray()));
      return returnVals;
    }

    private static double IntegrateHasher(double[] coordinates, int[] vertices)
    {
      // Get coordinates
      var x = new List<double>();
      var y = new List<double>();
      var z = new List<double>();

      foreach (var e in vertices)
      {
        x.Add(coordinates[e * 3]);
        y.Add(coordinates[e * 3 + 1]);
        z.Add(coordinates[e * 3 + 2]);
      }

      // Close the loop
      x.Add(x[0]);
      y.Add(y[0]);
      z.Add(z[0]);

      //Integrate
      double area1 = 0;
      for (var i = 0; i < x.Count() - 1; i++)
        area1 += x[i] * y[i + 1] - y[i] * x[i + 1];

      if (Math.Abs(area1) > 1e-16) return area1;

      //Integrate
      double area2 = 0;
      for (var i = 0; i < x.Count() - 1; i++)
        area2 += x[i] * z[i + 1] - z[i] * x[i + 1];

      if (Math.Abs(area2) > 1e-16) return area2;

      //Integrate
      double area3 = 0;
      for (var i = 0; i < y.Count() - 1; i++)
        area3 += y[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area3) > 1e-16) return area3;

      return 0;
    }

    public static bool InTri(double[] coordinates, int[] tri, int point)
    {
      // Get coordinates
      var p0 = new Point3D(coordinates[tri[0] * 3], coordinates[tri[0] * 3 + 1], coordinates[tri[0] * 3 + 2]);
      var p1 = new Point3D(coordinates[tri[1] * 3], coordinates[tri[1] * 3 + 1], coordinates[tri[1] * 3 + 2]);
      var p2 = new Point3D(coordinates[tri[2] * 3], coordinates[tri[2] * 3 + 1], coordinates[tri[2] * 3 + 2]);
      var p = new Point3D(coordinates[point * 3], coordinates[point * 3 + 1], coordinates[point * 3 + 2]);

      var u = Point3D.Subtract(p1, p0);
      var v = Point3D.Subtract(p2, p0);
      var n = Vector3D.CrossProduct(u, v);
      var w = Point3D.Subtract(p, p0);

      var gamma = Vector3D.DotProduct(Vector3D.CrossProduct(u, w), n) / (n.Length * n.Length);
      var beta = Vector3D.DotProduct(Vector3D.CrossProduct(w, v), n) / (n.Length * n.Length);
      var alpha = 1 - gamma - beta;

      if (alpha >= 0 && beta >= 0 && gamma >= 0 && alpha <= 1 && beta <= 1 && gamma <= 1)
        return true;
      else
        return false;
    }
    #endregion
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
      Vertices = edgeVertices.ToList();

      // Perform mesh making
      var faces = SplitMesh(
          edgeVertices,
          (Enumerable.Range(0, edgeVertices.Count() / 3).ToArray()));

      Faces = new List<int>();

      foreach (var face in faces)
      {
        Faces.Add(face.Count() - 3);
        Faces.AddRange(face);
      }

      if (color != null)
        Colors = Enumerable.Repeat(color.Value, Vertices.Count() / 3).ToList();
      else
        Colors = new List<int>();

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
      for (var i = 0; i < Vertices.Count(); i++)
        Vertices[i] *= factor;

      Helper.ScaleProperties(Properties, factor);
      GenerateHash();
    }

    //TODO: These methods need to be disintegrated 
    #region Mesh Generation Helper Functions
    private static List<List<int>> SplitMesh(double[] coordinates, int[] mesh)
    {
      if (mesh.Length <= 3) return new List<List<int>>() { mesh.ToList() };

      // Need to ensure same area!
      var currArea = IntegrateHasher(coordinates, mesh);

      // Assume area doesn't twist on itself
      if (currArea < 0)
      {
        mesh = mesh.Reverse().ToArray();
        currArea *= -1;
      }

      var indexToCut = 0;
      var numCut = 3;
      var bestCost = currArea * 10; // TODO: figure out a better way
      var newFace1 = new List<int>();
      var newFace2 = new List<int>();

      do
      {
        var face1 = mesh.Take(numCut).ToList();
        var face2 = mesh.Skip(numCut - 1).ToList();
        face2.Add(mesh[0]);

        var cost1 = IntegrateHasher(coordinates, face1.ToArray());
        var cost2 = IntegrateHasher(coordinates, face2.ToArray());

        if (cost1 > 0 && cost2 > 0)
        {
          // Check to make sure that the new region does not encompass the other's points
          var flag = false;
          for (var i = 1; i < face2.Count() - 1; i++)
          {
            if (InTri(coordinates, face1.ToArray(), face2[i]))
            {
              flag = true;
              break;
            }
          }

          if (!flag)
          {
            var cost = Math.Abs(cost1 + cost2 - currArea);
            if (bestCost > cost)
            {
              // Track best solution
              bestCost = cost;
              newFace1 = face1;
              newFace2 = face2;
            }
          }
        }

        mesh = mesh.Skip(1).Take(mesh.Count() - 1).Concat(new int[] { mesh[0] }).ToArray();
        indexToCut++;

        if (indexToCut >= mesh.Count())
          break;

      } while (bestCost > 1e-10);

      var returnVals = new List<List<int>>();
      if (newFace1.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace1.ToArray()));
      if (newFace2.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace2.ToArray()));
      return returnVals;
    }

    private static double IntegrateHasher(double[] coordinates, int[] vertices)
    {
      // Get coordinates
      var x = new List<double>();
      var y = new List<double>();
      var z = new List<double>();

      foreach (var e in vertices)
      {
        x.Add(coordinates[e * 3]);
        y.Add(coordinates[e * 3 + 1]);
        z.Add(coordinates[e * 3 + 2]);
      }

      // Close the loop
      x.Add(x[0]);
      y.Add(y[0]);
      z.Add(z[0]);

      //Integrate
      double area1 = 0;
      for (var i = 0; i < x.Count() - 1; i++)
        area1 += x[i] * y[i + 1] - y[i] * x[i + 1];

      if (Math.Abs(area1) > 1e-16) return area1;

      //Integrate
      double area2 = 0;
      for (var i = 0; i < x.Count() - 1; i++)
        area2 += x[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area2) > 1e-16) return area2;

      //Integrate
      double area3 = 0;
      for (var i = 0; i < y.Count() - 1; i++)
        area3 += y[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area3) > 1e-16) return area3;

      return 0;
    }

    public static bool InTri(double[] coordinates, int[] tri, int point)
    {
      // Get coordinates
      var p0 = new Point3D(coordinates[tri[0] * 3], coordinates[tri[0] * 3 + 1], coordinates[tri[0] * 3 + 2]);
      var p1 = new Point3D(coordinates[tri[1] * 3], coordinates[tri[1] * 3 + 1], coordinates[tri[1] * 3 + 2]);
      var p2 = new Point3D(coordinates[tri[2] * 3], coordinates[tri[2] * 3 + 1], coordinates[tri[2] * 3 + 2]);
      var p = new Point3D(coordinates[point * 3], coordinates[point * 3 + 1], coordinates[point * 3 + 2]);

      var u = Point3D.Subtract(p1, p0);
      var v = Point3D.Subtract(p2, p0);
      var n = Vector3D.CrossProduct(u, v);
      var w = Point3D.Subtract(p, p0);

      var gamma = Vector3D.DotProduct(Vector3D.CrossProduct(u, w), n) / (n.Length * n.Length);
      var beta = Vector3D.DotProduct(Vector3D.CrossProduct(w, v), n) / (n.Length * n.Length);
      var alpha = 1 - gamma - beta;

      if (alpha >= 0 & beta >= 0 & gamma >= 0 & alpha <= 1 & beta <= 1 & gamma <= 1)
        return true;
      else
        return false;
    }
    #endregion
  }
}
