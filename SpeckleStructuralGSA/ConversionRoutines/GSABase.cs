using System.Collections.Generic;
using SpeckleGSAInterfaces;
using SpeckleCore;
using System;

namespace SpeckleStructuralGSA
{
  public abstract class GSABase<T> : IGSASpeckleContainer, IGSAContainer<T> where T : SpeckleObject, new()
  {
    public int GSAId { get; set; }
    public string GWACommand { get; set; }
    public List<string> SubGWACommand { get; set; } = new List<string>();
    public object SpeckleObject { get => this.Value; set { this.Value = (T) value; } }

    public T Value { get; set; }

    public GSABase(T value)
    {
      this.Value = value;
    }

    public GSABase()
    {
      this.Value = new T();
    }
  }
}
