using System;
using System.Collections.Generic;
using System.Linq;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [GsaType(GwaKeyword.MEMB, GwaSetCommandType.Set, StreamBucket.Model, true, false)]
  public class GsaMemb : GsaRecord
  {
    //Not supporting: 3D members, or 2D reinforcement
    public string Name { get => name; set { name = value; } }
    public Colour Colour;
    public MemberType Type;

    #region members_for_1D_2D

    public ExposedSurfaces Exposure;
    public int? PropertyIndex;
    public int? Group;
    public List<int> NodeIndices;           //Perimeter/edge topology
    public List<List<int>> Voids;           //Void topologies corresponding to the V sections in the GWA string, like in "41 42 43 44 V(45 46 47 48)"
    public List<int> PointNodeIndices;      //Points to include, corresponding to the P list in the GWA string, like in "41 42 43 44 P(50 55)"
    public List<int> Polylines;             //Polyline topologies correspoding to the L sections in the GWA string, like in "41 42 43 44 L(71 72 73)"
    public List<List<int>> AdditionalAreas; //Additional solid area topologies corresponding to the A sections in the GWA string, like in "41 42 43 44 A(45 46 47 48)"
    public int? OrientationNodeIndex;
    public double Angle;
    public double? MeshSize;
    public bool IsIntersector;
    public AnalysisType AnalysisType;
    public FireResistance Fire;
    public double? LimitingTemperature;
    public int CreationFromStartDays;
    public int StartOfDryingDays;
    public int AgeAtLoadingDays;
    public int RemovedAtDays;
    public bool Dummy;

    #region members_1D
    public Dictionary<AxisDirection6, ReleaseCode> Releases1;
    public List<double> Stiffnesses1;
    public Dictionary<AxisDirection6, ReleaseCode> Releases2;
    public List<double> Stiffnesses2;
    public Restraint RestraintEnd1;
    public Restraint RestraintEnd2;
    public EffectiveLengthType EffectiveLengthType;
    public double? LoadHeight;
    public LoadHeightReferencePoint LoadHeightReferencePoint;
    public bool MemberHasOffsets;
    public bool End1AutomaticOffset;
    public bool End2AutomaticOffset;
    public double? End1OffsetX;
    public double? End2OffsetX;
    public double? OffsetY;
    public double? OffsetZ;

    #region members_1D_eff_len
    //Only one of each set of EffectiveLength__ and Fraction__ values will be filled.  This could be reviewed and refactored accordingly
    public double? EffectiveLengthYY;
    public double? PercentageYY;  //Range: 0-100
    public double? EffectiveLengthZZ;
    public double? PercentageZZ;  //Range: 0-100
    public double? EffectiveLengthLateralTorsional;
    public double? FractionLateralTorsional;  //Range: 0-1
    #endregion

    #region members_1D_explicit
    //Supporting "shortcuts" only, not restraint definitions down to the level of rotational (F1, F2, XX, YY, ZZ) and translational (F1, F2, Z, Y) restraints
    public List<RestraintDefinition> SpanRestraints;
    public List<RestraintDefinition> PointRestraints;
    #endregion

    #endregion

    #region members_2D
    public double? Offset2dZ;
    public double? OffsetAutomaticInternal;
    //Stored as string now but some separation into separate members
    public string Reinforcement;
    #endregion

    #endregion

    public GsaMemb() : base()
    {
      Version = 8;
    }

    public override bool FromGwa(string gwa)
    {
      throw new NotImplementedException();
    }

    public override bool Gwa(out List<string> gwa, bool includeSet = false)
    {
      gwa = new List<string>();
      //Just supporting non-void 1D types at this stage
      if (!(Type == MemberType.Beam || Type == MemberType.Generic1d || Type == MemberType.Column) || !InitialiseGwa(includeSet, out var items))
      {
        return false;
      }

      var axisDirs = Enum.GetValues(typeof(AxisDirection6)).Cast<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | AUTOMATIC | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EFF_LEN | lyy | lzz | llt | load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      //MEMB.8 | num | name | colour | type(1D) | exposure | prop | group | topology | node | angle | mesh_size | is_intersector | analysis_type | fire | limiting_temperature | time[4] | dummy | rls_1 { | k_1 } rls_2 { | k_2 } | restraint_end_1 | restraint_end_2 | EXPLICIT | num_pt | { pt | rest | } | num_span | { span | rest | } load_height | load_ref | is_off { | auto_off_x1 | auto_off_x2 | off_x1 | off_x2 | off_y | off_z }
      AddItems(ref items, Name, Colour.NO_RGB.ToString(), Type.GetStringValue(), Exposure.ToString(), PropertyIndex ?? 0, Group ?? 0,
        AddTopology(), OrientationNodeIndex ?? 0, Angle, MeshSize ?? 0, IsIntersector ? "YES" : "NO", AddAnalysisType(), (int)Fire, LimitingTemperature ?? 0,
        CreationFromStartDays, StartOfDryingDays, AgeAtLoadingDays, RemovedAtDays, Dummy ? "DUMMY" : "ACTIVE");
      
      AddEndReleaseItems(ref items, Releases1, Stiffnesses1, axisDirs);
      AddEndReleaseItems(ref items, Releases2, Stiffnesses2, axisDirs);

      AddItems(ref items, RestraintEnd1.GetStringValue(), RestraintEnd2.GetStringValue(), EffectiveLengthType.ToString());

      if (EffectiveLengthType == EffectiveLengthType.EFF_LEN)
      {
        AddItems(ref items, 
          AddEffectiveLength(EffectiveLengthYY, PercentageYY), 
          AddEffectiveLength(EffectiveLengthZZ, PercentageZZ),
          AddEffectiveLength(EffectiveLengthLateralTorsional, FractionLateralTorsional));
      }
      else if (EffectiveLengthType == EffectiveLengthType.EXPLICIT)
      {
        AddExplicitItems(ref items, PointRestraints);
        AddExplicitItems(ref items, SpanRestraints);
      }

      AddItems(ref items, LoadHeight ?? 0, LoadHeightReferencePoint.GetStringValue(), MemberHasOffsets ? "OFF" : "NO_OFF");

      if (MemberHasOffsets)
      {
        AddItems(ref items, AddAutoOrMan(End1AutomaticOffset), AddAutoOrMan(End2AutomaticOffset), End1OffsetX ?? 0, End2OffsetX ?? 0, OffsetY ?? 0, OffsetZ ?? 0);
      }

      gwa = (Join(items, out var gwaLine)) ? new List<string>() { gwaLine } : new List<string>();
      return gwa.Count() > 0;
    }

    #region other_Add_x_Items_fns
    private void AddEndReleaseItems(ref List<string> items, Dictionary<AxisDirection6, ReleaseCode> releases, List<double> stiffnesses, List<AxisDirection6> axisDirs)
    {
      var rls = "";
      var stiffnessIndex = 0;
      foreach (var d in axisDirs)
      {
        var releaseCode = (releases != null && releases.Count() > 0 && releases.ContainsKey(d)) ? releases[d] : ReleaseCode.Fixed;
        rls += releaseCode.GetStringValue();
        if (releaseCode == ReleaseCode.Stiff && releases.ContainsKey(d) && (++stiffnessIndex) < stiffnesses.Count())
        {
          stiffnesses.Add(stiffnesses[stiffnessIndex]);
        }
      }
      items.Add(rls);
      if (stiffnesses != null && stiffnesses.Count() > 0)
      {
        items.AddRange(stiffnesses.Select(s => s.ToString()));
      }
      return;
    }

    private void AddExplicitItems(ref List<string> items, List<RestraintDefinition> restraintDefinitions)
    {
      items.Add(restraintDefinitions.Count().ToString());
      //Let 0 mean "all" too in light of the fact that all is written as 0 in the GWA
      var allDef = restraintDefinitions.Where(rd => rd.All || rd.Index == 0);

      if (allDef.Count() > 0)
      {
        items.AddRange(new[] { 0.ToString(), allDef.First().Restraint.GetStringValue() });
        return;
      }
      var orderedRestrDef = restraintDefinitions.OrderBy(rd => rd.Index).ToList();
      foreach (var rd in orderedRestrDef)
      {
        items.AddRange(new[] { Index.ToString(), rd.Restraint.GetStringValue() });
      }
      return;
    }
    #endregion

    private string AddTopology()
    {
      var topoPortions = new List<string>
      {
        string.Join(" ", NodeIndices)
      };

      if (Voids != null && Voids.Count() > 0 && Voids.Any(v => v != null && v.Count() > 0))
      {
        var topoVoids = new List<string>();
        foreach (var vList in Voids.Where(v => v != null))
        {
          topoVoids.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoVoids));
      }

      if (PointNodeIndices != null && PointNodeIndices.Count() > 0)
      {
        topoPortions.Add("P(" + string.Join(" ", PointNodeIndices) + ")");
      }

      if (Polylines != null && Polylines.Count() > 0)
      {
        topoPortions.Add("L(" + string.Join(" ", PointNodeIndices) + ")");
      }

      if (AdditionalAreas != null && AdditionalAreas.Count() > 0 && AdditionalAreas.Any(v => v != null && v.Count() > 0))
      {
        var topoAdditional = new List<string>();
        foreach (var vList in AdditionalAreas.Where(v => v != null))
        {
          topoAdditional.Add("V(" + string.Join(" ", vList) + ")");
        }
        topoPortions.Add(string.Join(" ", topoAdditional));
      }
      return string.Join(" ", topoPortions);
    }

    private string AddTime()
    {
      return string.Join(" ", new[] { CreationFromStartDays, StartOfDryingDays, AgeAtLoadingDays, RemovedAtDays });
    }

    private string AddAnalysisType()
    {
      //TO DO: some validation here to ensure a valid combination of MemberType and AnalysisType
      return AnalysisType.ToString();
    }

    private string AddAutoOrMan(bool val)
    {
      return (val ? "AUTO" : "MAN");
    }

    private string AddEffectiveLength(double? el, double? fraction)
    {
      return ((el == null || el.Value == 0) && fraction.HasValue && fraction.Value > 0) 
        ? (fraction.Value + "%") 
        : (el.HasValue) 
            ? el.Value.ToString() 
            : 0.ToString();
    }
  }

  public struct RestraintDefinition
  {
    public bool All;
    public int? Index;
    public Restraint Restraint;
  }

}
