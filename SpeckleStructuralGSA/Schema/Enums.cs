namespace SpeckleStructuralGSA.Schema
{
  //These should be without version numbers
  public enum GwaKeyword
  {
    [StringValue("LOAD_NODE")]
    LOAD_NODE,
    [StringValue("NODE")]
    NODE,
    [StringValue("AXIS")]
    AXIS,
    [StringValue("LOAD_TITLE")]
    LOAD_TITLE,
    [StringValue("LOAD_GRID_AREA")]
    LOAD_GRID_AREA,
    [StringValue("GRID_SURFACE")]
    GRID_SURFACE,
    [StringValue("GRID_PLANE")]
    GRID_PLANE,
    [StringValue("EL")]
    EL,
    [StringValue("MEMB")]
    MEMB,
    [StringValue("LOAD_BEAM")]
    LOAD_BEAM,
    [StringValue("LOAD_BEAM_POINT")]
    LOAD_BEAM_POINT,
    [StringValue("LOAD_BEAM_UDL")]
    LOAD_BEAM_UDL,
    [StringValue("LOAD_BEAM_LINE")]
    LOAD_BEAM_LINE,
    [StringValue("LOAD_BEAM_PATCH")]
    LOAD_BEAM_PATCH,
    [StringValue("LOAD_BEAM_TRILIN")]
    LOAD_BEAM_TRILIN,
    [StringValue("ASSEMBLY")]
    ASSEMBLY
  }

  public enum CurveType
  {
    NotSet = 0,
    Lagrange,
    Circular
  }

  public enum PointDefinition
  {
    NotSet = 0,
    Point,
    Spacing,
    Storey,
    Explicit
  }

  public enum GridExpansion
  {
    NotSet = 0,
    Legacy = 1,
    PlaneAspect = 2,
    PlaneSmooth = 3,
    PlaneCorner = 4
  }

  public enum GridSurfaceSpan
  {
    NotSet = 0,
    One = 1,
    Two = 2
  }

  public enum GridSurfaceElementsType
  {
    NotSet = 0,
    OneD = 1,
    TwoD = 2
  }

  //Note: these enum values map to different integers in GWA than is shown here
  public enum GridPlaneAxisRefType
  {
    NotSet = 0,
    Global,
    XElevation,
    YElevation,
    GlobalCylindrical,
    Reference
  }

  public enum GridPlaneType
  {
    NotSet = 0,
    General = 1,
    Storey = 2 
  }

  public enum AxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Reference
  }

  public enum LoadBeamAxisRefType
  {
    NotSet = 0,
    Global,
    Local,
    Natural,
    Reference
  }

  public enum LoadDirection3
  {
    NotSet = 0,
    X = 1,
    Y = 2,
    Z = 3,
  }

  public enum LoadDirection6
  {
    NotSet = 0,
    X,
    Y,
    Z,
    XX,
    YY,
    ZZ
  }

  public enum LoadAreaOption
  {
    NotSet = 0,
    Plane,
    PolyRef,
    Polygon
  }

  public enum StreamBucket
  {
    Model ,
    Results
  }
}
