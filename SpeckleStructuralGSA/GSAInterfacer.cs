  using Interop.Gsa_10_0;
  using SpeckleCore;
  using SpeckleCoreGeometryClasses;
  using SpeckleStructuralClasses;
  using SQLite;
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Reflection;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;
  using System.Windows.Media.Media3D;

namespace SpeckleStructuralGSA
{
  public class GSAInterfacer
  {
    public IComAuto GSAObject;

    public Indexer Indexer = new Indexer();

    private Dictionary<string, object> PreviousGSAGetCache = new Dictionary<string, object>();
    private Dictionary<string, object> GSAGetCache = new Dictionary<string, object>();

    private Dictionary<string, object> PreviousGSASetCache = new Dictionary<string, object>();
    private Dictionary<string, object> GSASetCache = new Dictionary<string, object>();

    private Dictionary<string, string> SidCache = new Dictionary<string, string>();

    private const string SID_TAG = "speckle_app_id";

    private string PreviousGSAResultInit = "";

    public List<string> GetSetCache()
    {
      return GSASetCache.Keys.Where(k => k.Contains(SID_TAG)).ToList();
    }

    #region Communication
    public void InitializeReceiver(IComAuto GSAObject)
    {
      FullClearCache();
      this.GSAObject = GSAObject;
    }

    public void PreReceiving()
    {
      ClearCache();
    }

    public void PostReceiving()
    {
      BlankDepreciatedGWASetCommands();
    }

    public void InitializeSender(IComAuto GSAObject)
    {
      FullClearCache();
      this.GSAObject = GSAObject;
    }

    public void PreSending()
    {
      ClearCache();
    }

    public void PostSending()
    {

    }
    #endregion

    #region GWA Command
    /// <summary>
    /// Returns a list of GWA records with the index of the record prepended.
    /// </summary>
    /// <param name="command">GET GWA command</param>
    /// <returns>Array of GWA records</returns>
    public string[] GetGWARecords(string command)
    {
      if (!command.StartsWith("GET"))
        throw new Exception("GetGWAGetCommands() only takes in GET commands");

      object result = RunGWACommand(command);
      string[] newPieces = ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();
      return newPieces;
    }

    public string[][] GetSplitGWARecordsByKeyword(string keyword)
    {
      var gwaResults = GetGWARecords("GET_ALL\t" + keyword);
      var numRows = gwaResults.Count();
      if (numRows == 0)
        return null;

      var splitRecords = new string[numRows][];

      for (var i = 0; i < numRows; i++)
      {
        var pieces = ((string)gwaResults[i]).Split(new string[] { "\t" }, StringSplitOptions.None);  //Empty values should be preserved
        if (pieces.Count() > 0)
          splitRecords[i] = pieces;
      }

      return splitRecords;
    }

    /// <summary>
    /// Returns a list of new GWA records with the index of the record prepended.
    /// </summary>
    /// <param name="command">GET GWA command</param>
    /// <returns>Array of GWA records</returns>
    public string[] GetNewGWARecords(string command)
    {
      if (!command.StartsWith("GET"))
        throw new Exception("GetNewGWAGetCommands() only takes in GET commands");

      object result = RunGWACommand(command);

      if (PreviousGSAGetCache.ContainsKey(command))
      {
        if ((result as string) == (PreviousGSAGetCache[command] as string))
          return new string[0];

        string[] newPieces = ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();
        string[] prevPieces = ((string)PreviousGSAGetCache[command]).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();

        string[] ret = newPieces.Where(n => !prevPieces.Contains(n)).ToArray();

        return ret;
      }
      else
      {
        return ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();
      }
    }

    /// <summary>
    /// Returns a list of deleted GWA records with the index of the record prepended.
    /// </summary>
    /// <param name="command">GET GWA command</param>
    /// <returns>Array of GWA records</returns>
    public string[] GetDeletedGWARecords(string command)
    {
      if (!command.Contains("GET"))
        throw new Exception("GetDeletedGWAGetCommands() only takes in GET commands");

      object result = RunGWACommand(command);

      if (PreviousGSAGetCache.ContainsKey(command))
      {
        if ((result as string) == (PreviousGSAGetCache[command] as string))
          return new string[0];

        string[] newPieces = ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();
        string[] prevPieces = ((string)PreviousGSAGetCache[command]).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();

        string[] ret = prevPieces.Where(p => !newPieces.Contains(p)).ToArray();

        return ret;
      }
      else
        return new string[0];
    }

    /// <summary>
    /// Runs a GWA command with the option to cache GET and SET commands.
    /// </summary>
    /// <param name="command">GWA command</param>
    /// <param name="cache">Use cache</param>
    /// <returns>GWA command return object</returns>
    public object RunGWACommand(string command, bool cache = true)
    {
      if (cache)
      {
        if (command.StartsWith("GET"))
        {
          if (!GSAGetCache.ContainsKey(command))
          {
            // Let's speed things up a bit
            var commandPieces = command.Split(new char[] { '\t' });
            var newCommand = "GET_ALL\t" + commandPieces[1];

            GSAGetCache[newCommand] = GSAObject.GwaCommand(newCommand);

            var allRecords = ((string)GSAGetCache[newCommand]).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rec in allRecords)
            {
              var recPieces = rec.Split(new char[] { '\t' });
              GSAGetCache["GET\t" + commandPieces[1] + "\t" + recPieces[1]] = rec;
            }
          }

          return GSAGetCache.ContainsKey(command) ? GSAGetCache[command] : "";
        }

