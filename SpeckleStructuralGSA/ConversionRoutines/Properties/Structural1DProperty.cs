using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleGSAInterfaces;
using SpeckleStructuralClasses;
using SQLite;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_SEC.3", new string[] { "MAT_STEEL.3", "MAT_CONCRETE.17" }, "model", true, true, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) }, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) })]
  public class GSA1DProperty : GSABase<Structural1DProperty>
  {
    public void ParseGWACommand(string GSAUnits, List<GSAMaterialSteel> steels, List<GSAMaterialConcrete> concretes)
    {
      if (this.GWACommand == null)
        return;

      var obj = new Structural1DProperty();

      var pieces = this.GWACommand.ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);

      var counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = Helper.GetApplicationId(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
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

      counter++; // Analysis material
      var shapeDesc = pieces[counter++];
      counter++; // Cost

      obj = SetDesc(obj, shapeDesc, GSAUnits);

      if (!obj.Properties.ContainsKey("structural"))
      {
        obj.Properties.Add("structural", new Dictionary<string, object>());
      }
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeId", this.GSAId.ToString());
      ((Dictionary<string, object>)obj.Properties["structural"]).Add("NativeSectionProfile", shapeDesc);

      this.Value = obj;
    }

    public string SetGWACommand(string GSAUnits)
    {
      if (this.Value == null)
        return "";

      var prop = this.Value as Structural1DProperty;

      if (prop.ApplicationId == null)
      //if (prop.Profile == null && string.IsNullOrEmpty(prop.CatalogueName))
        return "";

      var keyword = typeof(GSA1DProperty).GetGSAKeyword();

      var index = Initialiser.AppResources.Cache.ResolveIndex(typeof(GSA1DProperty).GetGSAKeyword(), prop.ApplicationId);
      var materialRef = 0;
      var materialType = "UNDEF";

      var res = Initialiser.AppResources.Cache.LookupIndex(typeof(GSAMaterialSteel).GetGSAKeyword(), prop.MaterialRef);
      if (res.HasValue)
      {
        materialRef = res.Value;
        materialType = "STEEL";
      }
      else
      {
        res = Initialiser.AppResources.Cache.LookupIndex(typeof(GSAMaterialConcrete).GetGSAKeyword(), prop.MaterialRef);
        if (res.HasValue)
        {
          materialRef = res.Value;
          materialType = "CONCRETE";
        }
      }
      if (materialRef == 0)
      {
        //If the material reference can't be resolved, then point to generic material (which this code doesn't create anywhere else) with reference 1
        materialRef = 1;
        materialType = "GENERIC";
      }

      var sid = Helper.GenerateSID(prop);
      var ls = new List<string>
      {
        "SET",
        keyword + (string.IsNullOrEmpty(sid) ? "" : ":" + sid),
        index.ToString(),
        prop.Name == null || prop.Name == "" ? " " : prop.Name,
        "NO_RGB",
        materialType,
        materialRef.ToString(),
        "0", // Analysis material
        GetGSADesc(prop, GSAUnits),
        "0", // Cost
        "CENTROID", //this needs review
        "0", //this needs review
        "0" //this needs review
      };

      return (string.Join(Initialiser.AppResources.Proxy.GwaDelimiter.ToString(), ls));
    }

    private Structural1DProperty SetDesc(Structural1DProperty prop, string desc, string gsaUnit)
    {
      var pieces = desc.ListSplit("%");

      switch (pieces[0])
      {
        case "STD":
          return SetStandardDesc(prop, desc, gsaUnit);
        case "GEO":
          return SetGeometryDesc(prop, desc, gsaUnit);
        case "CAT":
          var transformed = TransformCategorySection(desc);
          if (transformed == null)
            return prop;
          prop.CatalogueName = pieces[2];
          return SetStandardDesc(prop, transformed, gsaUnit);
        default:
          return prop;
      }
    }

    private Structural1DProperty SetStandardDesc(Structural1DProperty prop, string desc, string gsaUnit)
    {
      var pieces = desc.ListSplit("%");

      var unit = Regex.Match(pieces[1], @"(?<=\()(.+)(?=\))").Value;
      if (unit == "") unit = "mm";

      var type = pieces[1].Split(new char[] { '(' })[0];

      if (type == "R")
      {
        // Rectangle
        var height = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpecklePolyline(new double[] {
                    width /2, height/2 , 0,
                    -width/2, height/2 , 0,
                    -width/2, -height/2 , 0,
                    width/2, -height/2 , 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Rectangular;
        prop.Hollow = false;
      }
      else if (type == "RHS")
      {
        // Hollow Rectangle
        var height = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var t1 = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        double t2 = 0;
        try
        { 
          t2 = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);
        }
        catch { t2 = t1; }
        prop.Profile = new SpecklePolyline(new double[] {
                    width /2, height/2 , 0,
                    -width/2, height/2 , 0,
                    -width/2, -height/2 , 0,
                    width/2, -height/2 , 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Rectangular;
        prop.Hollow = true;
        prop.Thickness = (t1 + t2) / 2; // TODO: Takes average thickness
      }
      else if (type == "C")
      {
        // Circle
        var diameter = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpeckleCircle(
            new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
            diameter / 2);
        prop.Shape = Structural1DPropertyShape.Circular;
        prop.Hollow = false;
      }
      else if (type == "CHS")
      {
        // Hollow Circle
        var diameter = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var t = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpeckleCircle(
            new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
            diameter / 2);
        prop.Shape = Structural1DPropertyShape.Circular;
        prop.Hollow = true;
        prop.Thickness = t;
      }
      else if (type == "I")
      {
        // I Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        var flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

        prop.Profile = new SpecklePolyline(new double[] {
                    webThickness/2, depth/2 - flangeThickness, 0,
                    width/2, depth/2 - flangeThickness, 0,
                    width/2, depth/2, 0,
                    -width/2, depth/2, 0,
                    -width/2, depth/2 - flangeThickness, 0,
                    -webThickness/2, depth/2 - flangeThickness, 0,
                    -webThickness/2, -(depth/2 - flangeThickness), 0,
                    -width/2, -(depth/2 - flangeThickness), 0,
                    -width/2, -depth/2, 0,
                    width/2, -depth/2, 0,
                    width/2, -(depth/2 - flangeThickness), 0,
                    webThickness/2, -(depth/2 - flangeThickness), 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.I;
        prop.Hollow = false;
      }
      else if (type == "T")
      {
        // T Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        var flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

        prop.Profile = new SpecklePolyline(new double[] {
                    webThickness/2, - flangeThickness, 0,
                    width/2, - flangeThickness, 0,
                    width/2, 0, 0,
                    -width/2, 0, 0,
                    -width/2, - flangeThickness, 0,
                    -webThickness/2, - flangeThickness, 0,
                    -webThickness/2, -depth, 0,
                    webThickness/2, -depth, 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.T;
        prop.Hollow = false;
      }
      else if (type == "CH")
      {
        // Channel Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        var flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

        prop.Profile = new SpecklePolyline(new double[] {
                    webThickness, depth/2 - flangeThickness, 0,
                    width, depth/2 - flangeThickness, 0,
                    width, depth/2, 0,
                    0, depth/2, 0,
                    0, -depth/2, 0,
                    width, -depth/2, 0,
                    width, -(depth/2 - flangeThickness), 0,
                    webThickness, -(depth/2 - flangeThickness), 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = false;
      }
      else if (type == "A")
      {
        // Angle Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        var flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

        prop.Profile = new SpecklePolyline(new double[] {
                    0, 0, 0,
                    width, 0, 0,
                    width, flangeThickness, 0,
                    webThickness, flangeThickness, 0,
                    webThickness, depth, 0,
                    0, depth, 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = false;
      }
      else if (type == "TR")
      {
        // Taper Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var topWidth = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var bottomWidth = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpecklePolyline(new double[] {
                    topWidth /2, depth/2 , 0,
                    -topWidth/2, depth/2 , 0,
                    -bottomWidth/2, -depth/2 , 0,
                    bottomWidth/2, -depth/2 , 0});
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = false;
      }
      else if (type == "E")
      {
        // Ellipse Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var index = Convert.ToInt32(pieces[4]);

        var coor = new List<double>();
        for (var i = 0; i < 360; i += 10)
        {
          var radius =
              depth * width / Math.Pow(
                  Math.Pow(depth * Math.Cos(i.ToRadians()), index)
                  + Math.Pow(width * Math.Sin(i.ToRadians()), index),
                  1 / index);

          coor.Add(radius * Math.Cos(i.ToRadians()));
          coor.Add(radius * Math.Sin(i.ToRadians()));
          coor.Add(0);
        }
        prop.Profile = new SpecklePolyline(coor.ToArray());
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = false;
      }
      else if (type == "OVAL")
      {
        // Oval Section
        var depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        var width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        var thickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);

        var coor = new List<double>();
        for (var i = 0; i < 360; i += 10)
        {
          var radius =
              depth * width / Math.Pow(
                  Math.Pow(depth * Math.Cos(i.ToRadians()), 2)
                  + Math.Pow(width * Math.Sin(i.ToRadians()), 2),
                  1 / 2);

          coor.Add(radius * Math.Cos(i.ToRadians()));
          coor.Add(radius * Math.Sin(i.ToRadians()));
          coor.Add(0);
        }
        prop.Profile = new SpecklePolyline(coor.ToArray());
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = true;
        prop.Thickness = thickness;
      }
      else
      {
        Initialiser.AppResources.Messenger.Message(MessageIntent.Display, MessageLevel.Error, "1D section profile string (" + type + ") is not supported", prop.ApplicationId);  // TODO: IMPLEMENT ALL SECTIONS
      }

      return prop;
    }

    private Structural1DProperty SetGeometryDesc(Structural1DProperty prop, string desc, string gsaUnit)
    {
      var pieces = desc.ListSplit("%");

      var unit = Regex.Match(pieces[1], @"(?<=()(.*?)(?=))").Value;
      if (unit == "") unit = "mm";

      var type = pieces[1].Split(new char[] { '(' })[0];

      if (type == "P")
      {
        // Perimeter Section
        var coor = new List<double>();

        var points = Regex.Matches(desc, @"(?<=\()(.*?)(?=\))");
        foreach (Match point in points)
        {
          try
          {
            var n = point.Value.Split(new char[] { '|' });

            coor.Add(Convert.ToDouble(n[0]).ConvertUnit(unit, gsaUnit));
            coor.Add(Convert.ToDouble(n[1]).ConvertUnit(unit, gsaUnit));
            coor.Add(0);
          }
          catch { }
        }

        prop.Profile = new SpecklePolyline(coor.ToArray());
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Generic;
        prop.Hollow = false;
        return prop;
      }
      else
      {
        // TODO: IMPLEMENT ALL SECTIONS
        return prop;
      }
    }

    private string GetGSADesc(Structural1DProperty prop, string gsaUnit)
    {
      if (!string.IsNullOrEmpty(prop.CatalogueName))
      {
        var desc = GetGSACategorySection(prop.CatalogueName);
        if (!string.IsNullOrEmpty(desc))
          return desc;
      }
        
      if (prop.Profile == null)
        return "";

      if (prop.Profile is SpeckleCircle)
      {
        var profile = prop.Profile as SpeckleCircle;

        if (prop.Hollow.HasValue && prop.Hollow.Value)
          return "STD%CHS(" + gsaUnit + ")%" + (profile.Radius * 2).ToString() + "%" + prop.Thickness.ToString();
        else
          return "STD%C(" + gsaUnit + ")%" + (profile.Radius * 2).ToString();
      }

      if (prop.Profile is SpecklePolyline)
      {
        var X = (prop.Profile as SpecklePolyline).Value.Where((x, i) => i % 3 == 0).ToList();
        var Y = (prop.Profile as SpecklePolyline).Value.Where((x, i) => i % 3 == 1).ToList();
        if (prop.Shape == Structural1DPropertyShape.Circular)
        {
          if (prop.Hollow.HasValue && prop.Hollow.Value)
            return "STD%CHS(" + gsaUnit + ")%" + (X.Max() - X.Min()).ToString() + "%" + prop.Thickness.ToString();
          else
            return "STD%C(" + gsaUnit + ")%" + (X.Max() - X.Min()).ToString();
        }
        else if (prop.Shape == Structural1DPropertyShape.Rectangular)
        {
          if (prop.Hollow.HasValue && prop.Hollow.Value)
            return "STD%RHS(" + gsaUnit + ")%" + (Y.Max() - Y.Min()).ToString() + "%" + (X.Max() - X.Min()).ToString() + "%" + prop.Thickness.ToString() + "%" + prop.Thickness.ToString();
          else
            return "STD%R(" + gsaUnit + ")%" + (Y.Max() - Y.Min()).ToString() + "%" + (X.Max() - X.Min()).ToString();
        }
        else if (prop.Shape == Structural1DPropertyShape.I)
        {
          var xDist = X.Distinct().ToList();
          var yDist = Y.Distinct().ToList();

          xDist.Sort();
          yDist.Sort();

          if (xDist.Count() == 4 && yDist.Count() == 4)
          {
            var width = xDist.Max() - xDist.Min();
            var depth = yDist.Max() - yDist.Min();
            var T = yDist[3] - yDist[2];
            var t = xDist[2] - xDist[1];

            return "STD%I(" + gsaUnit + ")%" + depth.ToString() + "%" + width.ToString() + "%" + T.ToString() + "%" + t.ToString();
          }
        }
        else if (prop.Shape == Structural1DPropertyShape.T)
        {
          var xDist = X.Distinct().ToList();
          var yDist = Y.Distinct().ToList();

          xDist.Sort();
          yDist.Sort();

          if (xDist.Count() == 4 && yDist.Count() == 3)
          {
            var width = xDist.Max() - xDist.Min();
            var depth = yDist.Max() - yDist.Min();
            var T = yDist[2] - yDist[1];
            var t = xDist[2] - xDist[1];

            return "STD%T(" + gsaUnit + ")%" + depth.ToString() + "%" + width.ToString() + "%" + T.ToString() + "%" + t.ToString();
          }
        }
        // Structural1DPropertyShape.Generic



        if (X.Count() < 3 || Y.Count() < 3) return "";

        var ls = new List<string>
        {
          "GEO"
        };
        ls.Add((gsaUnit == "mm") ? "P" : "P(" + gsaUnit + ")");

        for (var i = 0; i < X.Count(); i++)
        {
          var point = ((i == 0) ? "M" : "L") + "(" + X[i].ToString() + "|" + Y[i].ToString() + ")";

          ls.Add(point);
        }

        if (prop.Voids != null && prop.Voids.Count() > 0)
        {
          foreach (var propVoid in prop.Voids)
          {
            X = (propVoid as SpecklePolyline).Value.Where((x, i) => i % 3 == 0).ToList();
            Y = (propVoid as SpecklePolyline).Value.Where((x, i) => i % 3 == 1).ToList();

            for (var i = 0; i < X.Count(); i++)
            {
              var point = ((i == 0) ? "M" : "L") + "(" + X[i].ToString() + "|" + Y[i].ToString() + ")";

              ls.Add(point);
            }
          }
        }

        return string.Join("%", ls);
      }

      return "";
    }

    /// <summary>
    /// Transforms a GSA category section description into a generic section description.
    /// </summary>
    /// <param name="description"></param>
    /// <returns>Generic section description</returns>
    public string TransformCategorySection(string description)
    {
      var pieces = description.ListSplit("%");

      var DbPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) + @"\Oasys\GSA 10.1\sectlib.db3";

      try
      {
        using (var conn = new SQLiteConnection(DbPath, SQLiteOpenFlags.ReadOnly))
        {
          var query_type = "SELECT TYPE_NUM, TYPE_ABR" +
              " FROM Types" +
              " WHERE TYPE_ABR = ?";

          IEnumerable<GSASectionType> type = conn.Query<GSASectionType>(query_type, new object[] { pieces[1] });

          if (type.Count() == 0)
            return null;

          var typeCounter = 0;

          GSASection s = null;

          while (typeCounter < type.Count())
          { 
            var typeNum = type.ToList()[typeCounter].TYPE_NUM;

            var query_sect = "SELECT SECT_NAME, SECT_SHAPE, SECT_TYPE_NUM, SECT_DEPTH_DIAM, SECT_WIDTH, SECT_WEB_THICK, SECT_FLG_THICK" +
                " FROM Sect" +
                " WHERE SECT_TYPE_NUM = ? AND lower(SECT_NAME) = lower(?)";

            IEnumerable<GSASection> sect = conn.Query<GSASection>(query_sect, new object[] { typeNum, pieces[2] });
            if (sect.Count() > 0)
            {
              s = sect.ToList()[0];
              break;
            }

            typeCounter++;
          }

          if (s == null)
            return null;

          switch ((GSACAtSectionType)s.SECT_SHAPE)
          {
            case GSACAtSectionType.I:
              return "STD%I(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK + "%" + s.SECT_FLG_THICK;
            case GSACAtSectionType.CastellatedI:
              return null;
            case GSACAtSectionType.Channel:
              return "STD%CH(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK + "%" + s.SECT_FLG_THICK;
            case GSACAtSectionType.T:
              return "STD%T(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK + "%" + s.SECT_FLG_THICK;
            case GSACAtSectionType.Angles:
              return "STD%A(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK + "%" + s.SECT_FLG_THICK;
            case GSACAtSectionType.DoubleAngles:
              return null;
            case GSACAtSectionType.CircularHollow:
              return "STD%CHS(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WEB_THICK;
            case GSACAtSectionType.Circular:
              return "STD%C(m)%" + s.SECT_DEPTH_DIAM;
            case GSACAtSectionType.RectangularHollow:
              return "STD%RHS(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK + "%" + s.SECT_FLG_THICK;
            case GSACAtSectionType.Rectangular:
              return "STD%R(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH;
            case GSACAtSectionType.Oval:
              return "STD%OVAL(m)%" + s.SECT_DEPTH_DIAM + "%" + s.SECT_WIDTH + "%" + s.SECT_WEB_THICK;
            case GSACAtSectionType.TwoChannelsLaces:
              return null;
            default:
              return null;
          }
        }
      }
      catch { return null; }
    }

    /// <summary>
    /// Gets GSA category section string from name of section.
    /// </summary>
    /// <param name="name">Name of section</param>
    /// <returns>GSA section description or null if error.</returns>
    public string GetGSACategorySection(string name)
    {
      var DbPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) + @"\Oasys\GSA 10.1\sectlib.db3";

      try
      {
        using (var conn = new SQLiteConnection(DbPath, SQLiteOpenFlags.ReadOnly))
        {
          var query_sect = "SELECT SECT_NAME, SECT_SHAPE, SECT_TYPE_NUM, SECT_DEPTH_DIAM, SECT_WIDTH, SECT_WEB_THICK, SECT_FLG_THICK "
            + "FROM Sect s "
            + "INNER JOIN Types t ON s.SECT_TYPE_NUM = t.TYPE_NUM "
            + "WHERE lower(SECT_NAME) = lower(?) "
            + "ORDER BY SECT_DATE_ADDED DESC";

          IEnumerable<GSASection> sect;

          //Temporary work-around for a particular while the absence of some expected rows in the GSA catalogue table(s) is investigated
          if (name.Contains(@"/"))
          {
            var pieces = name.Split(new[] { '/' });
            var grade = pieces[0];
            name = pieces[1];
            
            sect = conn.Query<GSASection>(query_sect, new object[] { name });
            var typeNumbers = sect.Select(s => s.SECT_TYPE_NUM).ToList();

            if (typeNumbers.Count() > 0)
            {
              if (typeNumbers.Contains(124) && grade == "350")
              {
                return "CAT%A-SHS350%" + sect.ToList()[0].SECT_NAME;
              }
              else if (typeNumbers.Contains(125) && grade == "450")
              {
                return "CAT%A-SHS450%" + sect.ToList()[0].SECT_NAME;
              }
            }
          }

          sect = conn.Query<GSASection>(query_sect, new object[] { name });

          if (sect.Count() == 0)
            return null;

          var typeNum = sect.ToList()[0].SECT_TYPE_NUM;

          var query_type = "SELECT TYPE_NUM, TYPE_ABR" +
              " FROM Types" +
              " WHERE TYPE_NUM = ?";

          IEnumerable<GSASectionType> type = conn.Query<GSASectionType>(query_type, new object[] { typeNum });

          if (type.Count() == 0)
            return null;

          return "CAT%" + type.ToList()[0].TYPE_ABR + "%" + sect.ToList()[0].SECT_NAME;
        }
      }
      catch { return null; }
    }
  }

  #region GSA Category Section Helper Classes
  public class GSASection
  {
    public string SECT_NAME { get; set; }
    public int SECT_SHAPE { get; set; }
    public int SECT_TYPE_NUM { get; set; }
    public float SECT_DEPTH_DIAM { get; set; }
    public float SECT_WIDTH { get; set; }
    public float SECT_WEB_THICK { get; set; }
    public float SECT_FLG_THICK { get; set; }
  }

  public class GSASectionType
  {
    public int TYPE_NUM { get; set; }
    public string TYPE_ABR { get; set; }
  }
  #endregion

  public static partial class Conversions
  {
    public static string ToNative(this Structural1DProperty prop)
    {
      return SchemaConversion.Helper.ToNativeTryCatch(prop, () => new GSA1DProperty() { Value = prop }.SetGWACommand(Initialiser.AppResources.Settings.Units));
    }

    public static SpeckleObject ToSpeckle(this GSA1DProperty dummyObject)
    {
      var newLines = ToSpeckleBase<GSA1DProperty>();
      var typeName = dummyObject.GetType().Name;
      var propsLock = new object();
      var props = new SortedDictionary<int, GSA1DProperty>();
      var steels = Initialiser.GsaKit.GSASenderObjects.Get<GSAMaterialSteel>();      
      var concretes = Initialiser.GsaKit.GSASenderObjects.Get<GSAMaterialConcrete>();
      var keyword = dummyObject.GetGSAKeyword();

      Parallel.ForEach(newLines.Keys, k =>
      {
        var pPieces = newLines[k].ListSplit(Initialiser.AppResources.Proxy.GwaDelimiter);
        var gsaId = pPieces[1];
        try
        {
          var prop = new GSA1DProperty() { GWACommand = newLines[k] };
          prop.ParseGWACommand(Initialiser.AppResources.Settings.Units, steels, concretes);
          lock (propsLock)
          {
            props.Add(k, prop);
          }
        }
        catch (Exception ex)
        {
          Initialiser.AppResources.Messenger.Message(MessageIntent.TechnicalLog, MessageLevel.Error, ex,
            "Keyword=" + keyword, "Index=" + k);
        }
      });

      Initialiser.GsaKit.GSASenderObjects.AddRange(props.Values.ToList());

      return (props.Keys.Count > 0) ? new SpeckleObject() : new SpeckleNull();
    }
  }
}
