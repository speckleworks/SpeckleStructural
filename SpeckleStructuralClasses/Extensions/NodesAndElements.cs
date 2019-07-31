using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SpeckleCore;
using SpeckleCoreGeometryClasses;

namespace SpeckleStructuralClasses
{
  public partial class StructuralNode
  {
    public StructuralNode() { }

    public StructuralNode(double[] value, StructuralAxis axis, StructuralVectorBoolSix restraint, StructuralVectorSix stiffness, double mass, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Value = value.ToList();
      this.Axis = axis;
      this.Restraint = restraint;
      this.Stiffness = stiffness;
      this.Mass = mass;
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DElement
  {
    public Structural1DElement() { }

    public Structural1DElement(double[] value, Structural1DElementType elementType, string propertyRef, StructuralVectorThree zAxis, StructuralVectorBoolSix[] endRelease, StructuralVectorThree[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Value = value.ToList();
      this.ElementType = elementType;
      this.PropertyRef = propertyRef;
      this.ZAxis = zAxis;
      this.EndRelease = endRelease == null ? null : endRelease.ToList();
      this.Offset = offset == null ? null : offset.ToList();
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      if (Offset != null)
        for (int i = 0; i < this.Offset.Count(); i++)
          this.Offset[i].Scale(factor);

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DSpring
  {
    public Structural1DSpring() { }

    public Structural1DSpring(double[] value, string propertyRef, StructuralVectorThree zAxis, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Value = value.ToList();
      this.PropertyRef = propertyRef;
      this.ZAxis = zAxis;
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural1DElementPolyline
  {
    public Structural1DElementPolyline() { }

    public Structural1DElementPolyline(double[] value, Structural1DElementType elementType, string propertyRef, StructuralVectorThree[] zAxis, StructuralVectorBoolSix[] endRelease, StructuralVectorThree[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Value = value.ToList();
      this.ElementType = elementType;
      this.PropertyRef = propertyRef;
      this.ZAxis = zAxis == null ? null : zAxis.ToList();
      this.EndRelease = endRelease == null ? null : endRelease.ToList();
      this.Offset = offset == null ? null : offset.ToList();
      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public Structural1DElementPolyline(Structural1DElement[] elements, string applicationId = null, Dictionary<string, object> properties = null)
    {
      if (elements.Length == 0)
        throw new Exception("No elements specified.");

      this.Properties = properties;
      this.Value = new List<double>(elements[0].Value.Take(3).ToArray());
      this.ElementType = elements[0].ElementType;
      this.PropertyRef = elements[0].PropertyRef;
      this.ZAxis = new List<StructuralVectorThree>();
      this.EndRelease = new List<StructuralVectorBoolSix>();
      this.Offset = new List<StructuralVectorThree>();

      foreach (Structural1DElement element in elements)
      {
        if (this.ElementType != element.ElementType)
          throw new Exception("Different ElementTypes.");

        if (this.PropertyRef != element.PropertyRef)
          throw new Exception("Different PropertyRef.");

        if (!this.Value.Skip(this.Value.Count() - 3).Take(3).SequenceEqual(element.Value.Take(3)))
          throw new Exception("Elements not continuous.");

        this.Value.AddRange(element.Value.Skip(3).Take(3));
        this.ZAxis.Add(element.ZAxis);
        this.EndRelease.AddRange(element.EndRelease);
        this.Offset.AddRange(element.Offset);
      }

      this.ApplicationId = applicationId;

      GenerateHash();
    }

    public Structural1DElement[] Explode()
    {
      List<Structural1DElement> elements = new List<Structural1DElement>();

      for (int i = 0; i < Value.Count() / 3 - 1; i++)
      {
        Structural1DElement element = new Structural1DElement(
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
      for (int i = 0; i < this.Value.Count(); i++)
        this.Value[i] *= factor;

      if (this.Offset != null)
        for (int i = 0; i < this.Offset.Count(); i++)
          this.Offset[i].Scale(factor);

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DElement
  {
    public Structural2DElement() { }

    public Structural2DElement(double[] vertices, int[] faces, int[] colors, Structural2DElementType elementType, string propertyRef, StructuralAxis axis, double offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Vertices = vertices.ToList();
      this.Faces = faces.ToList();
      this.Colors = colors == null ? null : colors.ToList();
      this.ElementType = elementType;
      this.PropertyRef = propertyRef;
      this.Axis = axis;
      this.Offset = offset;
      this.ApplicationId = applicationId;

      this.TextureCoordinates = null;

      GenerateHash();
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Vertices.Count(); i++)
        this.Vertices[i] *= factor;

      this.Offset *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }
  }

  public partial class Structural2DElementMesh
  {
    public Structural2DElementMesh() { }

    public Structural2DElementMesh(double[] vertices, int[] faces, int[] colors, Structural2DElementType elementType, string propertyRef, StructuralAxis[] axis, double[] offset, string applicationId = null, double meshSize = 0, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Vertices = vertices.ToList();
      this.Faces = faces.ToList();
      this.Colors = colors == null ? null : colors.ToList();
      this.ElementType = elementType;
      this.PropertyRef = propertyRef;
      this.Axis = axis.ToList();
      this.Offset = offset.ToList();
      this.GSAMeshSize = meshSize;
      this.ApplicationId = applicationId;
      this.TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DElementMesh(double[] edgeVertices, int? color, Structural2DElementType elementType, string propertyRef, StructuralAxis[] axis, double[] offset, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Vertices = edgeVertices.ToList();

      // Perform mesh making
      List<List<int>> faces = SplitMesh(
          edgeVertices,
          (Enumerable.Range(0, edgeVertices.Count() / 3).ToArray()));

      this.Faces = new List<int>();

      foreach (List<int> face in faces)
      {
        this.Faces.Add(face.Count() - 3);
        this.Faces.AddRange(face);
      }

      if (color != null)
        this.Colors = Enumerable.Repeat(color.Value, this.Vertices.Count() / 3).ToList();
      else
        this.Colors = new List<int>();

      this.ElementType = elementType;
      this.PropertyRef = propertyRef;
      if (axis != null)
        this.Axis = axis.ToList();
      if (offset != null)
        this.Offset = offset.ToList();
      this.ApplicationId = applicationId;

      this.TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DElement[] Explode()
    {
      List<Structural2DElement> elements = new List<Structural2DElement>();

      int faceCounter = 0;

      for (int i = 0; i < Faces.Count(); i++)
      {
        List<double> vertices = new List<double>();
        List<int> colors = new List<int>();

        int numVertices = Faces[i++] + 3;
        for (int j = 0; j < numVertices; j++)
        {
          if (Colors != null && Colors.Count() > Faces[i])
          {
            colors.Add(Colors[Faces[i]]);
          }
          vertices.AddRange(Vertices.Skip(Faces[i++] * 3).Take(3));
        }
        i--;

        Structural2DElement element = new Structural2DElement(
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
      List<int[]> edgeConnectivities = new List<int[]>();

      // Get face connectivities and close loop
      List<int[]> faceConnnectivities = new List<int[]>();
      for (int i = 0; i < Faces.Count(); i++)
      {
        int numVertices = Faces[i] + 3;
        i++;
        faceConnnectivities.Add(Faces.Skip(i).Take(numVertices).Concat(Faces.Skip(i).Take(1)).ToArray());
        i += numVertices - 1;
      }

      // Get distinct edges
      List<Tuple<int, int, string, string>> edges = new List<Tuple<int, int, string, string>>();

      foreach (int[] conn in faceConnnectivities)
      {
        for (int i = 0; i < conn.Length - 1; i++)
        {
          string c1 = string.Join(",", this.Vertices.Skip(conn[i] * 3).Take(3).Select(x => Math.Round(x, 4).ToString()));
          string c2 = string.Join(",", this.Vertices.Skip(conn[i + 1] * 3).Take(3).Select(x => Math.Round(x, 4).ToString()));

          if (edges.Any(e => (e.Item3 == c1 & e.Item4 == c2) |
              (e.Item3 == c2 & e.Item4 == c1)))
          {
            edges.RemoveAll(x => (x.Item3 == c1 && x.Item4 == c2) || (x.Item3 == c2 && x.Item4 == c1));
          }
          else
          {
            if (conn[i] < conn[i + 1])
              edges.Add(new Tuple<int, int, string, string>(conn[i], conn[i + 1], c1, c2));
            else
              edges.Add(new Tuple<int, int, string, string>(conn[i + 1], conn[i], c2, c1));
          }
        }
      }

      // Reorder the edges
      List<int> currentLoop = new List<int>();
      List<string> flatCoor = new List<string>();
      currentLoop.Add(edges[0].Item1);
      currentLoop.Add(edges[0].Item2);
      flatCoor.Add(edges[0].Item3);
      flatCoor.Add(edges[0].Item4);

      edges.RemoveAt(0);

      while (edges.Count > 0)
      {
        string commonVertex = flatCoor.Last();

        List<Tuple<int, int, string, string>> nextEdge = edges.Where(e => e.Item3 == commonVertex | e.Item4 == commonVertex).ToList();

        if (nextEdge.Count > 0)
        {
          currentLoop.Add(nextEdge[0].Item3 == commonVertex ? nextEdge[0].Item2 : nextEdge[0].Item1);
          flatCoor.Add(nextEdge[0].Item3 == commonVertex ? nextEdge[0].Item4 : nextEdge[0].Item3);
          edges.Remove(nextEdge[0]);
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

      return edgeConnectivities;
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Vertices.Count(); i++)
        this.Vertices[i] *= factor;

      if (this.Offset != null)
        for (int i = 0; i < this.Offset.Count(); i++)
          this.Offset[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }

    //TODO: These methods need to be disintegrated 
    #region Mesh Generation Helper Functions
    private static List<List<int>> SplitMesh(double[] coordinates, int[] mesh)
    {
      if (mesh.Length <= 3) return new List<List<int>>() { mesh.ToList() };

      // Need to ensure same area!
      double currArea = IntegrateHasher(coordinates, mesh);

      // Assume area doesn't twist on itself
      if (currArea < 0)
      {
        mesh = mesh.Reverse().ToArray();
        currArea *= -1;
      }

      int indexToCut = 0;
      int numCut = 3;
      double bestCost = currArea * 10; // TODO: figure out a better way
      List<int> newFace1 = new List<int>();
      List<int> newFace2 = new List<int>();

      do
      {
        List<int> face1 = mesh.Take(numCut).ToList();
        List<int> face2 = mesh.Skip(numCut - 1).ToList();
        face2.Add(mesh[0]);

        double cost1 = IntegrateHasher(coordinates, face1.ToArray());
        double cost2 = IntegrateHasher(coordinates, face2.ToArray());

        if (cost1 > 0 & cost2 > 0)
        {
          // Check to make sure that the new region does not encompass the other's points
          bool flag = false;
          for (int i = 1; i < face2.Count() - 1; i++)
          {
            if (InTri(coordinates, face1.ToArray(), face2[i]))
            {
              flag = true;
              break;
            }
          }

          if (!flag)
          {
            double cost = Math.Abs(cost1 + cost2 - currArea);
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

      List<List<int>> returnVals = new List<List<int>>();
      if (newFace1.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace1.ToArray()));
      if (newFace2.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace2.ToArray()));
      return returnVals;
    }

    private static double IntegrateHasher(double[] coordinates, int[] vertices)
    {
      // Get coordinates
      List<double> x = new List<double>();
      List<double> y = new List<double>();
      List<double> z = new List<double>();

      foreach (int e in vertices)
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
      for (int i = 0; i < x.Count() - 1; i++)
        area1 += x[i] * y[i + 1] - y[i] * x[i + 1];

      if (Math.Abs(area1) > 1e-16) return area1;

      //Integrate
      double area2 = 0;
      for (int i = 0; i < x.Count() - 1; i++)
        area2 += x[i] * z[i + 1] - z[i] * x[i + 1];

      if (Math.Abs(area2) > 1e-16) return area2;

      //Integrate
      double area3 = 0;
      for (int i = 0; i < y.Count() - 1; i++)
        area3 += y[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area3) > 1e-16) return area3;

      return 0;
    }

    public static bool InTri(double[] coordinates, int[] tri, int point)
    {
      // Get coordinates
      Point3D p0 = new Point3D(coordinates[tri[0] * 3], coordinates[tri[0] * 3 + 1], coordinates[tri[0] * 3 + 2]);
      Point3D p1 = new Point3D(coordinates[tri[1] * 3], coordinates[tri[1] * 3 + 1], coordinates[tri[1] * 3 + 2]);
      Point3D p2 = new Point3D(coordinates[tri[2] * 3], coordinates[tri[2] * 3 + 1], coordinates[tri[2] * 3 + 2]);
      Point3D p = new Point3D(coordinates[point * 3], coordinates[point * 3 + 1], coordinates[point * 3 + 2]);

      Vector3D u = Point3D.Subtract(p1, p0);
      Vector3D v = Point3D.Subtract(p2, p0);
      Vector3D n = Vector3D.CrossProduct(u, v);
      Vector3D w = Point3D.Subtract(p, p0);

      double gamma = Vector3D.DotProduct(Vector3D.CrossProduct(u, w), n) / (n.Length * n.Length);
      double beta = Vector3D.DotProduct(Vector3D.CrossProduct(w, v), n) / (n.Length * n.Length);
      double alpha = 1 - gamma - beta;

      if (alpha >= 0 & beta >= 0 & gamma >= 0 & alpha <= 1 & beta <= 1 & gamma <= 1)
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
      this.Properties = properties;
      this.Vertices = vertices.ToList();
      this.Faces = faces.ToList();
      this.Colors = colors == null ? null : colors.ToList();
      this.ApplicationId = applicationId;

      this.TextureCoordinates = null;

      GenerateHash();
    }

    public Structural2DVoid(double[] edgeVertices, int? color, string applicationId = null, Dictionary<string, object> properties = null)
    {
      this.Properties = properties;
      this.Vertices = edgeVertices.ToList();

      // Perform mesh making
      List<List<int>> faces = SplitMesh(
          edgeVertices,
          (Enumerable.Range(0, edgeVertices.Count() / 3).ToArray()));

      this.Faces = new List<int>();

      foreach (List<int> face in faces)
      {
        this.Faces.Add(face.Count() - 3);
        this.Faces.AddRange(face);
      }

      if (color != null)
        this.Colors = Enumerable.Repeat(color.Value, this.Vertices.Count() / 3).ToList();
      else
        this.Colors = new List<int>();

      this.ApplicationId = applicationId;

      this.TextureCoordinates = null;

      GenerateHash();
    }

    public List<int[]> Edges()
    {
      List<int[]> edgeConnectivities = new List<int[]>();

      // Get face connectivities and close loop
      List<int[]> faceConnnectivities = new List<int[]>();
      for (int i = 0; i < Faces.Count(); i++)
      {
        int numVertices = Faces[i] + 3;
        i++;
        faceConnnectivities.Add(Faces.Skip(i).Take(numVertices).Concat(Faces.Skip(i).Take(1)).ToArray());
        i += numVertices - 1;
      }

      // Get distinct edges
      List<Tuple<int, int>> edges = new List<Tuple<int, int>>();

      foreach (int[] conn in faceConnnectivities)
      {
        for (int i = 0; i < conn.Length - 1; i++)
        {
          if (edges.Any(e => (e.Item1 == conn[i] & e.Item2 == conn[i + 1]) |
              (e.Item1 == conn[i + 1] & e.Item2 == conn[i])))
          {
            edges.Remove(new Tuple<int, int>(conn[i], conn[i + 1]));
            edges.Remove(new Tuple<int, int>(conn[i + 1], conn[i]));
          }
          else
          {
            if (conn[i] < conn[i + 1])
              edges.Add(new Tuple<int, int>(conn[i], conn[i + 1]));
            else
              edges.Add(new Tuple<int, int>(conn[i + 1], conn[i]));
          }
        }
      }

      // Reorder the edges
      List<int> currentLoop = new List<int>();
      currentLoop.Add(edges[0].Item1);
      currentLoop.Add(edges[0].Item2);

      edges.RemoveAt(0);

      while (edges.Count > 0)
      {
        int commonVertex = currentLoop[currentLoop.Count() - 1];

        List<Tuple<int, int>> nextEdge = edges.Where(e => e.Item1 == commonVertex | e.Item2 == commonVertex).ToList();

        if (nextEdge.Count > 0)
        {
          currentLoop.Add(nextEdge[0].Item1 == commonVertex ? nextEdge[0].Item2 : nextEdge[0].Item1);
          edges.Remove(nextEdge[0]);
        }
        else
        {
          // Next edge not found. Stop looking for more
          break;
        }

        if (currentLoop[0] == currentLoop[currentLoop.Count() - 1])
        {
          currentLoop.RemoveAt(0);

          edgeConnectivities.Add(currentLoop.ToArray());
          currentLoop = new List<int>();

          if (edges.Count > 0)
          {
            currentLoop.Add(edges[0].Item1);
            currentLoop.Add(edges[0].Item2);
            edges.RemoveAt(0);
          }
        }
      }

      return edgeConnectivities;
    }

    public override void Scale(double factor)
    {
      for (int i = 0; i < this.Vertices.Count(); i++)
        this.Vertices[i] *= factor;

      this.Properties = ScaleProperties(this.Properties, factor);
      this.GenerateHash();
    }

    //TODO: These methods need to be disintegrated 
    #region Mesh Generation Helper Functions
    private static List<List<int>> SplitMesh(double[] coordinates, int[] mesh)
    {
      if (mesh.Length <= 3) return new List<List<int>>() { mesh.ToList() };

      // Need to ensure same area!
      double currArea = IntegrateHasher(coordinates, mesh);

      // Assume area doesn't twist on itself
      if (currArea < 0)
      {
        mesh = mesh.Reverse().ToArray();
        currArea *= -1;
      }

      int indexToCut = 0;
      int numCut = 3;
      double bestCost = currArea * 10; // TODO: figure out a better way
      List<int> newFace1 = new List<int>();
      List<int> newFace2 = new List<int>();

      do
      {
        List<int> face1 = mesh.Take(numCut).ToList();
        List<int> face2 = mesh.Skip(numCut - 1).ToList();
        face2.Add(mesh[0]);

        double cost1 = IntegrateHasher(coordinates, face1.ToArray());
        double cost2 = IntegrateHasher(coordinates, face2.ToArray());

        if (cost1 > 0 & cost2 > 0)
        {
          // Check to make sure that the new region does not encompass the other's points
          bool flag = false;
          for (int i = 1; i < face2.Count() - 1; i++)
          {
            if (InTri(coordinates, face1.ToArray(), face2[i]))
            {
              flag = true;
              break;
            }
          }

          if (!flag)
          {
            double cost = Math.Abs(cost1 + cost2 - currArea);
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

      List<List<int>> returnVals = new List<List<int>>();
      if (newFace1.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace1.ToArray()));
      if (newFace2.Count() > 0)
        returnVals.AddRange(SplitMesh(coordinates, newFace2.ToArray()));
      return returnVals;
    }

    private static double IntegrateHasher(double[] coordinates, int[] vertices)
    {
      // Get coordinates
      List<double> x = new List<double>();
      List<double> y = new List<double>();
      List<double> z = new List<double>();

      foreach (int e in vertices)
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
      for (int i = 0; i < x.Count() - 1; i++)
        area1 += x[i] * y[i + 1] - y[i] * x[i + 1];

      if (Math.Abs(area1) > 1e-16) return area1;

      //Integrate
      double area2 = 0;
      for (int i = 0; i < x.Count() - 1; i++)
        area2 += x[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area2) > 1e-16) return area2;

      //Integrate
      double area3 = 0;
      for (int i = 0; i < y.Count() - 1; i++)
        area3 += y[i] * z[i + 1] - z[i] * y[i + 1];

      if (Math.Abs(area3) > 1e-16) return area3;

      return 0;
    }

    public static bool InTri(double[] coordinates, int[] tri, int point)
    {
      // Get coordinates
      Point3D p0 = new Point3D(coordinates[tri[0] * 3], coordinates[tri[0] * 3 + 1], coordinates[tri[0] * 3 + 2]);
      Point3D p1 = new Point3D(coordinates[tri[1] * 3], coordinates[tri[1] * 3 + 1], coordinates[tri[1] * 3 + 2]);
      Point3D p2 = new Point3D(coordinates[tri[2] * 3], coordinates[tri[2] * 3 + 1], coordinates[tri[2] * 3 + 2]);
      Point3D p = new Point3D(coordinates[point * 3], coordinates[point * 3 + 1], coordinates[point * 3 + 2]);

      Vector3D u = Point3D.Subtract(p1, p0);
      Vector3D v = Point3D.Subtract(p2, p0);
      Vector3D n = Vector3D.CrossProduct(u, v);
      Vector3D w = Point3D.Subtract(p, p0);

      double gamma = Vector3D.DotProduct(Vector3D.CrossProduct(u, w), n) / (n.Length * n.Length);
      double beta = Vector3D.DotProduct(Vector3D.CrossProduct(w, v), n) / (n.Length * n.Length);
      double alpha = 1 - gamma - beta;

      if (alpha >= 0 & beta >= 0 & gamma >= 0 & alpha <= 1 & beta <= 1 & gamma <= 1)
        return true;
      else
        return false;
    }
    #endregion
  }
}
