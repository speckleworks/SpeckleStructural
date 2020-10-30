namespace SpeckleStructuralGSA.Schema
{
  //These should be without version numbers
  public enum GwaKeyword
  {
    [StringValue("LOAD_NODE")]
    LOAD_NODE = 1,
    [StringValue("NODE")]
    NODE = 2,
    [StringValue("AXIS")]
    AXIS = 3,
    [StringValue("LOAD_TITLE")]
    LOAD_TITLE
  }

  public enum LoadDirection
  {
    NotSet = 0,
    X = 1,
    Y = 2,
    Z = 3,
    XX = 4,
    YY = 5,
    ZZ = 6
  }

  public enum StreamBucket
  {
    Model = 0,
    Results = 1
  }
}
