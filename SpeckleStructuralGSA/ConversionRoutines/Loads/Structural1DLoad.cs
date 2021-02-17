using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Spatial.Euclidean;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("LOAD_BEAM", new string[] { "EL.4", "MEMB.8" }, "model", true, true, new Type[] { typeof(GSA1DMember), typeof(GSA1DElement) }, new Type[] { typeof(GSA1DMember), typeof(GSA1DElement), typeof(GSA1DElementPolyline) })]
  public class GSA1DLoad : GSABase<Structural1DLoad>
  {
    public int Axis; // Store this temporarily to generate other loads
    public bool Projected;

    public void ParseGWACommand(List<GSA1DElement> elements, List<GSA1DMember> members)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DLoad();
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 0; // Skip identifier
      var identifier = pieces[counter++];

      obj.Name = pieces[counter++].Trim(new char[] { '"' });

      if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
      {
        var targetElements = Initialiser.AppResources.Proxy.ConvertGSAList(pieces[counter++], SpeckleGSAInterfaces.GSAEntity.ELEMENT);

        if (elements != null)
        {
          var elems = elements.Where(n => targetElements.Contains(n.GSAId)).ToList();

          obj.ElementRefs = elems.Select(n => ((SpeckleObject)n.Value).ApplicationId).OrderBy(i => i).ToList();
          this.SubGWACommand.AddRange(elems.Select(n => n.GWACommand));
        }
      }
      else if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Design)
      {
        var targetGroups = Helper.GetGroupsFromGSAList(pieces[counter++]);

        if (members != null)
        {
          var membs = members.Where(m => targetGroups.Contains(m.Group)).ToList();

          obj.ElementRefs = membs.Select(m => ((SpeckleObject)m.Value).ApplicationId).ToList();
          this.SubGWACommand.AddRange(membs.Select(n => n.GWACommand));
        }
      }

      obj.LoadCaseRef = Helper.GetApplicationId(typeof(GSALoadCase).GetGSAKeyword(), Convert.ToInt32(pieces[counter++]));

      var axis = pieces[counter++];
      this.Axis = axis == "GLOBAL" ? 0 : -1;// Convert.ToInt32(axis); // TODO: Assume local if not global

      this.Projected = pieces[counter++] == "YES";

      obj.Loading = new StructuralVectorSix(new double[6]);
      var direction = pieces[counter++].ToLower();

      double value = 0;

      // TODO: Only reads UDL load properly
      if (identifier.Contains("LOAD_BEAM_POINT.2"))
      {
        counter++; // Position
        counter++; // Value
        value = 0;
      }
      else if (identifier.Contains("LOAD_BEAM_UDL.2"))
        value = Convert.ToDouble(pieces[counter++]);
      else if (identifier.Contains("LOAD_BEAM_LINE.2"))
      {
        value = Convert.ToDouble(pieces[counter++]);
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else if (identifier.Contains("LOAD_BEAM_PATCH.2"))
      {
        counter++; // Position
        value = Convert.ToDouble(pieces[counter++]);
        counter++; // Position
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else if (identifier.Contains("LOAD_BEAM_TRILIN.2"))
      {
        counter++; // Position
        value = Convert.ToDouble(pieces[counter++]);
        counter++; // Position
        value += Convert.ToDouble(pieces[counter++]);
        value /= 2;
      }
      else
      {
        value = 0;
      }

      switch (direction.ToUpper())
      {
        case "X":
          obj.Loading.Value[0] = value;
          break;
        case "Y":
          obj.Loading.Value[1] = value;
          break;
        case "Z":
          obj.Loading.Value[2] = value;
          break;
        case "XX":
          obj.Loading.Value[3] = value;
          break;
        case "YY":
          obj.Loading.Value[4] = value;
          break;
        case "ZZ":
          obj.Loading.Value[5] = value;
          break;
        default:
          // TODO: Error case maybe?
          break;
      }

      this.Value = obj;
    }
  }

  public static partial class Conversions
  {
    //The ToNative() method is in the new schema conversion folder hierarchy

    public static SpeckleObject ToSpeckle(this GSA1DLoad dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DLoad>();
      var typeName = dummyObject.GetType().Name;
      var loads = new List<GSA1DLoad>();
      var elements = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DElement>();
      var members = Initialiser.GsaKit.GSASenderObjects.Get<GSA1DMember>();
      var keyword = dummyObject.GetGSAKeyword();

      foreach (var k in newLines.Keys)
      {
        var p = newLines[k];
        var loadSubList = new List<GSA1DLoad>();

        // Placeholder load object to get list of elements and load values
        // Need to transform to axis so one load definition may be transformed to many
        var initLoad = new GSA1DLoad() { GWACommand = p, GSAId = k };
        try
        {
          initLoad.ParseGWACommand(elements, members);
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }

        // Create load for each element applied
        foreach (var nRef in initLoad.Value.ElementRefs)
        {
          var load = new GSA1DLoad
          {
            GWACommand = initLoad.GWACommand,
            SubGWACommand = new List<string>(initLoad.SubGWACommand)
          };
          //var speckle1dLoad = load.Value;

          load.Value.Name = initLoad.Value.Name;
          load.Value.ApplicationId = initLoad.Value.ApplicationId;
          load.Value.LoadCaseRef = initLoad.Value.LoadCaseRef;

          if (Initialiser.AppResources.Settings.TargetLayer == GSATargetLayer.Analysis)
          {
            // Transform load to defined axis
            var gsaElem = elements.Where(e => ((SpeckleObject)e.Value).ApplicationId == nRef).First();
            var elem = (Structural1DElement)gsaElem.Value;
            var loadAxis = load.Axis == 0 ? new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })) :
                Helper.LocalAxisEntity1D(elem.Value.ToArray(), elem.ZAxis); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // Perform projection
            if (load.Projected)
            {
              var loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                var axisX = new Vector3D(elem.Value[5] - elem.Value[0], elem.Value[4] - elem.Value[1], elem.Value[3] - elem.Value[2]);
                var angle = loadDirection.AngleTo(axisX);
                var factor = Math.Sin(angle.Radians);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

            // If the loading already exists, add element ref to list
            var match = loadSubList.Count() > 0 ? loadSubList.Where(l => ((l.Value).Loading.Value as List<double>)
              .SequenceEqual((load.Value).Loading.Value as List<double>)).First() : null;
            if (match != null)
            {
              match.Value.ElementRefs.Add(nRef);
            }
            else
            {
              load.Value.ElementRefs = new List<string>() { nRef };
              loadSubList.Add(load);
            }
          }
          else
          {
            // Transform load to defined axis
            var gsaMemb = members.Where(e => (e.Value).ApplicationId == nRef).First();
            var memb = gsaMemb.Value;
            var loadAxis = load.Axis == 0 ? new StructuralAxis(
                new StructuralVectorThree(new double[] { 1, 0, 0 }),
                new StructuralVectorThree(new double[] { 0, 1, 0 }),
                new StructuralVectorThree(new double[] { 0, 0, 1 })) :
                Helper.LocalAxisEntity1D(memb.Value.ToArray(), memb.ZAxis); // Assumes if not global, local
            load.Value.Loading = initLoad.Value.Loading;
            load.Value.Loading.TransformOntoAxis(loadAxis);

            // Perform projection
            if (load.Projected)
            {
              var loadDirection = new Vector3D(
                  load.Value.Loading.Value[0],
                  load.Value.Loading.Value[1],
                  load.Value.Loading.Value[2]);

              if (loadDirection.Length > 0)
              {
                var axisX = new Vector3D(memb.Value[5] - memb.Value[0], memb.Value[4] - memb.Value[1], memb.Value[3] - memb.Value[2]);
                var angle = loadDirection.AngleTo(axisX);
                var factor = Math.Sin(angle.Radians);
                load.Value.Loading.Value[0] *= factor;
                load.Value.Loading.Value[1] *= factor;
                load.Value.Loading.Value[2] *= factor;
              }
            }

            // If the loading already exists, add element ref to list
            var match = loadSubList.Count() > 0 ? loadSubList.Where(l => (l.Value).Loading.Equals(load.Value.Loading)).First() : null;
            if (match != null)
            {
              match.Value.ElementRefs.Add(nRef);
            }
            else
            {
              load.Value.ElementRefs = new List<string>() { nRef };
              loadSubList.Add(load);
            }
          }
        }

        loads.AddRange(loadSubList);
      }

      Initialiser.GsaKit.GSASenderObjects.AddRange(loads);

      return (loads.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
