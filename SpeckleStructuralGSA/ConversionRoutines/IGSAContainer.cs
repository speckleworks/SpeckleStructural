using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleStructuralGSA
{
  public interface IGSAContainer<T>
  {
    /// <summary>
    /// Record index of GSA record
    /// </summary>
    int GSAId { get; set; }

    /// <summary>
    /// Associated GWA command
    /// </summary>
    string GWACommand { get; set; }

    /// <summary>
    /// List of GWA records used to read the object.
    /// </summary>
    //List<string> SubGWACommand { get; set; }

    /// <summary>
    /// SpeckleObject created
    /// </summary>
    T Value { get; }
  }
}
