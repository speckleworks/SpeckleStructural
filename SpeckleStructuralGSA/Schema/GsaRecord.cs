using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
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

    protected static readonly string SID_APPID_TAG = "speckle_app_id";
    protected static readonly string SID_STRID_TAG = "speckle_stream_id";
    protected static readonly string delim = "\t";

    public abstract bool FromGwa(string gwa);

    public abstract bool Gwa(out List<string> gwa, bool includeSet = false);

    public GsaRecord()
    {
      keyword = GetType().GetAttribute<GsaType>("Keyword").ToString();
      Enum.TryParse(GetType().GetAttribute<GsaType>("SetCommandType").ToString(), out gwaSetCommandType);
    }

    protected bool InitialiseGwa(bool includeSet, out List<string> items)
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
      items.Add(keyword + "." + Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));
      if (gwaSetCommandType == GwaSetCommandType.Set)
      {
        items.Add(Index.ToString());
      }
      return true;
    }

    //Designed to be called after ProcessGwa - and can handle the SET or SET_AT being included
    protected bool BasicFromGwa(string gwa, out List<string> remainingItems)
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
      var keyword = kwSplit[0];
      if (!keyword.Equals(keyword, StringComparison.InvariantCultureIgnoreCase))
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
        return gwa.ListSplit(delim).ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    protected bool Join(List<string> items, out string joined)
    {
      joined = string.Join(delim, items);
      return (joined.Length > 0);
    }

    protected string List(List<int> indices)
    {
      return string.Join(" ", indices);
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

    public static string Keyword<T>()
    {
      return typeof(T).GetAttribute<GsaType>("Keyword").ToString();
    }
    #endregion

  }
}
