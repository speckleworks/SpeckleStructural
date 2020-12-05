using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Schema
{
  public abstract class GsaRecord
  {
    public int? Index;
    public int Version;
    public string Sid;
    public string ApplicationId;
    public string StreamId;

    //Not all objects have names, so it's up to the concrete classes to include a property which uses this field.
    //It's included here so the AddName method can be included in this abstract class, saving its repetition in concrete classes
    protected string name;

    protected GwaSetCommandType gwaSetCommandType;
    protected string keyword;  //Useful in string rather than enum form for use in creating GWA commands

    public string Keyword => this.keyword;
    public GwaSetCommandType GwaSetCommandType => this.gwaSetCommandType;

    protected static readonly string SID_APPID_TAG = "speckle_app_id";
    protected static readonly string SID_STRID_TAG = "speckle_stream_id";

    public abstract bool FromGwa(string gwa);

    public abstract bool Gwa(out List<string> gwa, bool includeSet = false);

    public GsaRecord()
    {
      keyword = GetType().GetAttribute<GsaType>("Keyword").ToString();
      Enum.TryParse(GetType().GetAttribute<GsaType>("SetCommandType").ToString(), out gwaSetCommandType);
    }

    //The keywordOverride is mainly used for the LOAD_BEAM case
    protected bool InitialiseGwa(bool includeSet, out List<string> items, string keywordOverride = "")
    {
      items = new List<string>();
      if (includeSet)
      {
        if (gwaSetCommandType == GwaSetCommandType.SetAt)
        {
          items.AddRange(new[] { "SET_AT", Index.ToString() });
        }
        else
        {
          items.Add("SET");
        }
      }
      var sid = FormatSidTags(StreamId, ApplicationId);
      items.Add((string.IsNullOrEmpty(keywordOverride) ? keyword : keywordOverride) + "." + Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));
      if (gwaSetCommandType == GwaSetCommandType.Set)
      {
        items.Add(Index.ToString());
      }
      return true;
    }

    //Designed to be called after ProcessGwa - and can handle the SET or SET_AT being included
    protected bool BasicFromGwa(string gwa, out List<string> remainingItems, string keywordOverride = "")
    {
      var items = Split(gwa);
      remainingItems = new List<string>();
      if (items.Count() == 0)
      {
        return false;
      }

      //Process and remove just the initial SET or SET_AT <index> items      
      if (items[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        if (items[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
        {
          gwaSetCommandType = GwaSetCommandType.SetAt;

          if (int.TryParse(items[1], out var foundIndex))
          {
            Index = foundIndex;
          }

          //For SET_ATs the format is SET_AT <index> <keyword> .., so remove the first two
          items.Remove(items[1]);
          items.Remove(items[0]);
        }
        else
        {
          gwaSetCommandType = GwaSetCommandType.Set;

          items.Remove(items[0]);
        }
      }

      string keywordAndVersion;
      var delimIndex = items[0].IndexOf(':');
      if (delimIndex > 0)
      {
        //An SID has been found
        keywordAndVersion = items[0].Substring(0, delimIndex);
        var sidTags = items[0].Substring(delimIndex);
        var match = Regex.Match(sidTags, "(?<={" + SID_STRID_TAG + ":).*?(?=})");
        StreamId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : null;
        match = Regex.Match(sidTags, "(?<={" + SID_APPID_TAG + ":).*?(?=})");
        ApplicationId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : null;
      }
      else
      {
        keywordAndVersion = items[0];
      }

      var kwSplit = keywordAndVersion.Split(new[] { '.' });
      var foundKeyword = kwSplit[0];
      if (!foundKeyword.Equals(string.IsNullOrEmpty(keywordOverride) ? keyword : keywordOverride, StringComparison.InvariantCultureIgnoreCase))
      {
        return false;
      }
      if (kwSplit.Count() > 1)
      {
        if (!int.TryParse(kwSplit[1], out Version))
        {
          return false;
        }
      }
      else
      {
        Version = 1;
      }

      //Remove keyword
      items.Remove(items[0]);

      if (gwaSetCommandType == GwaSetCommandType.Set)
      {
        if (!int.TryParse(items[0], out var index) || index == 0)
        {
          return false;
        }
        Index = index;
        items.Remove(items[0]);
      }

      remainingItems = items;

      return true;
    }

    protected bool AddName(string v)
    {
      name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    protected List<string> Split(string gwa)
    {
      try
      {
        return gwa.ListSplit(Initialiser.Interface.GwaDelimiter).ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    protected bool AddYesNoBoolean(string v, out bool dest)
    {
      dest = (v.Equals("YES", StringComparison.InvariantCultureIgnoreCase)) ? true : false;
      return true;
    }

    protected bool AddNullableIndex(string v, out int? dest)
    {
      dest = int.TryParse(v, out var n) && n > 0 ? (int?)n : null;
      return true;
    }

    //For when you need to read a value from GWA that is stored in a nullable double member
    protected bool AddNullableDoubleValue(string v, out double? dest)
    {
      dest = double.TryParse(v, out var n) ? (double?)n : null;
      return true;
    }

    //For when you need to read a value from GWA that is stored in a nullable integer member
    protected bool AddNullableIntValue(string v, out int? dest)
    {
      dest = int.TryParse(v, out var n) ? (int?)n : null;
      return true;
    }

    protected bool Join(List<string> items, out string joined)
    {
      joined = string.Join(Initialiser.Interface.GwaDelimiter.ToString(), items);
      return (joined.Length > 0);
    }

    protected string List(List<int> indices)
    {
      return string.Join(" ", indices);
    }

    protected List<int> StringToIntList(string s, char delim = ' ')
    {
      var retList = new List<int>();
      foreach (var i in s.Split(delim).Where(i => i.IsDigits()))
      {
        if (int.TryParse(i, out var result))
        {
          retList.Add(result);
        }
      }
      return retList;
    }

    protected List<double> StringToDoubleList(string s, char delim = ' ')
    {
      var retList = new List<double>();
      foreach (var i in s.Split(delim).Where(i => Regex.IsMatch(i, @"\d+\.?\d*")))
      {
        if (double.TryParse(i, out var result))
        {
          retList.Add(result);
        }
      }
      return retList;
    }

    protected bool EnumParse<T>(string s, out T v)
    {
      try
      {
        v = (T)Enum.Parse(typeof(T), s);
        return true;
      }
      catch
      {
        v = default;
        return false;
      }
    }

    protected bool AddItems(ref List<string> items, params object[] list)
    {
      try
      {
        foreach (var l in list)
        {
          if (l == null)
          {
            items.Add("");
          }
          else if (l is Func<string>)
          {
            items.Add(((Func<string>)l)());
          }
          else
          {
            items.Add(l.ToString());
          }
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    protected bool FromGwaByFuncs(List<string> items, out List<string> remainingItems, params Func<string, bool>[] fns)
    {
      if (fns.Count() > items.Count())
      {
        remainingItems = new List<string>();
        return false;
      }
      for (var i = 0; i < fns.Count(); i++)
      {
        if (fns[i] != null && !fns[i](items[i]))
        {
          remainingItems = (i == 0) ? new List<string>() : items.Skip(i).ToList();
          return false;
        }
      }
      remainingItems = items.Skip(fns.Count()).ToList();
      return true;
    }

    #region static_methods
    private static string FormatApplicationIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? null : "{" + SID_APPID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    private static string FormatStreamIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? null : "{" + SID_STRID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    private static string FormatSidTags(string streamId = null, string applicationId = null)
    {
      var streamIdSidTag = FormatStreamIdSidTag(streamId);
      var appIdSidTag = FormatApplicationIdSidTag(applicationId);
      var sidTags = "";
      if (!string.IsNullOrEmpty(streamIdSidTag))
      {
        sidTags += streamIdSidTag;
      }
      if (!string.IsNullOrEmpty(appIdSidTag))
      {
        sidTags += appIdSidTag;
      }
      return string.IsNullOrEmpty(sidTags) ? null : sidTags;
    }

    public static string GetKeyword<T>()
    {
      return typeof(T).GetAttribute<GsaType>("Keyword").ToString();
    }

    public static GwaSetCommandType GetGwaSetCommandType<T>()
    {
      return (GwaSetCommandType)typeof(T).GetAttribute<GsaType>("SetCommandType");
    }
    #endregion

  }
}
