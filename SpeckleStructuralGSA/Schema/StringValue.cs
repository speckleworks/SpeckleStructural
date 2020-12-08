using System;

namespace SpeckleStructuralGSA.Schema
{
  public class StringValue : Attribute
  {
    public string Value { get; protected set; }

    public StringValue(string v)
    {
      Value = v;
    }
  }
}