        if (command.StartsWith("SET"))
        {
          if (PreviousGSASetCache.ContainsKey(command))
            GSASetCache[command] = PreviousGSASetCache[command];

          if (!GSASetCache.ContainsKey(command))
            GSASetCache[command] = GSAObject.GwaCommand(command);

          return GSASetCache[command];
        }
      }

      return GSAObject.GwaCommand(command);
    }

    /// <summary>
    /// BLANKS all SET GWA records which are in the previous cache, but not the current cache.
    /// </summary>
    public void BlankDepreciatedGWASetCommands()
    {
      List<string> prevSets = PreviousGSASetCache.Keys.Where(l => l.StartsWith("SET")).ToList();

      for (int i = 0; i < prevSets.Count(); i++)
      {
        string[] split = Regex.Replace(prevSets[i], ":{" + SID_TAG + ":.*}", "").ListSplit("\t");
        prevSets[i] = split[1] + "\t" + split[2] + "\t";
      }

      prevSets = prevSets.Where(l => !GSASetCache.Keys.Any(x => Regex.Replace(x, ":{" + SID_TAG + ":.*}", "").Contains(l))).ToList();

      for (int i = 0; i < prevSets.Count(); i++)
      {
        string p = prevSets[i];

        string[] split = p.ListSplit("\t");

        if (split[1].IsDigits())
        {
          // Uses SET
          if (!Indexer.InBaseline(split[0], Convert.ToInt32(split[1])))
            RunGWACommand("BLANK\t" + split[0] + "\t" + split[1], false);
        }
        else if (split[0].IsDigits())
        {

          // Uses SET_AT
          if (!Indexer.InBaseline(split[1], Convert.ToInt32(split[0])))
          {
            RunGWACommand("DELETE\t" + split[1] + "\t" + split[0], false);
            int idxShifter = Convert.ToInt32(split[0]) + 1;
            bool flag = false;
            while (!flag)
            {
              flag = true;

              prevSets = prevSets
                  .Select(line => line.Replace(
                      idxShifter.ToString() + "\t" + split[1] + "\t",
                      (idxShifter - 1).ToString() + "\t" + split[1] + "\t")).ToList();

              string target = "\t" + idxShifter.ToString() + "\t" + split[1] + "\t";
              string rep = "\t" + (idxShifter - 1).ToString() + "\t" + split[1] + "\t";

              string prevCacheKey = PreviousGSASetCache.Keys
                  .FirstOrDefault(x => x.Contains(target));
              if (prevCacheKey != null)
              {
                PreviousGSASetCache[prevCacheKey.Replace(target, rep)] = PreviousGSASetCache[prevCacheKey];
                PreviousGSASetCache.Remove(prevCacheKey);
                flag = false;
              }

              string currCacheKey = GSASetCache.Keys
                  .FirstOrDefault(x => x.Contains(target));
              if (currCacheKey != null)
              {
                GSASetCache[currCacheKey.Replace(target, rep)] = GSASetCache[currCacheKey];
                GSASetCache.Remove(currCacheKey);
                flag = false;
              }

              idxShifter++;
            }
          }
        }
        else
        {
          //Some commands - like "LOAD_GRAVITY.2" and "LOAD_2D_THERMAL.2" have no indices at all in their GWA commands
          //TODO
        }
      }
    }
    #endregion

    #region Nodes
    /// <summary>
    /// Create new node at the coordinate. If a node already exists, no new nodes are created. Updates Indexer with the index.
    /// </summary>
    /// <param name="x">X coordinate of the node</param>
    /// <param name="y">Y coordinate of the node</param>
    /// <param name="z">Z coordinate of the node</param>
    /// <param name="applicationId">Application ID of the node</param>
    /// <returns>Node index</returns>
    public int NodeAt(double x, double y, double z, double coincidentNodeAllowance, string applicationId = null)
    {
      int idx = GSAObject.Gen_NodeAt(x, y, z, coincidentNodeAllowance);

      if (applicationId != null)
        Indexer.ReserveIndicesAndMap(typeof(GSANode), new List<int>() { idx }, new List<string>() { applicationId });
      else
        Indexer.ReserveIndices(typeof(GSANode), new List<int>() { idx });

      // Add artificial cache
      string cacheKey = "SET\t" + typeof(GSANode).GetGSAKeyword() + "\t" + idx.ToString() + "\t";
      if (!GSASetCache.ContainsKey(cacheKey))
        GSASetCache[cacheKey] = 0;

      return idx;
    }
    #endregion

