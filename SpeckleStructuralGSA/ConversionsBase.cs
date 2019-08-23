using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Gsa_10_0;
using SQLite;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA
{
  public static partial class Conversions
  {
    public static GSATargetLayer GSATargetLayer { get => (Initialiser.Settings.TargetDesignLayer) ? GSATargetLayer.Design : GSATargetLayer.Analysis ; }
  }
}
