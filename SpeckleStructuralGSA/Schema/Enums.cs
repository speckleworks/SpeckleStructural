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
    Global = 1,
    Local = 2,
    Reference = 3
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
    X = 1,
    Y = 2,
    Z = 3,
    XX = 4,
    YY = 5,
    ZZ = 6
  }

  public enum LoadAreaOption
  {
    NotSet = 0,
    Plane = 1,
    PolyRef = 2,
    Polygon = 3
  }

  public enum StreamBucket
  {
    Model = 0,
    Results = 1
  }
}
