using System;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  [AttributeUsage(AttributeTargets.Class)]
  public class GsaType : Attribute
  {
    public GwaKeyword Keyword { get; protected set; }
    //Usually each GSA table only uses one keyword; for the few cases where one set of indices are shared across multiple keywords, this is where the other keywords are stored
    public GwaKeyword[] TableSharedKeywords { get; protected set; }
    public GwaSetCommandType SetCommandType { get; protected set; }
    public StreamBucket StreamBucket { get; protected set; }
    public bool AnalysisLayer { get; protected set; }
    public bool DesignLayer { get; protected set; }
    public GwaKeyword[] ReferencedKeywords { get; protected set; }

    public GsaType(GwaKeyword keyword, GwaSetCommandType setCommandType, StreamBucket streamBucket, bool analysisLayer, bool designLayer, params GwaKeyword[] referencedKeywords)
    {
      this.Keyword = keyword;
      this.TableSharedKeywords = new GwaKeyword[0];
      this.SetCommandType = setCommandType;
      this.StreamBucket = streamBucket;
      this.AnalysisLayer = analysisLayer;
      this.DesignLayer = designLayer;
      this.ReferencedKeywords = referencedKeywords;
    }

    public GsaType(GwaKeyword keyword, GwaSetCommandType setCommandType, StreamBucket streamBucket)
    {
      this.Keyword = keyword;
      this.TableSharedKeywords = new GwaKeyword[0];
      this.SetCommandType = setCommandType;
      this.StreamBucket = streamBucket;
      this.AnalysisLayer = true;
      this.DesignLayer = true;
      this.ReferencedKeywords = new GwaKeyword[0];
    }

    public GsaType(GwaKeyword keyword, GwaKeyword[] tableSharedKeywords, GwaSetCommandType setCommandType, StreamBucket streamBucket)
    {
      this.Keyword = keyword;
      this.TableSharedKeywords = tableSharedKeywords;
      this.SetCommandType = setCommandType;
      this.StreamBucket = streamBucket;
      this.AnalysisLayer = true;
      this.DesignLayer = true;
      this.ReferencedKeywords = new GwaKeyword[0];
    }
  }
}
