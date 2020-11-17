using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.NODE, GwaSetCommandType.Set, StreamBucket.Model)]
  public class GsaNode : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public double X;
    public double Y;
    public double Z;

    public GsaNode() : base()
    {
      //Defaults
      Version = 3;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;
      items = items.Skip(1).ToList();  //Skip colour

      //Only basic level of support is offered now - the arguments after x y z are ignored
      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      //Zero values are valid for origin, but not for vectors below
      X = items[0].ToDouble();
      Y = items[1].ToDouble();
      Z = items[2].ToDouble();

      return true;
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      if (!InitialiseGwa(includeSet, out var items))
      {
        gwa = new List<string>();
        return false;
      }

      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      AddItems(ref items, Name, "NO_RGB", X, Y, Z);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }
  }
}
