using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  //Elements can have parent members and the application IDs should be based on that of their parents, so they need to be read first, hence the inclusion of that as a read prerequisite
  [GSAObject("EL.4", new string[] { "NODE.3" }, "model", true, false, 
    new Type[] { typeof(GSANode), typeof(GSA1DProperty), typeof(GSASpringProperty), typeof(GSA1DMember) }, 
    new Type[] { typeof(GSANode), typeof(GSA1DProperty), typeof(GSASpringProperty) })]
  public class GSA1DElement : GSABase<Structural1DElement>
  {
    public int Member;

    public void ParseGWACommand(List<GSANode> nodes)
    {
      // GWA command from 10.1 docs
      // EL.4 | num | name | colour | type | prop | group | topo() | orient_node | orient_angle |
      // is_rls { | rls { | k } }
      // off_x1 | off_x2 | off_y | off_z | parent_member | dummy

      if (this.GWACommand == null)
      {
        return;
      }

      var keyword = this.GetGSAKeyword();

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
      
      var counter = 1; // Skip identifier

      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(keyword, this.GSAId);

      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Colour
      counter++; // Type
      var propRef = pieces[counter++];
      //Sometimes the property ref argument seems to have a relative length span attached to it:
      //e.g. 1[0.245882:0.491765] where 1 is the actual property reference
      var propRefNumerical = "";
      var index = 0;
      while (index < propRef.Length && char.IsDigit(propRef[index]))
      {
        propRefNumerical += propRef[index++];
      }
      
      obj.PropertyRef = Helper.GetApplicationId(typeof(GSA1DProperty).GetGSAKeyword(), Convert.ToInt32(propRefNumerical));
      counter++; // Group

      obj.Value = new List<double>();
      for (var i = 0; i < 2; i++)
      {
        var key = pieces[counter++];
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(key)).FirstOrDefault();
        node.ForceSend = true;
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      var orientationNodeRef = pieces[counter++];
      var rotationAngle = Convert.ToDouble(pieces[counter++]);

      try
      {
        if (orientationNodeRef != "0")
        {
          var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
          node.ForceSend = true;

          obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle, node.Value.Value.ToArray()).Normal as StructuralVectorThree;
          this.SubGWACommand.Add(node.GWACommand);
        }
        else
        {
          obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;
        }
      }
      catch
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Generating axis from coordinates for 1D element", obj.ApplicationId);
      }

      if (pieces[counter++] != "NO_RLS")
      {
        var start = pieces[counter++];
        var end = pieces[counter++];

        var endReleases = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[6]),
          new StructuralVectorBoolSix(new bool[6])
        };

        endReleases[0].Value[1] = ParseEndRelease(start[1], pieces, ref counter);
        endReleases[0].Value[2] = ParseEndRelease(start[2], pieces, ref counter);
        endReleases[0].Value[3] = ParseEndRelease(start[3], pieces, ref counter);
        endReleases[0].Value[4] = ParseEndRelease(start[4], pieces, ref counter);
        endReleases[0].Value[5] = ParseEndRelease(start[5], pieces, ref counter);

        endReleases[1].Value[0] = ParseEndRelease(end[0], pieces, ref counter);
        endReleases[1].Value[1] = ParseEndRelease(end[1], pieces, ref counter);
        endReleases[1].Value[2] = ParseEndRelease(end[2], pieces, ref counter);
        endReleases[1].Value[3] = ParseEndRelease(end[3], pieces, ref counter);
        endReleases[1].Value[4] = ParseEndRelease(end[4], pieces, ref counter);
        endReleases[1].Value[5] = ParseEndRelease(end[5], pieces, ref counter);

        obj.EndRelease = endReleases;
      }
      else
      {
        obj.EndRelease = new List<StructuralVectorBoolSix>
        {
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true }),
          new StructuralVectorBoolSix(new bool[] { true, true, true, true, true, true })
        };
      }

      var offsets = new List<StructuralVectorThree>
      {
        new StructuralVectorThree(new double[3]),
        new StructuralVectorThree(new double[3])
      };

      offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]);

      offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[1] = offsets[0].Value[1];

      offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]);
      offsets[1].Value[2] = offsets[0].Value[2];

      obj.Offset = offsets;

      counter++; // Dummy

      if (Helper.GetElementParentIdFromGwa(this.GWACommand, out int memberIndex))
      {
        Member = memberIndex;
        obj.ApplicationId = SpeckleStructuralClasses.Helper.CreateChildApplicationId(this.GSAId, Helper.GetApplicationId(typeof(GSA1DMember).GetGSAKeyword(), memberIndex));
      }

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;

      Initialiser.AppResources.Cache.SetApplicationId(keyword, this.GSAId, obj.ApplicationId);
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var element = this.Value as Structural1DElement;

      if (element.Value == null || element.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA1DElement).GetGSAKeyword();

      if (!SchemaConversion.Helper.ValidateCoordinates(element.Value, out var nodeIndices))
      {
        Initialiser.AppResources.Cache.RemoveFromProvisional(keyword, element.ApplicationId);
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, 
          "Coordinates resolve to identical GSA nodes for application ID:", element.ApplicationId);
        return "";
      }

      var index = Initialiser.AppResources.Cache.ResolveIndex(keyword, element.ApplicationId);

      var propKeyword = (element.ElementType == Structural1DElementType.Spring)
        ? typeof(GSASpringProperty).GetGSAKeyword()
        : typeof(GSA1DProperty).GetGSAKeyword();

      var indexResult = Initialiser.AppResources.Cache.LookupIndex(propKeyword, element.PropertyRef);

      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(propKeyword, element.PropertyRef);
      if (indexResult == null && element.ApplicationId != null)
      {
        if (element.PropertyRef == null)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Blank property references found for these Application IDs:",
            element.ApplicationId);
        }
        else
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "1D property references not found:",
            element.ApplicationId + " referencing " + element.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(element);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        element.Name == null || element.Name == "" ? " " : element.Name,
        "NO_RGB",
        "BEAM", // Type
        propRef.ToString(), // Prop
        group.ToString() // Group
      };
      
      // topo()
      foreach (var i in nodeIndices)
      {
        ls.Add(i.ToString());
      }

      ls.Add("0"); // Orientation Node

      // orient_angle
      double orientAngle = 0;
      try
      {
        if (element.ZAxis != null)
        {
          orientAngle = Helper.Get1DAngle(element.Value.ToArray(), element.ZAxis);
        }
      }
      catch { }
      ls.Add(orientAngle.ToString());
      
      // is_rls { | k }
      try
      {
        var subLs = new List<string>();
        var anyReleases = false;
        if (element.EndRelease != null && element.EndRelease.Count() > 0)
        {
          if (element.EndRelease[0].Value.Any(x => x) || (element.EndRelease.Count() > 1 && element.EndRelease[1].Value.Any(x => x)))
          {
            anyReleases = true;
          }
        }
        if (anyReleases)
        {
          subLs.Add("RLS");
          var end1 = "";

          end1 += element.EndRelease[0].Value[0] ? "R" : "F";
          end1 += element.EndRelease[0].Value[1] ? "R" : "F";
          end1 += element.EndRelease[0].Value[2] ? "R" : "F";
          end1 += element.EndRelease[0].Value[3] ? "R" : "F";
          end1 += element.EndRelease[0].Value[4] ? "R" : "F";
          end1 += element.EndRelease[0].Value[5] ? "R" : "F";
          subLs.Add(end1);

          if (element.EndRelease.Count() > 1)
          {
            var end2 = "";

            end2 += element.EndRelease[1].Value[0] ? "R" : "F";
            end2 += element.EndRelease[1].Value[1] ? "R" : "F";
            end2 += element.EndRelease[1].Value[2] ? "R" : "F";
            end2 += element.EndRelease[1].Value[3] ? "R" : "F";
            end2 += element.EndRelease[1].Value[4] ? "R" : "F";
            end2 += element.EndRelease[1].Value[5] ? "R" : "F";

            subLs.Add(end2);
          }
          else
          {
            subLs.Add("FFFFFF");
          }

          ls.AddRange(subLs);
        }
        else
        {
          ls.Add("NO_RLS");
        }
      }
      catch { ls.Add("NO_RLS"); }

      // off_x1 | off_x2 | off_y | off_z
      var offsets = new List<double>() { 0, 0, 0, 0 };
      try
      {
        if (element.Offset != null)
        {
          offsets[0] = element.Offset[0].Value[0];
          offsets[1] = element.Offset[1].Value[0];
          offsets[2] = element.Offset[0].Value[1];
          offsets[3] = element.Offset[0].Value[2];
        }
      }
      catch { }
      ls.AddRange(offsets.Select(o => o.ToString()));

      ls.Add(""); // parent_member

      ls.Add((element.GSADummy.HasValue && element.GSADummy.Value) ? "DUMMY" : ""); // dummy

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }

    private static bool ParseEndRelease(char code, string[] pieces, ref int counter)
    {
      switch (code)
      {
        case 'F':
          return false;
        case 'R':
          return true;
        default:
          // TODO
          counter++;
          return true;
      }
    }
  }

  [GSAObject("MEMB.8", new string[] { "NODE.3" }, "model", false, true, new Type[] { typeof(GSA1DProperty), typeof(GSANode), typeof(GSASpringProperty) }, new Type[] { typeof(GSA1DProperty), typeof(GSANode), typeof(GSASpringProperty) })]
  public class GSA1DMember : GSABase<Structural1DElement>
  {
    public int Group; // Keep for load targetting

    public void ParseGWACommand(List<GSANode> nodes)
    {
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      // 
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      // 
      // MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 }
      // rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | }
      // load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }

      if (this.GWACommand == null)
        return;

      var obj = new Structural1DElement();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // num - Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' }); // name
      counter++; // colour

      // type(1D)
      var type = pieces[counter++];
      if (type == "BEAM")
        obj.ElementType = Structural1DElementType.Beam;
      else if (type == "COLUMN")
        obj.ElementType = Structural1DElementType.Column;
      else if (type == "CANTILEVER")
        obj.ElementType = Structural1DElementType.Cantilever; // doesnt appear to be an option in GSA10.1
      else
        obj.ElementType = Structural1DElementType.Generic;

      counter++; // exposure - fire property e.g. TOP_BOT - not currently supported
      var propId = Convert.ToInt32(pieces[counter++]);
      
      this.Group = Convert.ToInt32(pieces[counter++]); // group - Keep group for load targetting

      // topology
      obj.Value = new List<double>();
      var nodeRefs = pieces[counter++].ListSplit(" ");
      for (var i = 0; i < nodeRefs.Length; i++)
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(nodeRefs[i])).FirstOrDefault();
        if (node == null)
        {
          //TO DO: review how this is possible and prevent it
          continue;
        }
        obj.Value.AddRange(node.Value.Value);
        this.SubGWACommand.Add(node.GWACommand);
      }

      // orientation
      var orientationNodeRef = pieces[counter++]; // node - aka orientation node
      var rotationAngle = Convert.ToDouble(pieces[counter++]); // angle

      if (orientationNodeRef != "0")
      {
        var node = nodes.Where(n => n.GSAId == Convert.ToInt32(orientationNodeRef)).FirstOrDefault();
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle, node.Value.Value.ToArray()).Normal as StructuralVectorThree;
        this.SubGWACommand.Add(node.GWACommand);
      }
      else
      {
        obj.ZAxis = Helper.Parse1DAxis(obj.Value.ToArray(), rotationAngle).Normal as StructuralVectorThree;
      }

      var meshSize = Convert.ToDouble(pieces[counter++]);
      // since this is a nullable GSA-specific property(and therefore needs a review), only set if not default
      if (meshSize > 0)
      {
        obj.GSAMeshSize = meshSize;
      }

      counter++; // is_intersector
      var analysisType = pieces[counter++]; // analysis_type
      if (analysisType == "SPRING")
      {
        obj.ElementType = Structural1DElementType.Spring;
      }

      obj.PropertyRef = Helper.GetApplicationId((obj.ElementType == Structural1DElementType.Spring ) 
        ? typeof(GSASpringProperty).GetGSAKeyword()
        : typeof(GSA1DProperty).GetGSAKeyword(), propId); // prop


      counter++; // fire
      counter++; // limiting temperature
      counter++; // time[] 1
      counter++; // time[] 2
      counter++; // time[] 3
      counter++; // time[] 4

      // dummy - since this is a nullable GSA-specific property (and therefore needs a review), only set if true
      if (pieces[counter++].ToLower() == "dummy")
      {
        obj.GSADummy = true;
      }

      // end releases
      var releases = new List<StructuralVectorBoolSix>();
      var endReleases = new List<StructuralVectorBoolSix>();
      if (counter < pieces.Length)
      {
        var end1Release = pieces[counter++].ToLower();
        endReleases.Add(ParseEndRelease(end1Release));
        if (end1Release.Contains('k'))
        {
          counter++; // skip past spring stiffnesses
        }
      }
      if (counter < pieces.Length)
      {
        var end2Release = pieces[counter++].ToLower();
        endReleases.Add(ParseEndRelease(end2Release));
        if (end2Release.Contains('k'))
        {
          counter++; // skip past spring stiffnesses
        }
      }

      if (endReleases.Count() > 0)
      {
        obj.EndRelease = endReleases;
      }

      // skip to offsets
      if(!pieces.Last().ToLower().StartsWith("no"))
      {
        // this approach ignores the auto / manual distinction in GSA
        // which may affect the true offset
        
        counter = pieces.Length - 4;

        var offsets = new List<StructuralVectorThree>
        {
          new StructuralVectorThree(new double[3]),
          new StructuralVectorThree(new double[3])
        };

        offsets[0].Value[0] = Convert.ToDouble(pieces[counter++]); // x1
        offsets[1].Value[0] = Convert.ToDouble(pieces[counter++]); // x2

        offsets[0].Value[1] = Convert.ToDouble(pieces[counter++]); // y
        offsets[1].Value[1] = offsets[0].Value[1]; // y

        offsets[0].Value[2] = Convert.ToDouble(pieces[counter++]); // z
        offsets[1].Value[2] = offsets[0].Value[2]; // z

        obj.Offset = offsets;
      }

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());

      this.Value = obj;
    }

    public string SetGWACommand(int group = 0)
    {
      if (this.Value == null)
        return "";

      var member = this.Value as Structural1DElement;
      if (member.Value == null || member.Value.Count() == 0)
        return "";

      var keyword = typeof(GSA1DMember).GetGSAKeyword();

      if (!SchemaConversion.Helper.ValidateCoordinates(member.Value, out var nodeIndices))
      {
        Initialiser.AppResources.Cache.RemoveFromProvisional(keyword, member.ApplicationId);
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, 
          "Coordinates resolve to identical GSA nodes for application ID:", member.ApplicationId);
        return "";
      }

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA1DMember).GetGSAKeyword(), member.ApplicationId);

      var propKeyword = ((member.ElementType == Structural1DElementType.Spring) ? typeof(GSASpringProperty) : typeof(GSA1DProperty)).GetGSAKeyword();
      var indexResult = Initialiser.AppResources.Cache.LookupIndex(propKeyword, member.PropertyRef);
      //If the reference can't be found, then reserve a new index so that it at least doesn't point to any other existing record
      var propRef = indexResult ?? Initialiser.AppResources.Cache.ResolveIndex(propKeyword, member.PropertyRef);
      if (indexResult == null && member.ApplicationId != null)
      {
        if (member.PropertyRef == null)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "Blank property references found for these Application IDs:",
            member.ApplicationId);
        }
        else
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "1D property references not found:",
            member.ApplicationId + " referencing " + member.PropertyRef);
        }
      }

      var sid = Helper.GenerateSID(member);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        member.Name == null || member.Name == "" ? " " : member.Name,
        "NO_RGB"
      };
      ls.Add(Structural1DElementTypeToString(member.ElementType));
      ls.Add("ALL"); // fire exposure reference, default to worst case (also GSA default)
      ls.Add(propRef.ToString());
      ls.Add(group != 0 ? group.ToString() : index.ToString()); // TODO: This allows for targeting of elements from members group
      ls.Add(string.Join(" ", nodeIndices));

      ls.Add("0"); // Orientation node
      if (member.Value == null)
      {
        ls.Add("0");
      }
      else
      {
        try
        {
          ls.Add(Helper.Get1DAngle(member.Value.ToArray(), member.ZAxis ?? new StructuralVectorThree(0, 0, 1)).ToString());
        }
        catch { ls.Add("0"); }
      }
      ls.Add(member.GSAMeshSize == null ? "0" : member.GSAMeshSize.ToString()); // Target mesh size
      ls.Add("YES"); // intersector - GSA default
      ls.Add((member.ElementType == Structural1DElementType.Spring) ? "SPRING" : "BEAM"); // analysis type - there are more options than this in GSA docs
      ls.Add("0"); // Fire
      ls.Add("0"); // Limiting temperature
      ls.Add("0"); // Time 1
      ls.Add("0"); // Time 2
      ls.Add("0"); // Time 3
      ls.Add("0"); // Time 4
      ls.Add((member.GSADummy.HasValue && member.GSADummy.Value) ? "DUMMY" : "ACTIVE");

      if (member.EndRelease == null || member.EndRelease.Count == 0)
      {
        ls.AddRange(new[] { EndReleaseToGWA(null), EndReleaseToGWA(null) });
      }
      else if (member.EndRelease != null && member.EndRelease.Count == 1)
      {
        ls.AddRange(new[] { EndReleaseToGWA(member.EndRelease.First()), EndReleaseToGWA(null) });
      }
      else
      {
        ls.AddRange(new[] { EndReleaseToGWA(member.EndRelease.First()), EndReleaseToGWA(member.EndRelease.Last()) });
      }

      ls.Add("Free"); // restraint_end_1
      ls.Add("Free"); // restraint_end_2

      ls.Add("AUTOMATIC"); // Effective length option
      ls.Add("0"); // height
      ls.Add("SHR_CENTRE"); // load_ref

      if (member.Offset == null)
      {
        ls.Add("NO_OFF");
      }
      else
      {
        ls.Add("YES");
        ls.Add("MAN");
        ls.Add("MAN");
        try
        {
          var subLs = new List<string>
        {
          member.Offset[0].Value[0].ToString(), // Offset x-start
          member.Offset[1].Value[0].ToString(), // Offset x-end

          member.Offset[0].Value[1].ToString(),
          member.Offset[0].Value[2].ToString()
        };

          ls.AddRange(subLs);
        }
        catch
        {
          ls.AddRange(new[] { "0", "0", "0", "0" });
        }
      }

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }

    private static string Structural1DElementTypeToString(Structural1DElementType t)
    {
      switch (t)
      {
        case Structural1DElementType.Beam: return "BEAM";
        case Structural1DElementType.Column: return "COLUMN";
        case Structural1DElementType.Cantilever: return "CANTILEVER";
        default: return "1D_GENERIC";
      }
    }

    private static StructuralVectorBoolSix ParseEndRelease(string code)
    {
      if (code.Length != 6)
        throw new ArgumentException($"End release code must be exactly six characters long - input code '{code}'");

      bool[] releases = new bool[6];
      for(int i = 0; i < code.Length; i++)
      {
        char piece = code.ToLower()[i];
        if (piece == 'f')
          releases[i] = false;
        else
          releases[i] = true;
      }

      return new StructuralVectorBoolSix(releases);
    }

    private static string EndReleaseToGWA(StructuralVectorBoolSix release)
    {
      string code = "";

      if (release == null)
        return "FFFFFF"; // GSA default

      foreach (bool b in release.Value)
        if (b)
          code += "R";
        else
          code += "F";

      return code;
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this SpeckleLine inputObject)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(inputObject, () =>
      {
        var convertedObject = new Structural1DElement();

        foreach (var p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
        {
          var inputProperty = inputObject.GetType().GetProperty(p.Name);
          if (inputProperty != null)
            p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
        }

        return convertedObject.ToNative();
      });
    }

    public static string ToNative(this Structural1DElement beam)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(beam, () => (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis) 
        ? new GSA1DElement() { Value = beam }.SetGWACommand()
        : new GSA1DMember() { Value = beam }.SetGWACommand());
    }

    public static SpeckleObject ToSpeckle(this GSA1DElement dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DElement>();
      var typeName = dummyObject.GetType().Name;
      var elementsLock = new object();
      var elements = new SortedDictionary<int, GSA1DElement>();
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var keyword = dummyObject.GetGSAKeyword();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

        if (pPieces[4] == "BEAM" && pPieces[4].ParseElementNumNodes() == 2)
        {
          var gsaId = pPieces[1];
          try
          {
            var element = new GSA1DElement() { GWACommand = newLines[k] };
            element.ParseGWACommand(nodes);
            lock (elementsLock)
            {
              elements.Add(k, element);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
              "Keyword=" + keyword, "Index=" + k);
          }
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GsaKit.GSASenderObjects.AddRange(elements.Values.ToList());

      return (elements.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }

    public static SpeckleObject ToSpeckle(this GSA1DMember dummyObject)
    {
      var nodes = Initialiser.GsaKit.GSASenderObjects.Get<GSANode>();
      var membersLock = new object();
      var members = new SortedDictionary<int, GSA1DMember>();
      var newLines = ToSpeckleBase<GSA1DMember>();
      var typeName = dummyObject.GetType().Name;
      var keyword = dummyObject.GetGSAKeyword();

#if DEBUG
      foreach (var k in newLines.Keys)
#else
      Parallel.ForEach(newLines.Keys, k =>
#endif
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        if (pPieces[4].Is1DMember())
        {
          try
          {
            var member = new GSA1DMember() { GWACommand = newLines[k] };
            member.ParseGWACommand(nodes);
            lock (membersLock)
            {
              members.Add(k, member);
            }
          }
          catch (Exception ex)
          {
            Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
              "Keyword=" + keyword, "Index=" + k);
          }
        }
      }
#if !DEBUG
      );
#endif

      Initialiser.GsaKit.GSASenderObjects.AddRange(members.Values.ToList());

      return (members.Keys.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
