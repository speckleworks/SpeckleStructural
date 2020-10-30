using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.AXIS, GwaSetCommandType.Set, StreamBucket.Model)]
  public class GsaAxis : GsaRecord
  {
    //Only supporting cartesian at this stage
    public string Name { get => name; set { name = value; } }
    public double OriginX;
    public double OriginY;
    public double OriginZ;
    public double? XDirX;
    public double? XDirY;
    public double? XDirZ;
    public double? XYDirX;
    public double? XYDirY;
    public double? XYDirZ;

    public GsaAxis() : base()
    {
      //Defaults
      Version = 1;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //AXIS.1 | num | name | type | Ox | Oy | Oz | Xx | Xy | Xz | XYx | XYy | Xyz
      FromGwaByFuncs(items, out remainingItems, AddName);
      items = remainingItems;

      //Zero values are valid for origin, but not for vectors below
      OriginX = items[0].ToDouble();
      OriginY = items[1].ToDouble();
      OriginZ = items[2].ToDouble();
      items = items.Skip(3).ToList();

      //Zero values aren't valid for vectors - so these are to be treated as nullable
      var values = items.Select(i => (double.TryParse(i, out var d) && d > 0) ? (double?)d : null).ToArray();

      XDirX = values[0];
      XDirY = values[1];
      XDirZ = values[2];
      XYDirX = values[3];
      XYDirY = values[4];
      XYDirZ = values[5];

      return true;
    }

    public override bool Gwa(out string gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = "";
        return false;
      }

      //AXIS.1 | num | name | type | Ox | Oy | Oz | Xx | Xy | Xz | XYx | XYy | Xyz
      AddItems(ref items, Name, "CART", OriginX, OriginY, OriginZ, XDirX, XDirY, XDirZ, XYDirX, XYDirY, XYDirZ);

      return Join(items, out gwa);
    }
  }
}
