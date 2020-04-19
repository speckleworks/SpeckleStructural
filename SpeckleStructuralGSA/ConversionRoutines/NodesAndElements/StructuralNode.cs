using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("NODE.2", new string[] { "AXIS.1" }, "nodes", true, true, new Type[] { }, new Type[] { typeof(GSA1DElement), typeof(GSA1DMember), typeof(GSA2DElement), typeof(GSA2DMember) })]
  public class GSANode : IGSASpeckleContainer
  {
    public bool ForceSend; // This is to filter only "important" nodes

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNode();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralNode();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
      obj.Value = new List<double>
      {
        Convert.ToDouble(pieces[counter++]),
        Convert.ToDouble(pieces[counter++]),
        Convert.ToDouble(pieces[counter++])
      };

      //counter += 3; // TODO: Skip unknown fields in NODE.3

      while (counter < pieces.Length)
      {
        var s = pieces[counter++];

        switch (s)
        {
          case "NO_GRID":
          case "NO_REST":
          case "NO_MESH":
          case "NO_STIFF":
            continue;
          case "GRID":
            counter++; // Grid place
            counter++; // Datum
            counter++; // Grid line A
            counter++; // Grid line B
            break;
          case "REST":
            obj.Restraint = new StructuralVectorBoolSix(new bool[6]);
            for (var i = 0; i < 6; i++)
              obj.Restraint.Value[i] = pieces[counter++] == "0" ? false : true;
            this.ForceSend = true;
            break;
          case "STIFF":
            obj.Stiffness = new StructuralVectorSix(new double[6]);
            for (var i = 0; i < 6; i++)
              obj.Stiffness.Value[i] = Convert.ToDouble(pieces[counter++]);
            this.ForceSend = true;
            break;
          case "MESH":
            obj.GSALocalMeshSize = pieces[counter++].ToDouble();
            counter++; // Edge length
            counter++; // Radius
            counter++; // Tie to mesh
            counter++; // Column rigidity
            counter++; // Column prop
            counter++; // Column node
            counter++; // Column angle
            counter++; // Column factor
            counter++; // Column slab factor
            break;
          default: // Axis
            string gwaRec = null;
            obj.Axis = Helper.Parse0DAxis(Convert.ToInt32(s), Initialiser.Interface, out gwaRec, obj.Value.ToArray());
            if (gwaRec != null)
              this.SubGWACommand.Add(gwaRec);
            break;
        }
      }

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

      var index = Helper.NodeAt(node.Value[0], node.Value[1], node.Value[2], Initialiser.Settings.CoincidentNodeAllowance);

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(node),
        index.ToString(),
        node.Name == null || node.Name == "" ? " " : node.Name,
        "NO_RGB",
        string.Join("\t", node.Value.ToArray()),

        //ls.Add("0"); // TODO: Skip unknown fields in NODE.3
        //ls.Add("0"); // TODO: Skip unknown fields in NODE.3
        //ls.Add("0"); // TODO: Skip unknown fields in NODE.3

        "NO_GRID"
      };


      var gwaCommands = new List<string>();
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
      catch { ls.Add("0"); }

      try
      {
        if (node.Restraint == null || !node.Restraint.Value.Any(x => x))
        {
          ls.Add("NO_REST");
        }
        else
        {
          var subLs = new List<string>
          {
            "REST",
            node.Restraint.Value[0] ? "1" : "0",
            node.Restraint.Value[1] ? "1" : "0",
            node.Restraint.Value[2] ? "1" : "0",
            node.Restraint.Value[3] ? "1" : "0",
            node.Restraint.Value[4] ? "1" : "0",
            node.Restraint.Value[5] ? "1" : "0"
          };
          ls.AddRange(subLs);
        }

        

      }
      catch { ls.Add("NO_REST"); }

      try
      {
        if (node.Stiffness == null || !node.Stiffness.Value.Any(x => x == 0))
        {
          ls.Add("NO_STIFF");
        }
        else
        {
          var subLs = new List<string>
          {
            "STIFF",
            node.Stiffness.Value[0].ToString(),
            node.Stiffness.Value[1].ToString(),
            node.Stiffness.Value[2].ToString(),
            node.Stiffness.Value[3].ToString(),
            node.Stiffness.Value[4].ToString(),
            node.Stiffness.Value[5].ToString()
          };
          ls.AddRange(subLs);
        }
      }
      catch { ls.Add("NO_STIFF"); }

      try
      {
        if (node.GSALocalMeshSize == 0)
        {
          ls.Add("NO_MESH");
        }
        else
        {
          var subLs = new List<string>
          {
            "MESH",
            node.GSALocalMeshSize.ToString(),
            "0", // Radius
            "NO", // Tie to mesh
            "NO", // column rigidity will be generated
            "0", // Column property number
            "0", //Column orientation node
            "0", //Column orientation angle
            "1", //Column dimension factor
            "0" //Column slab thickness factor
          };

          ls.AddRange(subLs);
        }
      }
      catch (Exception)
      {
        ls.Add("NO_MESH");
      }

      gwaCommands.Add(string.Join("\t", ls));

      return string.Join("\n", gwaCommands);
    }
  }

  [GSAObject("EL.3", new string[] { "PROP_MASS.2" }, "elements", true, false, new Type[] { typeof(GSANode) }, new Type[] { typeof(GSANode) })]
  public class GSA0DElement : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new StructuralNode();

    public void ParseGWACommand()
    {
      if (this.GWACommand == null)
        return;

      var obj = new StructuralNode();

      var pieces = this.GWACommand.ListSplit("\t");

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

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var node = this.Value as StructuralNode;
      if (node.Value == null || node.Value.Count() != 3)
      {
        Initialiser.AppUI.Message("Node with invalid point", node.ApplicationId);
        return "";
      }

      if (!node.Mass.HasValue || node.Mass == 0)
        return "";

      var keyword = typeof(GSA0DElement).GetGSAKeyword();
      var index = Initialiser.Cache.ResolveIndex(typeof(GSA0DElement).GetGSAKeyword(), node.ApplicationId);
      var propIndex = Initialiser.Cache.ResolveIndex("PROP_MASS.2", node.ApplicationId);
      var nodeIndex = Initialiser.Cache.ResolveIndex(typeof(GSANode).GetGSAKeyword(), node.ApplicationId);

      var gwaCommands = new List<string>();

      var ls = new List<string>
      {
        "SET",
        keyword + ":" + Helper.GenerateSID(node),
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

      gwaCommands.Add(string.Join("\t", ls));

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

      gwaCommands.Add(string.Join("\t", ls));

      return string.Join("\n", gwaCommands);
    }

    private double GetGSAMass(int propertyIndex)
    {
      var gwa = Initialiser.Cache.GetGwa("PROP_MASS.2", propertyIndex).FirstOrDefault();
      var pieces = gwa.ListSplit("\t");

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

      var nodes = new List<GSANode>();

      foreach (var p in newLines.Values)
      {
        var node = new GSANode { GWACommand = p };
        node.ParseGWACommand();
        nodes.Add(node);
      }

      Initialiser.GSASenderObjects.AddRange(nodes);

      return (nodes.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA0DElement dummyObject)
    {
      if (Initialiser.GSASenderObjects.Count<GSA0DElement>() == 0)
        return new SpeckleNull();

      var newLines = ToSpeckleBase<GSA0DElement>();

      var changed = false;

      var nodes = Initialiser.GSASenderObjects.Get<GSANode>();

      foreach (var p in newLines.Values)
      {
        var pPieces = p.ListSplit("\t");
        if (pPieces[4].ParseElementNumNodes() == 1)
        {
          try
          {
            var massNode = new GSA0DElement() { GWACommand = p };
            massNode.ParseGWACommand();

            var match = nodes
                .Where(n => n.Value.ApplicationId == massNode.Value.ApplicationId)
                .First();

            if (match != null)
            {
              match.Value.Mass = massNode.Value.Mass;
              match.SubGWACommand.AddRange(massNode.SubGWACommand.Concat(new string[] { p }));

              match.ForceSend = true;

              changed = true;
            }
          }
          catch { }
        }
      }

      return (changed) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
