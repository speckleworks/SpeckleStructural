using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_2D.6", new string[] { "MAT_STEEL.3", "MAT_CONCRETE.17" }, "model", true, true, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) }, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) })]
  public class GSA2DProperty : IGSASpeckleContainer
  {
    public bool IsAxisLocal;

    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural2DProperty();

    public void ParseGWACommand(List<GSAMaterialSteel> steels, List<GSAMaterialConcrete> concretes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural2DProperty();

      var pieces = this.GWACommand.ListSplit("\t");

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
      counter++; // Type
      this.IsAxisLocal = pieces[counter++] == "LOCAL"; // Axis
      counter++; // Analysis material
      var materialType = pieces[counter++];
      var materialGrade = pieces[counter++];
      if (materialType == "STEEL")
      {
        if (steels != null)
        {
          var matchingMaterial = steels.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }
      else if (materialType == "CONCRETE")
      {
        if (concretes != null)
        {
          var matchingMaterial = concretes.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }

      counter++; // design property
      obj.Thickness = Convert.ToDouble(pieces[counter++]); // version 5 and 6 of this command are meant to include 'profile' but it does not yet seem functional and so only thickness is recorded

      switch (pieces[counter++])
      {
        case "CENTROID":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Middle;
          break;
        case "TOP_CENTRE":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Top;
          break;
        case "BOT_CENTRE":
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Bottom;
          break;
        default:
          obj.ReferenceSurface = Structural2DPropertyReferenceSurface.Middle;
          break;
      }
      // Ignore the rest

      this.Value = obj;
    }

    public string SetGWACommand()
    {
      if (this.Value == null)
        return "";

      var prop = this.Value as Structural2DProperty;
      if (prop.ReferenceSurface == Structural2DPropertyReferenceSurface.NotSet)
        return "";

      var keyword = typeof(GSA2DProperty).GetGSAKeyword();

      var index = Initialiser.Cache.ResolveIndex(typeof(GSA2DProperty).GetGSAKeyword(), prop.ApplicationId);
      var materialRef = 1;  //Default to 1 even if there is no such record - better for GSA than a default of 0
      var materialType = "UNDEF";

      var res = Initialiser.Cache.LookupIndex(typeof(GSAMaterialSteel).GetGSAKeyword(), prop.MaterialRef);
      if (res.HasValue)
      {
        materialRef = res.Value;
        materialType = "STEEL";
      }
      else
      {
        res = Initialiser.Cache.LookupIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), prop.MaterialRef);
        if (res.HasValue)
        {
          materialRef = res.Value;
          materialType = "CONCRETE";
        }
      }

      var sid = Helper.GenerateSID(prop);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        prop.Name == null || prop.Name == "" ? " " : prop.Name,
        "NO_RGB",
        "SHELL",
        "GLOBAL",
        "0", // Analysis material
        materialType,
        materialRef.ToString(),
        "0", // Design
        prop.Thickness.ToString()
      };

      switch (prop.ReferenceSurface)
      {
        case Structural2DPropertyReferenceSurface.Middle:
          ls.Add("CENTROID");
          break;
        case Structural2DPropertyReferenceSurface.Top:
          ls.Add("TOP_CENTRE");
          break;
        case Structural2DPropertyReferenceSurface.Bottom:
          ls.Add("BOT_CENTRE");
          break;
        default:
          ls.Add("CENTROID");
          break;
      }
      ls.Add("0"); // Ref_z
      ls.Add("0"); // Mass
      ls.Add("100%"); // Flex modifier
      ls.Add("100%"); // Shear modifier
      ls.Add("100%"); // Inplane modifier
      ls.Add("100%"); // Weight modifier
      ls.Add("NO_ENV"); // Environmental data

      return (string.Join("\t", ls));
    }
  }

  public static partial class Conversions
  {
    public static string ToNative(this Structural2DProperty prop)
    {
      return new GSA2DProperty() { Value = prop }.SetGWACommand();
    }

    public static SpeckleObject ToSpeckle(this GSA2DProperty dummyObject)
    {
      var newLines = ToSpeckleBase<GSA2DProperty>();
      var typeName = dummyObject.GetType().Name;
      var propsLock = new object();
      var props = new List<GSA2DProperty>();
      var steels = Initialiser.GSASenderObjects.Get<GSAMaterialSteel>();
      var concretes = Initialiser.GSASenderObjects.Get<GSAMaterialConcrete>();

      Parallel.ForEach(newLines.Values, p =>
      {
        var pPieces = p.ListSplit("\t");
        var gsaId = pPieces[1];
        try
        {
          var prop = new GSA2DProperty() { GWACommand = p };
          prop.ParseGWACommand(steels, concretes);
          lock (propsLock)
          {
            props.Add(prop);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppUI.Message(typeName + ": " + ex.Message, gsaId);
        }
      });

      Initialiser.GSASenderObjects.AddRange(props);

      return (props.Count() > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