    #region Axis
    /// <summary>
    /// Set the structural axis in GSA and returns the index of the axis.
    /// Returns 0 for global axis.
    /// </summary>
    /// <param name="axis">Axis to set</param>
    /// <returns>Index of axis</returns>
    public int SetAxis(StructuralAxis axis, string name = "")
    {
      string gwaAxisName = name ?? "";

      if (axis.Xdir.Value.SequenceEqual(new double[] { 1, 0, 0 }) &&
          axis.Ydir.Value.SequenceEqual(new double[] { 0, 1, 0 }) &&
          axis.Normal.Value.SequenceEqual(new double[] { 0, 0, 1 }))
        return 0;

      List<string> ls = new List<string>();

      int res = Indexer.ResolveIndex("AXIS");

      ls.Add("SET");
      ls.Add("AXIS");
      ls.Add(res.ToString());
      ls.Add(gwaAxisName);
      ls.Add("CART");

      ls.Add("0");
      ls.Add("0");
      ls.Add("0");

      ls.Add(axis.Xdir.Value[0].ToString());
      ls.Add(axis.Xdir.Value[1].ToString());
      ls.Add(axis.Xdir.Value[2].ToString());

      ls.Add(axis.Ydir.Value[0].ToString());
      ls.Add(axis.Ydir.Value[1].ToString());
      ls.Add(axis.Ydir.Value[2].ToString());

      RunGWACommand(string.Join("\t", ls));

      return res;
    }

    public int SetAxis(SpeckleVector xVector, SpeckleVector xyVector, SpecklePoint origin, string name = "")
    {
      int res = Indexer.ResolveIndex("AXIS");

      string gwaAxisName = name ?? "";

      var ls = new List<string>()
        {
          "SET",
          "AXIS",
          res.ToString(),
          gwaAxisName,
          "CART",

          origin.Value[0].ToString(),
          origin.Value[1].ToString(),
          origin.Value[2].ToString(),

          xVector.Value[0].ToString(),
          xVector.Value[1].ToString(),
          xVector.Value[2].ToString(),

          xyVector.Value[0].ToString(),
          xyVector.Value[1].ToString(),
          xyVector.Value[2].ToString(),
        };

      RunGWACommand(string.Join("\t", ls));

      return res;
    }

    /// <summary>
    /// Calculates the local axis of a 1D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="zAxis">Z axis of the 1D entity</param>
    /// <returns>Axis</returns>
    public StructuralAxis LocalAxisEntity1D(double[] coor, StructuralVectorThree zAxis)
    {
      Vector3D axisX = new Vector3D(coor[3] - coor[0], coor[4] - coor[1], coor[5] - coor[2]);
      Vector3D axisZ = new Vector3D(zAxis.Value[0], zAxis.Value[1], zAxis.Value[2]);
      Vector3D axisY = Vector3D.CrossProduct(axisZ, axisX);

      StructuralAxis axis = new StructuralAxis(
          new StructuralVectorThree(new double[] { axisX.X, axisX.Y, axisX.Z }),
          new StructuralVectorThree(new double[] { axisY.X, axisY.Y, axisY.Z }),
          new StructuralVectorThree(new double[] { axisZ.X, axisZ.Y, axisZ.Z })
      );
      axis.Normalize();
      return axis;
    }

    /// <summary>
    /// Calculates the local axis of a point from a GSA node axis.
    /// </summary>
    /// <param name="axis">ID of GSA node axis</param>
    /// <param name="gwaRecord">GWA record of AXIS if used</param>
    /// <param name="evalAtCoor">Coordinates to evaluate axis at</param>
    /// <returns>Axis</returns>
    public StructuralAxis Parse0DAxis(int axis, out string gwaRecord, double[] evalAtCoor = null)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      gwaRecord = null;

