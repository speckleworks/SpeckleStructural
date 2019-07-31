using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleStructuralGSA
{
  /// <summary>
  /// Attribute containing read and write information for the object.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class GSAObject : Attribute
  {
    /// <summary>
    /// GSA Keyword
    /// </summary>
    private string gsaKeyword;

    /// <summary>
    /// GSA keywords the object depends on
    /// </summary>
    private string[] subGsaKeywords;

    /// <summary>
    /// Stream name, if sending to seperate streams
    /// </summary>
    private string stream;

    /// <summary>
    /// Is the object is on the analysis layer?
    /// </summary>
    private bool analysisLayer;

    /// <summary>
    /// Is the object is on the design layer?
    /// </summary>
    private bool designLayer;

    /// <summary>
    /// Types which should be read before reading this types.
    /// </summary>
    private Type[] readPrerequisite;

    /// <summary>
    /// Types which should be written before writing this types.
    /// </summary>
    private Type[] writePrerequisite;

    public GSAObject(string gsaKeyword, string[] subGsaKeywords, string stream, bool analysisLayer, bool designLayer, Type[] readPrerequisite, Type[] writePrerequisite)
    {
      this.gsaKeyword = gsaKeyword;
      this.subGsaKeywords = subGsaKeywords;
      this.stream = stream;
      this.analysisLayer = analysisLayer;
      this.designLayer = designLayer;
      this.readPrerequisite = readPrerequisite;
      this.writePrerequisite = writePrerequisite;
    }

    public virtual string GSAKeyword
    {
      get { return gsaKeyword; }
    }

    public virtual string[] SubGSAKeywords
    {
      get { return subGsaKeywords; }
    }

    public virtual string Stream
    {
      get { return stream; }
    }

    public virtual bool AnalysisLayer
    {
      get { return analysisLayer; }
    }

    public virtual bool DesignLayer
    {
      get { return designLayer; }
    }

    public virtual Type[] ReadPrerequisite
    {
      get { return readPrerequisite; }
    }

    public virtual Type[] WritePrerequisite
    {
      get { return writePrerequisite; }
    }
  }

  public interface IGSASpeckleContainer
  {
    /// <summary>
    /// Record index of GSA record
    /// </summary>
    int GSAId { get; set; }

    /// <summary>
    /// Associated GWA command.
    /// </summary>
    string GWACommand { get; set; }

    /// <summary>
    /// List of GWA records used to read the object.
    /// </summary>
    List<string> SubGWACommand { get; set; }

    /// <summary>
    /// SpeckleObject created
    /// </summary>
    dynamic Value { get; set; }
  }
}
