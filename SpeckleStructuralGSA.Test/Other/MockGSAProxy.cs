using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SpeckleGSAInterfaces;
using SpeckleGSAProxy;

namespace SpeckleStructuralGSA.Test
{
  public static class MockGSAProxy
  {
    public delegate void ParseCallback(string fullGwa, out string keyword, out int? index, out string streamId, out string applicationId, out string gwaWithoutSet, out GwaSetCommandType? gwaSetCommandType, bool includeKwVersion = false);

    public static int nodeIndex = 0;
    //Copied over from the GSAProxy
    public static Dictionary<string, string[]> IrregularKeywordGroups = new Dictionary<string, string[]> {
      { "LOAD_BEAM", new string[] { "LOAD_BEAM_POINT", "LOAD_BEAM_UDL", "LOAD_BEAM_LINE", "LOAD_BEAM_PATCH", "LOAD_BEAM_TRILIN" } }
    };
    private static readonly string SID_APPID_TAG = "speckle_app_id";
    private static readonly string SID_STRID_TAG = "speckle_stream_id";

    public static int[] ConvertGSAList(string list, GSAEntity type)
    {
      var elements = list.Split(new[] { ' ' });

      var indices = new List<int>();
      foreach (var e in elements)
      {
        if (e.All(c => char.IsDigit(c)) && int.TryParse(e, out int index))
        {
          indices.Add(index);
        }
      }

      //It's assumed for now that any list of GSA indices that would correspond to the App IDs in the list would be a sequence from 1
      return indices.ToArray();
    }

    public static int NodeAt(double x, double y, double z, double coincidenceTol) => ++nodeIndex;

    public static string FormatApplicationIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_APPID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatStreamIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_STRID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatSidTags(string streamId = "", string applicationId = "")
    {
      return FormatStreamIdSidTag(streamId) + FormatApplicationIdSidTag(applicationId);
    }

    public static void ParseGeneralGwa(string fullGwa, out string keyword, out int? index, out string streamId, out string applicationId, out string gwaWithoutSet, out GwaSetCommandType? gwaSetCommandType, bool includeKwVersion = false)
    {
      var pieces = fullGwa.ListSplit(GSAProxy.GwaDelimiter).ToList();
      keyword = "";
      streamId = "";
      applicationId = "";
      index = null;
      gwaWithoutSet = fullGwa;
      gwaSetCommandType = null;

      if (pieces.Count() < 2)
      {
        return;
      }

      //Remove the Set for the purpose of this method
      if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        if (pieces[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
        {
          gwaSetCommandType = GwaSetCommandType.SetAt;

          if (int.TryParse(pieces[1], out int foundIndex))
          {
            index = foundIndex;
          }

          //For SET_ATs the format is SET_AT <index> <keyword> .., so remove the first two
          pieces.Remove(pieces[1]);
          pieces.Remove(pieces[0]);
        }
        else
        {
          gwaSetCommandType = GwaSetCommandType.Set;
          if (int.TryParse(pieces[2], out int foundIndex))
          {
            index = foundIndex;
          }

          pieces.Remove(pieces[0]);
        }
      }
      else
      {
        if (int.TryParse(pieces[1], out var foundIndex))
        {
          index = foundIndex;
        }
      }

      var delimIndex = pieces[0].IndexOf(':');
      if (delimIndex > 0)
      {
        //An SID has been found
        keyword = pieces[0].Substring(0, delimIndex);
        var sidTags = pieces[0].Substring(delimIndex);
        var match = Regex.Match(sidTags, "(?<={" + SID_STRID_TAG + ":).*?(?=})");
        streamId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
        match = Regex.Match(sidTags, "(?<={" + SID_APPID_TAG + ":).*?(?=})");
        applicationId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
      }
      else
      {
        keyword = pieces[0];
      }

      foreach (var groupKeyword in IrregularKeywordGroups.Keys)
      {
        if (IrregularKeywordGroups[groupKeyword].Contains(keyword))
        {
          keyword = groupKeyword;
          break;
        }
      }

      if (!includeKwVersion)
      {
        keyword = keyword.Split('.').First();
      }

      gwaWithoutSet = string.Join(GSAProxy.GwaDelimiter.ToString(), pieces);
      return;
    }
  }
}