      switch (axis)
      {
        case 0:
          // Global
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 1, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 })
          );
        case -11:
          // X elevation
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 0, -1, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { -1, 0, 0 })
          );
        case -12:
          // Y elevation
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { 0, -1, 0 })
          );
        case -14:
          // Vertical
          return new StructuralAxis(
              new StructuralVectorThree(new double[] { 0, 0, 1 }),
              new StructuralVectorThree(new double[] { 1, 0, 0 }),
              new StructuralVectorThree(new double[] { 0, 1, 0 })
          );
        case -13:
          // Global cylindrical
          x = new Vector3D(evalAtCoor[0], evalAtCoor[1], 0);
          x.Normalize();
          z = new Vector3D(0, 0, 1);
          y = Vector3D.CrossProduct(z, x);

          return new StructuralAxis(
              new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
              new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
              new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
          );
        default:
          string res = GetGWARecords("GET\tAXIS\t" + axis.ToString()).FirstOrDefault();
          gwaRecord = res;

          string[] pieces = res.Split(new char[] { '\t' });
          if (pieces.Length < 13)
          {
            return new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })
            );
          }
          Vector3D origin = new Vector3D(Convert.ToDouble(pieces[4]), Convert.ToDouble(pieces[5]), Convert.ToDouble(pieces[6]));

          Vector3D X = new Vector3D(Convert.ToDouble(pieces[7]), Convert.ToDouble(pieces[8]), Convert.ToDouble(pieces[9]));
          X.Normalize();


          Vector3D Yp = new Vector3D(Convert.ToDouble(pieces[10]), Convert.ToDouble(pieces[11]), Convert.ToDouble(pieces[12]));
          Vector3D Z = Vector3D.CrossProduct(X, Yp);
          Z.Normalize();

          Vector3D Y = Vector3D.CrossProduct(Z, X);

          Vector3D pos = new Vector3D(0, 0, 0);

          if (evalAtCoor == null)
            pieces[3] = "CART";
          else
          {
            pos = new Vector3D(evalAtCoor[0] - origin.X, evalAtCoor[1] - origin.Y, evalAtCoor[2] - origin.Z);
            if (pos.Length == 0)
              pieces[3] = "CART";
          }

          switch (pieces[3])
          {
            case "CART":
              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { X.X, X.Y, X.Z }),
                  new StructuralVectorThree(new double[] { Y.X, Y.Y, Y.Z }),
                  new StructuralVectorThree(new double[] { Z.X, Z.Y, Z.Z })
              );
            case "CYL":
              x = new Vector3D(pos.X, pos.Y, 0);
              x.Normalize();
              z = Z;
              y = Vector3D.CrossProduct(Z, x);
              y.Normalize();

              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
                  new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
                  new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
              );
            case "SPH":
              x = pos;
              x.Normalize();
              z = Vector3D.CrossProduct(Z, x);
              z.Normalize();
              y = Vector3D.CrossProduct(z, x);
              z.Normalize();

              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
                  new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
                  new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
              );
            default:
              return new StructuralAxis(
                  new StructuralVectorThree(new double[] { 1, 0, 0 }),
                  new StructuralVectorThree(new double[] { 0, 1, 0 }),
                  new StructuralVectorThree(new double[] { 0, 0, 1 })
              );
          }
      }
    }

    /// <summary>
    /// Calculates the local axis of a 1D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="rotationAngle">Angle of rotation from default axis</param>
    /// <param name="orientationNode">Node to orient axis to</param>
    /// <returns>Axis</returns>
    public StructuralAxis Parse1DAxis(double[] coor, double rotationAngle = 0, double[] orientationNode = null)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      x = new Vector3D(coor[3] - coor[0], coor[4] - coor[1], coor[5] - coor[2]);
      x.Normalize();

      if (orientationNode == null)
      {
        if (x.X == 0 && x.Y == 0)
        {
          //Column
          y = new Vector3D(0, 1, 0);
          z = Vector3D.CrossProduct(x, y);
        }
        else
        {
          //Non-Vertical
          Vector3D Z = new Vector3D(0, 0, 1);
          y = Vector3D.CrossProduct(Z, x);
          y.Normalize();
          z = Vector3D.CrossProduct(x, y);
          z.Normalize();
        }
      }
      else
      {
        Vector3D Yp = new Vector3D(orientationNode[0], orientationNode[1], orientationNode[2]);
        z = Vector3D.CrossProduct(x, Yp);
        z.Normalize();
        y = Vector3D.CrossProduct(z, x);
        y.Normalize();
      }

      //Rotation
      Matrix3D rotMat = HelperClass.RotationMatrix(x, rotationAngle.ToRadians());
      y = Vector3D.Multiply(y, rotMat);
      z = Vector3D.Multiply(z, rotMat);

      return new StructuralAxis(
          new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
          new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
          new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
      );
    }

    /// <summary>
    /// Calculates the local axis of a 2D entity.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="rotationAngle">Angle of rotation from default axis</param>
    /// <param name="isLocalAxis">Is axis calculated from local coordinates?</param>
    /// <returns>Axis</returns>
    public StructuralAxis Parse2DAxis(double[] coor, double rotationAngle = 0, bool isLocalAxis = false)
    {
      Vector3D x;
      Vector3D y;
      Vector3D z;

      List<Vector3D> nodes = new List<Vector3D>();

      for (int i = 0; i < coor.Length; i += 3)
        nodes.Add(new Vector3D(coor[i], coor[i + 1], coor[i + 2]));

      if (isLocalAxis)
      {
        if (nodes.Count == 3)
        {
          x = Vector3D.Subtract(nodes[1], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[2], nodes[0]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
        else if (nodes.Count == 4)
        {
          x = Vector3D.Subtract(nodes[2], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[3], nodes[1]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
        else
        {
          // Default to QUAD method
          x = Vector3D.Subtract(nodes[2], nodes[0]);
          x.Normalize();
          z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[3], nodes[1]));
          z.Normalize();
          y = Vector3D.CrossProduct(z, x);
          y.Normalize();
        }
      }
      else
      {
        x = Vector3D.Subtract(nodes[1], nodes[0]);
        x.Normalize();
        z = Vector3D.CrossProduct(x, Vector3D.Subtract(nodes[2], nodes[0]));
        z.Normalize();

        x = new Vector3D(1, 0, 0);
        x = Vector3D.Subtract(x, Vector3D.Multiply(Vector3D.DotProduct(x, z), z));

        if (x.Length == 0)
          x = new Vector3D(0, z.X > 0 ? -1 : 1, 0);

        y = Vector3D.CrossProduct(z, x);

        x.Normalize();
        y.Normalize();
      }

      //Rotation
      Matrix3D rotMat = HelperClass.RotationMatrix(z, rotationAngle * (Math.PI / 180));
      x = Vector3D.Multiply(x, rotMat);
      y = Vector3D.Multiply(y, rotMat);

      return new StructuralAxis(
          new StructuralVectorThree(new double[] { x.X, x.Y, x.Z }),
          new StructuralVectorThree(new double[] { y.X, y.Y, y.Z }),
          new StructuralVectorThree(new double[] { z.X, z.Y, z.Z })
      );
    }

    /// <summary>
    /// Calculates rotation angle of 1D entity to align with axis.
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="zAxis">Z axis of entity</param>
    /// <returns>Rotation angle</returns>
    public double Get1DAngle(double[] coor, StructuralVectorThree zAxis)
    {
      return Get1DAngle(LocalAxisEntity1D(coor, zAxis));
    }

    /// <summary>
    /// Calculates rotation angle of 1D entity to align with axis.
    /// </summary>
    /// <param name="axis">Axis of entity</param>
    /// <returns>Rotation angle</returns>
    public double Get1DAngle(StructuralAxis axis)
    {
      Vector3D axisX = new Vector3D(axis.Xdir.Value[0], axis.Xdir.Value[1], axis.Xdir.Value[2]);
      Vector3D axisY = new Vector3D(axis.Ydir.Value[0], axis.Ydir.Value[1], axis.Ydir.Value[2]);
      Vector3D axisZ = new Vector3D(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);

      if (axisX.X == 0 & axisX.Y == 0)
      {
        // Column
        Vector3D Yglobal = new Vector3D(0, 1, 0);

        double angle = Math.Acos(Vector3D.DotProduct(Yglobal, axisY) / (Yglobal.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) return 0;

        Vector3D signVector = Vector3D.CrossProduct(Yglobal, axisY);
        double sign = Vector3D.DotProduct(signVector, axisX);

        return sign >= 0 ? angle : -angle;
      }
      else
      {
        Vector3D Zglobal = new Vector3D(0, 0, 1);
        Vector3D Y0 = Vector3D.CrossProduct(Zglobal, axisX);
        double angle = Math.Acos(Vector3D.DotProduct(Y0, axisY) / (Y0.Length * axisY.Length)).ToDegrees();
        if (double.IsNaN(angle)) angle = 0;

        Vector3D signVector = Vector3D.CrossProduct(Y0, axisY);
        double sign = Vector3D.DotProduct(signVector, axisX);

        return sign >= 0 ? angle : 360 - angle;
      }
    }

    /// <summary>
    /// Calculates rotation angle of 2D entity to align with axis
    /// </summary>
    /// <param name="coor">Entity coordinates</param>
    /// <param name="axis">Axis of entity</param>
    /// <returns>Rotation angle</returns>
    public double Get2DAngle(double[] coor, StructuralAxis axis)
    {
      Vector3D axisX = new Vector3D(axis.Xdir.Value[0], axis.Xdir.Value[1], axis.Xdir.Value[2]);
      Vector3D axisY = new Vector3D(axis.Ydir.Value[0], axis.Ydir.Value[1], axis.Ydir.Value[2]);
      Vector3D axisZ = new Vector3D(axis.Normal.Value[0], axis.Normal.Value[1], axis.Normal.Value[2]);

      Vector3D x0;
      Vector3D z0;

      List<Vector3D> nodes = new List<Vector3D>();

      for (int i = 0; i < coor.Length; i += 3)
        nodes.Add(new Vector3D(coor[i], coor[i + 1], coor[i + 2]));

      // Get 0 angle axis in GLOBAL coordinates
      x0 = Vector3D.Subtract(nodes[1], nodes[0]);
      x0.Normalize();
      z0 = Vector3D.CrossProduct(x0, Vector3D.Subtract(nodes[2], nodes[0]));
      z0.Normalize();

      x0 = new Vector3D(1, 0, 0);
      x0 = Vector3D.Subtract(x0, Vector3D.Multiply(Vector3D.DotProduct(x0, z0), z0));

      if (x0.Length == 0)
        x0 = new Vector3D(0, z0.X > 0 ? -1 : 1, 0);

      x0.Normalize();

      // Find angle
      double angle = Math.Acos(Vector3D.DotProduct(x0, axisX) / (x0.Length * axisX.Length)).ToDegrees();
      if (double.IsNaN(angle)) return 0;

      Vector3D signVector = Vector3D.CrossProduct(x0, axisX);
      double sign = Vector3D.DotProduct(signVector, axisZ);

      return sign >= 0 ? angle : -angle;
    }

    /// <summary>
    /// Maps a flat array of coordinates from a local coordinate system to the global Cartesian coordinate system.
    /// </summary>
    /// <param name="values">Flat array of coordinates</param>
    /// <param name="axis">Local coordinate system</param>
    /// <returns>Transformed array of coordinates</returns>
    public double[] MapPointsLocal2Global(double[] values, StructuralAxis axis)
    {
      List<double> newVals = new List<double>();

      for (int i = 0; i < values.Length; i += 3)
      {
        List<double> coor = values.Skip(i).Take(3).ToList();

        double x = 0;
        double y = 0;
        double z = 0;

        x += axis.Xdir.Value[0] * coor[0];
        y += axis.Xdir.Value[1] * coor[0];
        z += axis.Xdir.Value[2] * coor[0];

        x += axis.Ydir.Value[0] * coor[1];
        y += axis.Ydir.Value[1] * coor[1];
        z += axis.Ydir.Value[2] * coor[1];

        x += axis.Normal.Value[0] * coor[2];
        y += axis.Normal.Value[1] * coor[2];
        z += axis.Normal.Value[2] * coor[2];

        newVals.Add(x);
        newVals.Add(y);
        newVals.Add(z);
      }

      return newVals.ToArray();
    }

    /// <summary>
    /// Maps a flat array of coordinates from the global Cartesian coordinate system to a local coordinate system.
    /// </summary>
    /// <param name="values">Flat array of coordinates</param>
    /// <param name="axis">Local coordinate system</param>
    /// <returns>Transformed array of coordinates</returns>
    public double[] MapPointsGlobal2Local(double[] values, StructuralAxis axis)
    {
      List<double> newVals = new List<double>();

      for (int i = 0; i < values.Length; i += 3)
      {
        List<double> coor = values.Skip(i).Take(3).ToList();

        double x = 0;
        double y = 0;
        double z = 0;

        x += axis.Xdir.Value[0] * coor[0];
        y += axis.Ydir.Value[0] * coor[0];
        z += axis.Normal.Value[0] * coor[0];

        x += axis.Xdir.Value[1] * coor[1];
        y += axis.Ydir.Value[1] * coor[1];
        z += axis.Normal.Value[1] * coor[1];

        x += axis.Xdir.Value[2] * coor[2];
        y += axis.Ydir.Value[2] * coor[2];
        z += axis.Normal.Value[2] * coor[2];

        newVals.Add(x);
        newVals.Add(y);
        newVals.Add(z);
      }

      return newVals.ToArray();
    }
    #endregion

    #region Polyline and Grids
    public (string, string) GetPolylineDesc(int polylineRef)
    {
      string res = GetGWARecords("GET\tPOLYLINE.1\t" + polylineRef.ToString()).FirstOrDefault();
      string[] pieces = res.ListSplit("\t");

      return (pieces[6], res);
    }

    public (int, string) GetGridPlaneRef(int gridSurfaceRef)
    {
      string res = GetGWARecords("GET\tGRID_SURFACE.1\t" + gridSurfaceRef.ToString()).FirstOrDefault();
      string[] pieces = res.ListSplit("\t");

      return (Convert.ToInt32(pieces[3]), res);
    }

    public (int, double, string) GetGridPlaneData(int gridPlaneRef)
    {
      string res = GetGWARecords("GET\tGRID_PLANE.4\t" + gridPlaneRef.ToString()).FirstOrDefault();
      string[] pieces = res.ListSplit("\t");

      return (Convert.ToInt32(pieces[4]), Convert.ToDouble(pieces[5]), res);
    }
    #endregion

    #region Elements
    public (double, string) GetGSATotal2DElementOffset(int propIndex, double insertionPointOffset)
    {
      double materialInsertionPointOffset = 0;
      double zMaterialOffset = 0;

      string res = GetGWARecords("GET\tPROP_2D\t" + propIndex.ToString()).FirstOrDefault();

      if (res == null || res == "")
        return (insertionPointOffset, null);

      string[] pieces = res.ListSplit("\t");

      zMaterialOffset = -Convert.ToDouble(pieces[12]);
      return (insertionPointOffset + zMaterialOffset + materialInsertionPointOffset, res);
    }
    #endregion

    #region Loads
    public (StructuralLoadTaskType, string) GetLoadTaskType(string taskRef)
    {
      string[] commands = GetGWARecords("GET\tTASK.1\t" + taskRef);

      string[] taskPieces = commands[0].ListSplit("\t");
      StructuralLoadTaskType taskType = StructuralLoadTaskType.LinearStatic;

      if (taskPieces[4] == "GSS")
      {
        if (taskPieces[5] == "STATIC")
          taskType = StructuralLoadTaskType.LinearStatic;
        else if (taskPieces[5] == "MODAL")
          taskType = StructuralLoadTaskType.Modal;
      }
      else if (taskPieces[4] == "GSRELAX")
      {
        if (taskPieces[5] == "BUCKLING_NL")
          taskType = StructuralLoadTaskType.NonlinearStatic;
      }

      return (taskType, commands[0]);
    }
    #endregion

    #region List
    /// <summary>
    /// Converts a GSA list to a list of indices.
    /// </summary>
    /// <param name="list">GSA list</param>
    /// <param name="type">GSA entity type</param>
    /// <returns></returns>
    public int[] ConvertGSAList(string list, GSAEntity type)
    {
      if (list == null) return new int[0];

      string[] pieces = list.ListSplit(" ");
      pieces = pieces.Where(s => !string.IsNullOrEmpty(s)).ToArray();

      List<int> items = new List<int>();
      for (int i = 0; i < pieces.Length; i++)
      {
        if (pieces[i].IsDigits())
          items.Add(Convert.ToInt32(pieces[i]));
        else if (pieces[i].Contains('"'))
        {
          items.AddRange(ConvertNamedGSAList(pieces[i], type));
        }
        else if (pieces[i] == "to")
        {
          int lowerRange = Convert.ToInt32(pieces[i - 1]);
          int upperRange = Convert.ToInt32(pieces[i + 1]);

          for (int j = lowerRange + 1; j <= upperRange; j++)
            items.Add(j);

          i++;
        }
        else
        {
          try
          {
            int[] itemTemp;
            GSAObject.EntitiesInList(pieces[i], (GsaEntity)type, out itemTemp);

            if (itemTemp == null)
              GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out itemTemp);

            if (itemTemp == null)
              continue;

            items.AddRange((int[])itemTemp);
          }
          catch
          { }
        }
      }

      return items.ToArray();
    }

    /// <summary>
    /// Converts a named GSA list to a list of indices.
    /// </summary>
    /// <param name="list">GSA list</param>
    /// <param name="type">GSA entity type</param>
    /// <returns></returns>
    public int[] ConvertNamedGSAList(string list, GSAEntity type)
    {
      list = list.Trim(new char[] { '"', ' ' });

      try
      {
        string res = GetGWARecords("GET\tLIST\t" + list).FirstOrDefault();

        string[] pieces = res.Split(new char[] { '\t' });

        return ConvertGSAList(pieces[pieces.Length - 1], type);
      }
      catch
      {
        try
        {
          GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out int[] itemTemp);
          if (itemTemp == null)
            return new int[0];
          else
            return (int[])itemTemp;
        }
        catch { return new int[0]; }
      }
    }

    /// <summary>
    /// Extracts and return the group indicies in the list.
    /// </summary>
    /// <param name="list">List</param>
    /// <returns>Array of group indices</returns>
    public int[] GetGroupsFromGSAList(string list)
    {
      string[] pieces = list.ListSplit(" ");

      List<int> groups = new List<int>();

      foreach (string p in pieces)
        if (p.Length > 0 && p[0] == 'G')
          groups.Add(Convert.ToInt32(p.Substring(1)));

      return groups.ToArray();
    }
    #endregion

    #region Cache
    /// <summary>
    /// Move current cache into previous cache.
    /// </summary>
    public void ClearCache()
    {
      PreviousGSAGetCache = new Dictionary<string, object>(GSAGetCache);
      GSAGetCache.Clear();
      PreviousGSASetCache = new Dictionary<string, object>(GSASetCache);
      GSASetCache.Clear();
      SidCache.Clear();
    }

    /// <summary>
    /// Clear current and previous cache.
    /// </summary>
    public void FullClearCache()
    {
      PreviousGSAGetCache.Clear();
      GSAGetCache.Clear();
      PreviousGSASetCache.Clear();
      GSASetCache.Clear();
      SidCache.Clear();
    }

    /// <summary>
    /// Blanks all records within the current and previous cache.
    /// </summary>
    public void DeleteSpeckleObjects()
    {
      BlankDepreciatedGWASetCommands();
      ClearCache();
      BlankDepreciatedGWASetCommands();
    }
    #endregion

    #region SID
    public string GenerateSID(SpeckleObject obj)
    {
      string sid = "";

      if (!string.IsNullOrEmpty(obj.ApplicationId))
        sid += "{" + SID_TAG + ":" + obj.ApplicationId + "}";

      return sid;
    }

    public string GetSID(string keyword, int id)
    {
      if (!SidCache.ContainsKey(keyword + "\t" + id.ToString()))
      {
        try
        {
          // Look in GET cache first
          if (GSAGetCache.ContainsKey("GET\t" + keyword + "\t" + id.ToString()))
          {
            var command = (string)GSAGetCache["GET\t" + keyword + "\t" + id.ToString()];
            var match = Regex.Match(command, "(?<={" + SID_TAG + ":).*?(?=})");
            if (!string.IsNullOrEmpty(match.Value))
              SidCache[keyword + "\t" + id.ToString()] = match.Value;
            else
              SidCache[keyword + "\t" + id.ToString()] = "gsa/" + keyword + "_" + id.ToString();
          }
          else
          {
            SidCache[keyword + "\t" + id.ToString()] = GSAObject.GetSidTagValue(keyword, id, SID_TAG);
            if (string.IsNullOrEmpty(SidCache[keyword + "\t" + id.ToString()]))
              SidCache[keyword + "\t" + id.ToString()] = "gsa/" + keyword + "_" + id.ToString();
          }
        }
        catch
        {
          SidCache[keyword + "\t" + id.ToString()] = "gsa/" + keyword + "_" + id.ToString();
        }
      }

      return SidCache[keyword + "\t" + id.ToString()];
    }
    #endregion

    #region Results
    /// <summary>
    /// Checks if the load case exists in the GSA file
    /// </summary>
    /// <param name="loadCase">GSA load case description</param>
    /// <returns>True if load case exists</returns>
    public bool CaseExist(string loadCase)
    {
      try
      {
        string[] pieces = loadCase.Split(new char[] { 'p' }, StringSplitOptions.RemoveEmptyEntries);

        if (pieces.Length == 1)
          return GSAObject.CaseExist(loadCase[0].ToString(), Convert.ToInt32(loadCase.Substring(1))) == 1;
        else if (pieces.Length == 2)
          return GSAObject.CaseExist(loadCase[0].ToString(), Convert.ToInt32(pieces[0].Substring(1))) == 1;
        else
          return false;
      }
      catch { return false; }
    }

    /// <summary>
    /// General extraction
    /// </summary>
    /// <param name="id">GSA entity ID</param>
    /// <param name="loadCase">Load case</param>
    /// <param name="axis">Result axis</param>
    /// <returns>Dictionary of reactions with keys {x,y,z,xx,yy,zz}.</returns>
    public Dictionary<string, object> GetGSAResult(int id, int resHeader, int flags, List<string> keys, string loadCase, string axis = "local", int num1DPoints = 2)
    {
      try
      {
        GsaResults[] res;
        int num;

        // Special case for assemblies
        if (Enum.IsDefined(typeof(ResHeader), resHeader) || resHeader == 18002000)
        {
          var initKey = "ARR" + flags.ToString() + axis + loadCase + resHeader.ToString() + num1DPoints.ToString();
          if (PreviousGSAResultInit != initKey)
          {
            GSAObject.Output_Init_Arr(flags, axis, loadCase, (ResHeader)resHeader, num1DPoints);
            PreviousGSAResultInit = initKey;
          }
          try
          { 
            try
            { 
              GSAObject.Output_Extract_Arr(id, out var outputExtractResults, out num);
              res = (GsaResults[])outputExtractResults;
            }
            catch
            {
              // Try reinit if fail
              GSAObject.Output_Init_Arr(flags, axis, loadCase, (ResHeader)resHeader, num1DPoints);
              GSAObject.Output_Extract_Arr(id, out var outputExtractResults, out num);
              res = (GsaResults[])outputExtractResults;
            }
          }
          catch
          {
            PreviousGSAResultInit = ""; // Blank the prev set

            res = new GsaResults[keys.Count];
            var resList = new List<GsaResults>();

            // Try individual extract
            for (int i = 1; i <= keys.Count; i++)
            {
              var indivResHeader = resHeader + i;
              GSAObject.Output_Init(flags, axis, loadCase, indivResHeader, num1DPoints);

              var numPos = 1;

              try
              {
                numPos = GSAObject.Output_NumElemPos(id);
              }
              catch { }
              
              if (i == 1)
              {
                res = new GsaResults[numPos];
                for (int j = 0; j < res.Length; j++)
                  res[j] = new GsaResults() { dynaResults = new double[keys.Count] };
              }

              for (int j = 0; j < numPos; j++)
                res[j].dynaResults[i - 1] = (double)GSAObject.Output_Extract(id, j);
            }
          }
        }
        else
        {
          var initKey = "SINGLE" + flags.ToString() + axis + loadCase + resHeader.ToString() + num1DPoints.ToString();
          if (PreviousGSAResultInit != initKey)
          {
            GSAObject.Output_Init(flags, axis, loadCase, resHeader, num1DPoints);
            PreviousGSAResultInit = initKey;
          }
          int numPos = GSAObject.Output_NumElemPos(id);

          res = new GsaResults[numPos];

          try
          {
            for (int i = 0; i < numPos; i++)
              res[i] = new GsaResults() { dynaResults = new double[] { (double)GSAObject.Output_Extract(id, i) } };
          }
          catch
          {
            // Try reinit if fail
            GSAObject.Output_Init(flags, axis, loadCase, resHeader, num1DPoints);
            for (int i = 0; i < numPos; i++)
              res[i] = new GsaResults() { dynaResults = new double[] { (double)GSAObject.Output_Extract(id, i) } };
          }
        }

        int counter = 0;
        Dictionary<string, object> ret = new Dictionary<string, object>();

        foreach (string key in keys)
        {
          ret[key] = res.Select(x => (double)x.dynaResults.GetValue(counter)).ToList();
          counter++;
        }

        return ret;
      }
      catch
      {
        Dictionary<string, object> ret = new Dictionary<string, object>();

        foreach (string key in keys)
          ret[key] = new List<double>() { 0 };

        return ret;
      }
    }
    #endregion
  }
}
