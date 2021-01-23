using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.NODE, GwaSetCommandType.Set, true)]
  public class GsaNode : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public Colour Colour = Colour.NO_RGB;
    public double X;
    public double Y;
    public double Z;
    public NodeRestraint NodeRestraint;
    public List<AxisDirection6> Restraints;
    public AxisRefType AxisRefType;
    public int? AxisIndex;
    public double? MeshSize;
    public int? SpringPropertyIndex;
    public int? MassPropertyIndex;
    public int? DamperPropertyIndex;

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

      //NODE.3 | num | name | colour | x | y | z | restraint | axis | mesh_size | springProperty | massProperty | damperProperty
      //Zero values are valid for origin, but not for vectors below
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => double.TryParse(v, out X), (v) => double.TryParse(v, out Y), (v) => double.TryParse(v, out Z),
        AddRestraints, AddAxis, (v) => AddNullableDoubleValue(v, out MeshSize), (v) => AddNullableIndex(v, out SpringPropertyIndex), 
        (v) => AddNullableIndex(v, out MassPropertyIndex), (v) => AddNullableIndex(v, out DamperPropertyIndex)))
      {
        return false;
      }

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
      AddItems(ref items, Name, "NO_RGB", X, Y, Z, AddRestraints(), AddAxis(), MeshSize ?? 0, SpringPropertyIndex ?? 0, MassPropertyIndex ?? 0, 
        DamperPropertyIndex ?? 0);

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns

    private string AddRestraints()
    {
      return "";
    }

    private string AddAxis()
    {
      return "";
    }
    #endregion

    #region from_gwa_fns
    private bool AddRestraints(string v)
    {
      var boolRestraints = SpeckleStructuralGSA.Helper.RestraintBoolArrayFromCode(v);
      if (boolRestraints == null || boolRestraints.Count() < 6)
      {
        return false;
      }
      if (boolRestraints.All(r => r == false))
      {
        NodeRestraint = NodeRestraint.Free;
      }
      else if (boolRestraints.Take(3).All(r => r == true) && boolRestraints.Skip(3).Take(3).All(r => r == false))
      {
        NodeRestraint = NodeRestraint.Pin;
      }
      else if (boolRestraints.All(r => r == true))
      {
        NodeRestraint = NodeRestraint.Fix;
      }
      else
      {
        NodeRestraint = NodeRestraint.Custom;
        var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(e => e != AxisDirection6.NotSet).ToList();
        for (var i = 0; i < 6; i++)
        {
          if (boolRestraints[i])
          {
            if (Restraints == null)
            {
              Restraints = new List<AxisDirection6>();
            }
            //This list signifies the true/positive values
            Restraints.Add(axisDirs[i]);
          }
        }
      }

      return true;
    }

    private bool AddAxis(string v)
    {
      if (v.Trim().Equals(AxisRefType.Global.ToString(), StringComparison.InvariantCultureIgnoreCase))
      {
        AxisRefType = AxisRefType.Global;
        return true;
      }
      else
      {
        AxisRefType = AxisRefType.Reference;
        return AddNullableIndex(v, out AxisIndex);
      }
    }

    #endregion
  }
}
