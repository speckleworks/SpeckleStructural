using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using SpeckleStructuralClasses;
using SQLite;

namespace SpeckleStructuralGSA
{
  [GSAObject("PROP_SEC.3", new string[] { "MAT_STEEL.3", "MAT_CONCRETE.16" }, "properties", true, true, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) }, new Type[] { typeof(GSAMaterialSteel), typeof(GSAMaterialConcrete) })]
  public class GSA1DProperty : IGSASpeckleContainer
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public dynamic Value { get; set; } = new Structural1DProperty();

    public void ParseGWACommand(GSAInterfacer GSA, string GSAUnits, List<GSAMaterialSteel> steels, List<GSAMaterialConcrete> concretes)
    {
      if (this.GWACommand == null)
        return;

      Structural1DProperty obj = new Structural1DProperty();

      string[] pieces = this.GWACommand.ListSplit("\t");

      int counter = 1; // Skip identifier
      this.GSAId = Convert.ToInt32(pieces[counter++]);
      obj.ApplicationId = GSA.GetSID(this.GetGSAKeyword(), this.GSAId);
      obj.Name = pieces[counter++].Trim(new char[] { '"' });
      counter++; // Color
      string materialType = pieces[counter++];
      string materialGrade = pieces[counter++];
      if (materialType == "STEEL")
      {
        if (steels != null)
        {
          GSAMaterialSteel matchingMaterial = steels.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }
      else if (materialType == "CONCRETE")
      {
        if (concretes != null)
        {
          GSAMaterialConcrete matchingMaterial = concretes.Where(m => m.GSAId.ToString() == materialGrade).FirstOrDefault();
          obj.MaterialRef = matchingMaterial == null ? null : matchingMaterial.Value.ApplicationId;
          if (matchingMaterial != null)
            this.SubGWACommand.Add(matchingMaterial.GWACommand);
        }
      }

      counter++; // Analysis material
      string shapeDesc = pieces[counter++];
      counter++; // Cost

      obj = SetDesc(obj, shapeDesc, GSAUnits);

      this.Value = obj;
    }

    public void SetGWACommand(GSAInterfacer GSA, string GSAUnits)
    {
      if (this.Value == null)
        return;

      Structural1DProperty prop = this.Value as Structural1DProperty;

      if (prop.Profile == null)
        return;

      string keyword = typeof(GSA1DProperty).GetGSAKeyword();

      int index = GSA.Indexer.ResolveIndex(typeof(GSA1DProperty), prop);
      int materialRef = 0;
      string materialType = "UNDEF";

      var res = GSA.Indexer.LookupIndex(typeof(GSAMaterialSteel), prop.MaterialRef);
      if (res.HasValue)
      {
        materialRef = res.Value;
        materialType = "STEEL";
      }
      else
      {
        res = GSA.Indexer.LookupIndex(typeof(GSAMaterialConcrete), prop.MaterialRef);
        if (res.HasValue)
        {
          materialRef = res.Value;
          materialType = "CONCRETE";
        }
      }

      List<string> ls = new List<string>();

      ls.Add("SET");
      ls.Add(keyword + ":" + GSA.GenerateSID(prop));
      ls.Add(index.ToString());
      ls.Add(prop.Name == null || prop.Name == "" ? " " : prop.Name);
      ls.Add("NO_RGB");
      ls.Add(materialType);
      ls.Add(materialRef.ToString());
      ls.Add("0"); // Analysis material
      ls.Add(GetGSADesc(prop, GSAUnits));
      ls.Add("0"); // Cost

      GSA.RunGWACommand(string.Join("\t", ls));
    }

    private Structural1DProperty SetDesc(Structural1DProperty prop, string desc, string gsaUnit)
    {
      string[] pieces = desc.ListSplit("%");

      switch (pieces[0])
      {
        case "STD":
          return SetStandardDesc(prop, desc, gsaUnit);
        case "GEO":
          return SetGeometryDesc(prop, desc, gsaUnit);
        case "CAT":
          string transformed = TransformCategorySection(desc);
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
      string[] pieces = desc.ListSplit("%");

      string unit = Regex.Match(pieces[1], @"(?<=\()(.+)(?=\))").Value;
      if (unit == "") unit = "mm";

      string type = pieces[1].Split(new char[] { '(' })[0];

      if (type == "R")
      {
        // Rectangle
        double height = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
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
        double height = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double t1 = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
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
        double diameter = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpeckleCircle(
            new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
            diameter / 2);
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Circular;
        prop.Hollow = false;
      }
      else if (type == "CHS")
      {
        // Hollow Circle
        double diameter = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double t = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        prop.Profile = new SpeckleCircle(
            new SpecklePlane(new SpecklePoint(0, 0, 0),
                new SpeckleVector(0, 0, 1),
                new SpeckleVector(1, 0, 0),
                new SpeckleVector(0, 1, 0)),
            diameter / 2);
        (prop.Profile as SpecklePolyline).Closed = true;
        prop.Shape = Structural1DPropertyShape.Circular;
        prop.Hollow = true;
        prop.Thickness = t;
      }
      else if (type == "I")
      {
        // I Section
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        double flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        double flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        double flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double webThickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
        double flangeThickness = Convert.ToDouble(pieces[5]).ConvertUnit(unit, gsaUnit);

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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double topWidth = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double bottomWidth = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);
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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        int index = Convert.ToInt32(pieces[4]);

        List<double> coor = new List<double>();
        for (int i = 0; i < 360; i += 10)
        {
          double radius =
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
        double depth = Convert.ToDouble(pieces[2]).ConvertUnit(unit, gsaUnit);
        double width = Convert.ToDouble(pieces[3]).ConvertUnit(unit, gsaUnit);
        double thickness = Convert.ToDouble(pieces[4]).ConvertUnit(unit, gsaUnit);

        List<double> coor = new List<double>();
        for (int i = 0; i < 360; i += 10)
        {
          double radius =
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
        // TODO: IMPLEMENT ALL SECTIONS
      }

      return prop;
    }

    private Structural1DProperty SetGeometryDesc(Structural1DProperty prop, string desc, string gsaUnit)
    {
      string[] pieces = desc.ListSplit("%");

      string unit = Regex.Match(pieces[1], @"(?<=()(.*?)(?=))").Value;
      if (unit == "") unit = "mm";

      string type = pieces[1].Split(new char[] { '(' })[0];

      if (type == "P")
      {
        // Perimeter Section
        List<double> coor = new List<double>();

        MatchCollection points = Regex.Matches(desc, @"(?<=\()(.*?)(?=\))");
        foreach (Match point in points)
        {
          try
          {
            string[] n = point.Value.Split(new char[] { '|' });

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
        SpeckleCircle profile = prop.Profile as SpeckleCircle;

        if (prop.Hollow)
          return "STD%CHS(" + gsaUnit + ")%" + (profile.Radius * 2).ToString() + "%" + prop.Thickness.ToString();
        else
          return "STD%C(" + gsaUnit + ")%" + (profile.Radius * 2).ToString();
      }

      if (prop.Profile is SpecklePolyline)
      {
        List<double> X = (prop.Profile as SpecklePolyline).Value.Where((x, i) => i % 3 == 0).ToList();
        List<double> Y = (prop.Profile as SpecklePolyline).Value.Where((x, i) => i % 3 == 1).ToList();
        if (prop.Shape == Structural1DPropertyShape.Circular)
        {
          if (prop.Hollow)
            return "STD%CHS(" + gsaUnit + ")%" + (X.Max() - X.Min()).ToString() + "%" + prop.Thickness.ToString();
          else
            return "STD%C(" + gsaUnit + ")%" + (X.Max() - X.Min()).ToString();
        }
        else if (prop.Shape == Structural1DPropertyShape.Rectangular)
        {
          if (prop.Hollow)
            return "STD%RHS(" + gsaUnit + ")%" + (Y.Max() - Y.Min()).ToString() + "%" + (X.Max() - X.Min()).ToString() + "%" + prop.Thickness.ToString() + "%" + prop.Thickness.ToString();
          else
            return "STD%R(" + gsaUnit + ")%" + (Y.Max() - Y.Min()).ToString() + "%" + (X.Max() - X.Min()).ToString();
        }
        else if (prop.Shape == Structural1DPropertyShape.I)
        {
          List<double> xDist = X.Distinct().ToList();
          List<double> yDist = Y.Distinct().ToList();

          xDist.Sort();
          yDist.Sort();

          if (xDist.Count() == 4 && yDist.Count() == 4)
          {
            double width = xDist.Max() - xDist.Min();
            double depth = yDist.Max() - yDist.Min();
            double T = yDist[3] - yDist[2];
            double t = xDist[2] - xDist[1];

            return "STD%I(" + gsaUnit + ")%" + depth.ToString() + "%" + width.ToString() + "%" + T.ToString() + "%" + t.ToString();
          }
        }
        else if (prop.Shape == Structural1DPropertyShape.T)
        {
          List<double> xDist = X.Distinct().ToList();
          List<double> yDist = Y.Distinct().ToList();

          xDist.Sort();
          yDist.Sort();

          if (xDist.Count() == 4 && yDist.Count() == 3)
          {
            double width = xDist.Max() - xDist.Min();
            double depth = yDist.Max() - yDist.Min();
            double T = yDist[2] - yDist[1];
            double t = xDist[2] - xDist[1];

            return "STD%T(" + gsaUnit + ")%" + depth.ToString() + "%" + width.ToString() + "%" + T.ToString() + "%" + t.ToString();
          }
        }
        // Structural1DPropertyShape.Generic



        if (X.Count() < 3 || Y.Count() < 3) return "";

        List<string> ls = new List<string>();

        ls.Add("GEO");
        if (gsaUnit == "mm")
          ls.Add("P");
        else
          ls.Add("P(" + gsaUnit + ")");

        for (int i = 0; i < X.Count(); i++)
        {
          string point = ((i == 0) ? "M" : "L") + "(" + X[i].ToString() + "|" + Y[i].ToString() + ")";

          ls.Add(point);
        }

        if (prop.Voids != null && prop.Voids.Count() > 0)
        {
          foreach (var propVoid in prop.Voids)
          {
            X = (propVoid as SpecklePolyline).Value.Where((x, i) => i % 3 == 0).ToList();
            Y = (propVoid as SpecklePolyline).Value.Where((x, i) => i % 3 == 1).ToList();

            for (int i = 0; i < X.Count(); i++)
            {
              string point = ((i == 0) ? "M" : "L") + "(" + X[i].ToString() + "|" + Y[i].ToString() + ")";

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
      string[] pieces = description.ListSplit("%");

      string DbPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) + @"\Oasys\GSA 10.0\sectlib.db3";

      try
      {
        using (SQLiteConnection conn = new SQLiteConnection(DbPath, SQLiteOpenFlags.ReadOnly))
        {
          string query_type = "SELECT TYPE_NUM, TYPE_ABR" +
              " FROM Types" +
              " WHERE TYPE_ABR = ?";

          IEnumerable<GSASectionType> type = conn.Query<GSASectionType>(query_type, new object[] { pieces[1] });

          if (type.Count() == 0)
            return null;

          int typeCounter = 0;

          GSASection s = null;

          while (typeCounter < type.Count())
          { 
            int typeNum = type.ToList()[typeCounter].TYPE_NUM;

            string query_sect = "SELECT SECT_NAME, SECT_SHAPE, SECT_TYPE_NUM, SECT_DEPTH_DIAM, SECT_WIDTH, SECT_WEB_THICK, SECT_FLG_THICK" +
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
      string DbPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles) + @"\Oasys\GSA 10.0\sectlib.db3";

      try
      {
        using (SQLiteConnection conn = new SQLiteConnection(DbPath, SQLiteOpenFlags.ReadOnly))
        {
          string query_sect = "SELECT SECT_NAME, SECT_SHAPE, SECT_TYPE_NUM, SECT_DEPTH_DIAM, SECT_WIDTH, SECT_WEB_THICK, SECT_FLG_THICK" +
              " FROM Sect" +
              " WHERE lower(SECT_NAME) = lower(?)" +
              " ORDER BY SECT_DATE_ADDED DESC LIMIT 1";

          IEnumerable<GSASection> sect = conn.Query<GSASection>(query_sect, new object[] { name });

          if (sect.Count() == 0)
            return null;

          int typeNum = sect.ToList()[0].SECT_TYPE_NUM;

          string query_type = "SELECT TYPE_NUM, TYPE_ABR" +
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
    public static bool ToNative(this Structural1DProperty prop)
    {
      new GSA1DProperty() { Value = prop }.SetGWACommand(GSA, GSAUnits);

      return true;
    }

    public static SpeckleObject ToSpeckle(this GSA1DProperty dummyObject)
    {
      if (!GSASenderObjects.ContainsKey(typeof(GSA1DProperty)))
        GSASenderObjects[typeof(GSA1DProperty)] = new List<object>();

      List<GSA1DProperty> props = new List<GSA1DProperty>();
      List<GSAMaterialSteel> steels = GSASenderObjects[typeof(GSAMaterialSteel)].Cast<GSAMaterialSteel>().ToList();
      List<GSAMaterialConcrete> concretes = GSASenderObjects[typeof(GSAMaterialConcrete)].Cast<GSAMaterialConcrete>().ToList();

      string keyword = typeof(GSA1DProperty).GetGSAKeyword();
      string[] subKeywords = typeof(GSA1DProperty).GetSubGSAKeyword();

      string[] lines = GSA.GetGWARecords("GET_ALL\t" + keyword);
      List<string> deletedLines = GSA.GetDeletedGWARecords("GET_ALL\t" + keyword).ToList();
      foreach (string k in subKeywords)
        deletedLines.AddRange(GSA.GetDeletedGWARecords("GET_ALL\t" + k));

      // Remove deleted lines
      GSASenderObjects[typeof(GSA1DProperty)].RemoveAll(l => deletedLines.Contains((l as IGSASpeckleContainer).GWACommand));
      foreach (KeyValuePair<Type, List<object>> kvp in GSASenderObjects)
        kvp.Value.RemoveAll(l => (l as IGSASpeckleContainer).SubGWACommand.Any(x => deletedLines.Contains(x)));

      // Filter only new lines
      string[] prevLines = GSASenderObjects[typeof(GSA1DProperty)].Select(l => (l as IGSASpeckleContainer).GWACommand).ToArray();
      string[] newLines = lines.Where(l => !prevLines.Contains(l)).ToArray();

      foreach (string p in newLines)
      {
        GSA1DProperty prop = new GSA1DProperty() { GWACommand = p };
        prop.ParseGWACommand(GSA, GSAUnits, steels, concretes);
        props.Add(prop);
      }

      GSASenderObjects[typeof(GSA1DProperty)].AddRange(props);

      if (props.Count() > 0 || deletedLines.Count() > 0) return new SpeckleObject();

      return new SpeckleNull();
    }
  }
}
