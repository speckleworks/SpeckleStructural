using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema.Loading
{
  public class GsaLoad2dThermal : GsaRecord
  {
    public string Name { get => name; set { name = value; } }
    public List<int> Entities;
    public int? LoadCaseIndex;
    public Load2dThermalType Type;
    public List<double> Values;

    public GsaLoad2dThermal() : base()
    {
      //Defaults
      Version = 2;
    }

    public override bool FromGwa(string gwa)
    {
      if (!BasicFromGwa(gwa, out var remainingItems))
      {
        return false;
      }
      var items = remainingItems;

      //LOAD_2D_THERMAL.2 | name | list | case | type | values(n)
      if (!FromGwaByFuncs(items, out remainingItems, AddName, (v) => AddEntities(v, out Entities), (v) => AddNullableIndex(v, out LoadCaseIndex),
        (v) => Enum.TryParse(v, true, out Type)))
      {
        return false;
      }
      items = remainingItems;

      if (items.Count() > 0)
      {
        Values = new List<double>();
        foreach (var item in items)
        {
          if (double.TryParse(item, out double v))
          {
            Values.Add(v);
          }
          else
          {
            return false;
          }
        }
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

      //LOAD_2D_THERMAL.2 | name | list | case | type | values(n)
      AddItems(ref items, Name, AddEntities(Entities), LoadCaseIndex ?? 0, Type.GetStringValue(), AddValues());

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region to_gwa_fns
    private string AddValues()
    {
      if (Values != null && Values.Count() > 0)
      {
        return string.Join(" ", Values);
      }
      else
      {
        return "";
      }
    }
    #endregion
  }
}
