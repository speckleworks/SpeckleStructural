using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("NODE.3", new string[] { "AXIS.1", "PROP_SPR.4", "PROP_MASS.2" }, "model", true, true, new Type[] { }, new Type[] { })]
  public class GSANode : GSABase<StructuralNode>
  {
    public bool ForceSend; // This is to filter only "important" nodes

    public void ParseGWACommand()
    {
      // NODE.3 | num | name | colour | x | y | z | restraint | axis |
      // mesh_size | springProperty | massProperty | damperProperty

      if (this.GWACommand == null)
        return;

      var obj = new StructuralNode();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]); // num
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      counter++; // colour
      obj.Value = new List<double>
      {
        Convert.ToDouble(pieces[counter++]), // x
        Convert.ToDouble(pieces[counter++]), // y
        Convert.ToDouble(pieces[counter++]) // z
      };

      if (counter >= pieces.Length)
      {
        this.Value = obj;
        return;
      }

      if (counter < pieces.Length)
      {
        obj.Restraint = Helper.RestraintFromCode(pieces[counter++]); // restraint
      }

      if (counter < pieces.Length)
      {

        // axis
        var axis = pieces[counter++];
        if (string.IsNullOrEmpty(axis) || axis == "GLOBAL")
        {
          obj.Axis = Helper.Global;
        }
        else
        {
          string gwaRec = null;
          obj.Axis = Helper.Parse0DAxis(Convert.ToInt32(axis), out gwaRec, obj.Value.ToArray());
          if (gwaRec != null)
          {
            this.SubGWACommand.Add(gwaRec);
          }
        }
      }

      if (counter < pieces.Length)
      {
        obj.GSALocalMeshSize = pieces[counter++].ToDouble(); // mesh_size
      }

      if (counter < pieces.Length)
      {
        // springProperty
        var spKeyword = typeof(GSASpringProperty).GetGSAKeyword().Split('.').First();
        var springPropsGwa = Initialiser.AppResources.Cache.GetGwa(spKeyword, Convert.ToInt32(pieces[counter++])); // not sure how this could ever return multiple?
        if (springPropsGwa.Count > 0)
        {
          var springPropGWA = springPropsGwa[0];
          var springProp = new GSASpringProperty();
          springProp.GWACommand = springPropGWA;
          springProp.ParseGWACommand();
          obj.Stiffness = springProp.Value.Stiffness;
        }
      }

      if (counter < pieces.Length)
      {
        // massProperty
        // Speckle node currently only supports single mass, rather than the more complicated PROP_MASS in GSA
        var massPropsGwa = Initialiser.AppResources.Cache.GetGwa("PROP_MASS", Convert.ToInt32(pieces[counter++]));
        if (massPropsGwa.Count > 0)
        {
          var massPropGwa = massPropsGwa[0];
          var massPropPieces = massPropGwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
          obj.Mass = Convert.ToDouble(massPropPieces[5]);
        }
      }
      
      // damperProperty - not yet supported

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var node = this.Value as StructuralNode;

      if (node.Value == null || node.Value.Count() != 3)
      {
        return "";
      }

      var keyword = typeof(GSANode).GetGSAKeyword();

      var index = Initialiser.AppResources.Proxy.NodeAt(node.Value[0], node.Value[1], node.Value[2], Initialiser.AppResources.Settings.CoincidentNodeAllowance);

      var sid = Helper.GenerateSID(node);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(), // num
        node.Name == null || node.Name == "" ? " " : node.Name, // name
        "NO_RGB", // colour
        string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), node.Value.Select(v => Math.Round(v, 8)).ToArray()), // x y z - GSA seems to round to 8 here
      };

      // restraint
      try
      {
        if (node.Restraint != null)
        {
          string restraint = "";
          restraint += node.Restraint.Value[0] ? "x" : "";
          restraint += node.Restraint.Value[1] ? "y" : "";
          restraint += node.Restraint.Value[2] ? "z" : "";
          restraint += node.Restraint.Value[3] ? "xx" : "";
          restraint += node.Restraint.Value[4] ? "yy" : "";
          restraint += node.Restraint.Value[5] ? "zz" : "";

          if (restraint == "")
          {
            ls.Add("free");
          }
          else if (restraint == "xyz")
          {
            ls.Add("pin");
          }
          else if (restraint == "xyzxxyyzz")
          {
            ls.Add("fix");
          }
          else
          {
            ls.Add(restraint);
          }
        }
        else
        {
          ls.Add("free");
        }
      }
      catch { ls.Add("free"); }

      var gwaCommands = new List<string>();

      //This condition will need to be expanded if springProperty, massProperty and damperProperty are ever implemented
      if (node.Axis != null && node.GSALocalMeshSize.HasValue)
      {
        // axis
        
        var axisGwa = "";
        try
        {
          Helper.SetAxis(node.Axis, out var axisIndex, out axisGwa, node.Name);
          if (axisGwa.Length > 0)
          {
            gwaCommands.Add(axisGwa);
          }

          ls.Add(axisIndex.ToString());
        }
        catch { ls.Add("GLOBAL"); }

        ls.Add(node.GSALocalMeshSize.HasValue ? node.GSALocalMeshSize.Value.ToString() : ""); // mesh_size - may need to perform rounding here

        // TODO: springProperty
        // naive of one spring property per springy node could create thousands of spring props

        // TODO: massProperty
        // similar potential problem as spring props

        // damperProperty - not supported
      }

      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      return string.Join("\n", gwaCommands);
    }
  }

  //Note this relies on GSANode being called first - which creates the nodes to be received/sent.  The nodes are then altered by the methods
  //for GSA0DElement
  [GSAObject("EL.4", new string[] { "PROP_MASS.2" }, "model", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DElement : GSABase<StructuralNode>
  {
    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralNode();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      counter++; // Reference
      counter++; // Name
      counter++; // Color
      counter++; // Type
      var mass = GetGSAMass(Convert.ToInt32(pieces[counter++]));
      obj.Mass = mass;
      counter++; // group
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      // Rest is unimportant for 0D element

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var node = this.Value as StructuralNode;
      if (node.Value == null || node.Value.Count() != 3)
      {
        return "";
      }

      if (!node.Mass.HasValue || node.Mass == 0)
        return "";

      var keyword = typeof(GSA0DElement).GetGSAKeyword();
      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA0DElement).GetGSAKeyword(), node.ApplicationId);
      var propIndex = Initialiser.AppResources.Cache.ResolveIndex("PROP_MASS.2", node.ApplicationId);
      var nodeIndex = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSANode).GetGSAKeyword(), node.ApplicationId);

      var gwaCommands = new List<string>();

      var sid = Helper.GenerateSID(node);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        node.Name == null || node.Name == "" ? " " : node.Name,
        "NO_RGB",
        "MASS",
        propIndex.ToString(),
        "0", // Group
        nodeIndex.ToString(),
        "0", // Orient Node
        "0", // Beta
        "NO_RLS", // Release
        "0", // Offset x-start
        "0", // Offset y-start
        "0", // Offset y
        "0", // Offset z
        "" //Dummy
      };

      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      ls = new List<string>
      {
        "SET",
        "PROP_MASS.2" + ":" + Helper.GenerateSID(node),
        propIndex.ToString(),
        "",
        "NO_RGB",
        "GLOBAL",
        node.Mass.ToString(),
        "0",
        "0",
        "0",
        "0",
        "0",
        "0",

        "MOD",
        "100%",
        "100%",
        "100%"
      };

      gwaCommands.Add(string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));

      return string.Join("\n", gwaCommands);
    }

    private double GetGSAMass(int propertyIndex)
    {
      var gwa = Initialiser.AppResources.Cache.GetGwa("PROP_MASS", propertyIndex).FirstOrDefault();
      var pieces = gwa.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      this.SubGWACommand.Add(gwa);

      return Convert.ToDouble(pieces[5]);
    }
  }
  
  public static partial class Conversions
  {
    public static string ToNative(this SpecklePoint inputObject)
    {
      var convertedObject = new StructuralNode();

      foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        var inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      return convertedObject.ToNative();
    }

    public static string ToNative(this StructuralNode node)
    {
      return string.Join("\n", new[] { new GSANode() { Value = node }.SetGWACommand(), new GSA0DElement() { Value = node }.SetGWACommand() });
    }

    public static SpeckleObject ToSpeckle(this GSANode dummyObject)
    {
      var newLines = ToSpeckleBase<GSANode>();
      var typeName = dummyObject.GetType().Name;
      var nodesLock = new object();
      var nodes = new SortedDictionary<int, GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        var node = new GSANode { GWACommand = newLines[k] };
        try
        {
          node.ParseGWACommand();
          lock (nodesLock)
          {
            nodes.Add(k, node);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, gsaId);
          Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, gsaId);
        }
      }
      );

      Initialiser.GsaKit.GSASenderObjects.AddRange(nodes.Values.ToList());

      return (nodes.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA0DElement dummyObject)
    {
      if (Initialiser.GsaKit.GSASenderObjects.Count<GSA0DElement>() == 0)
        return new SpeckleNull();

      var newLines = ToSpeckleBase<GSA0DElement>();
      var typeName = dummyObject.GetType().Name;
      var changed = false;

      var nodesLock = new object();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        if (pPieces[4].ParseElementNumNodes() == 1)
        {
          try
          {
            var massNode = new GSA0DElement() { GWACommand = newLines[k] };
            massNode.ParseGWACommand();

            GSANode match;
            lock (nodesLock)
            {
              match = nodes.Where(n => n.Value.ApplicationId == massNode.Value.ApplicationId).First();

              if (match != null)
              {
                match.Value.Mass = massNode.Value.Mass;
                match.SubGWACommand.AddRange(massNode.SubGWACommand.Concat(new string[] { newLines[k] }));

                match.ForceSend = true;

                changed = true;
              }
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.Display, MessageLevel.Error, typeName, gsaId);
            Initialiser.AppResources.Messenger.CacheMessage(MessageIntent.TechnicalLog, MessageLevel.Error, ex, typeName, gsaId);
          }
        }
      }
      );

      return (changed) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
