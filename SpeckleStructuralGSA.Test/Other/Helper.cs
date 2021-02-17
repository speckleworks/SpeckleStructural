using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SpeckleGSAInterfaces;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  public static class Helper
  {
    public static string RemoveKeywordVersionFromApplicationIds(string gwa)
    {
      var matches = Regex.Matches(gwa, @"(gsa/[A-Z_]+)\.[0-9]{1,2}");
      if (matches.Count > 0)
      {
        var matched = matches.Cast<Match>().Select(m => m.Value).Distinct().ToList();
        foreach (var m in matched)
        {
          gwa = gwa.Replace(m, m.Split('.').First());
        }
      }
      return gwa;
    }

    public static string RemoveVersionFromKeyword(string keyword)
    {
      return keyword.Split('.').First();
    }

    //made public so that the sender tests can use it to know which keywords to use to hydrate the cache
    public static Dictionary<Type, List<Type>> GetTypeCastPriority(ioDirection ioDirection, GSATargetLayer layer, bool resultsOnly)
    {
      // Grab GSA interface and attribute type
      var attributeType = typeof(GSAObject);
      var interfaceType = typeof(IGSASpeckleContainer);

      var ioAttribute = (ioDirection == ioDirection.Receive) ? "WritePrerequisite" : "ReadPrerequisite";

      // Grab all GSA related object
      var ass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "SpeckleStructuralGSA");
      var objTypes = ass.GetTypes().Where(t => interfaceType.IsAssignableFrom(t) && t != interfaceType && !t.IsAbstract).ToList();

      var TypePrerequisites = new Dictionary<Type, List<Type>>();

      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType)) continue;

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType)) continue;

        if (ioDirection == ioDirection.Send)
        {
          if (t.GetAttribute("Stream", attributeType) != null)
            if (resultsOnly && t.GetAttribute("Stream", attributeType) as string != "results") continue;
        }

        var prereq = new List<Type>();
        if (t.GetAttribute(ioAttribute, attributeType) != null)
          prereq = ((Type[])t.GetAttribute(ioAttribute, attributeType)).ToList();

        TypePrerequisites[t] = prereq;
      }

      // Remove wrong layer objects from prerequisites
      foreach (var t in objTypes)
      {
        if (t.GetAttribute("AnalysisLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Analysis) && !(bool)t.GetAttribute("AnalysisLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (t.GetAttribute("DesignLayer", attributeType) != null)
          if ((layer == GSATargetLayer.Design) && !(bool)t.GetAttribute("DesignLayer", attributeType))
            foreach (var kvp in TypePrerequisites)
              kvp.Value.Remove(t);

        if (ioDirection == ioDirection.Send)
        {
          if (t.GetAttribute("Stream", attributeType) != null)
            if (resultsOnly && t.GetAttribute("Stream", attributeType) as string != "results")
              foreach (var kvp in TypePrerequisites)
                kvp.Value.Remove(t);
        }
      }

      return TypePrerequisites;
      // Generate which GSA object to cast for each type
      //TypeCastPriority = TypePrerequisites.ToList();
      //TypeCastPriority.Sort((x, y) => x.Value.Count().CompareTo(y.Value.Count()));
    }

    public static T DeserialiseJson<T>(string json)
    {
      var response = default(T);
      try
      {
        response = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings()
        {
          ContractResolver = new DefaultContractResolver() { NamingStrategy = new CamelCaseNamingStrategy() },
          ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
          NullValueHandling = NullValueHandling.Ignore
        });
      }
      catch (Exception e)
      {
        return default(T);
      }
      return response;
    }

    public static string ReadFile(string testDataFileName, string directory)
    {
      byte[] buffer;
      //Directory should have a trailing slash

      var fileStream = new FileStream(Path.Combine(directory, testDataFileName), FileMode.Open, FileAccess.Read);
      try
      {
        var length = (int)fileStream.Length;  // get file length
        buffer = new byte[length];            // create buffer
        int count;                            // actual number of bytes read
        var sum = 0;                          // total number of bytes read

        // read until Read method returns 0 (end of the stream has been reached)
        while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
        {
          sum += count;  // sum is a buffer offset for next reading
        }
      }
      finally
      {
        fileStream.Close();
      }
      return Encoding.UTF8.GetString(buffer);
    }

    public static void WriteFile(string data, string fileName, string directory)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      writer.Write(data);
      writer.Flush();

      stream.Position = 0;
      stream.Seek(0, SeekOrigin.Begin);

      var fileStream = File.Create(Path.Combine(directory, fileName));
      stream.CopyTo(fileStream);
      fileStream.Close();
    }

    public static object GetAttribute(this object t, string attribute, Type attributeType)
    {
      try
      {
        if (t is Type)
        {
          var attObj = Attribute.GetCustomAttribute((Type)t, attributeType);
          return attributeType.GetProperty(attribute).GetValue(attObj);
        }
        else
        {
          var attObj = Attribute.GetCustomAttribute(t.GetType(), attributeType);
          return attributeType.GetProperty(attribute).GetValue(attObj);
        }
      }
      catch { return null; }
    }

    public static bool IsReferencing(this Assembly assembly, AssemblyName referenceName)
    {
      if (AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), referenceName))
      {
        return true;
      }

      foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies())
      {
        if (AssemblyName.ReferenceMatchesDefinition(referencedAssemblyName, referenceName))
        {
          return true;
        }
      }

      return false;
    }

    public static bool GwaToCache(IEnumerable<string> gwaCommands, string streamId)
    {
      foreach (var c in gwaCommands)
      {
        if (!GwaToCache(c, streamId))
        {
          return false;
        }
      }
      return true;
    }

    //Copied and modified from Receiver in SpeckleGSA - the Speckle object isn't copied to the cache here because that's only used for merging
    public static bool GwaToCache(string gwaCommand, string streamId)
    {
      var lines = gwaCommand.Split(new[] { '\n' }).Where(l => !string.IsNullOrEmpty(l)).ToList();
      foreach (var l in lines)
      {
        //At this point the SID will be filled with the application ID
        Initialiser.AppResources.Proxy.ParseGeneralGwa(l, out var keyword, out var foundIndex,
          out var foundStreamId, out var foundApplicationId, out var gwaWithoutSet, out var gwaSetCommandType);

        var originalSid = Initialiser.AppResources.Proxy.FormatSidTags(foundStreamId, foundApplicationId);
        var newSid = Initialiser.AppResources.Proxy.FormatSidTags(streamId, foundApplicationId);

        //If the SID tag has been set then update it with the stream
        gwaWithoutSet = (string.IsNullOrEmpty(originalSid))
            ? gwaWithoutSet.Replace(keyword, keyword + ":" + newSid)
            : gwaWithoutSet.Replace(originalSid, newSid);

        if (!Initialiser.AppResources.Cache.Upsert(keyword, foundIndex.Value, gwaWithoutSet, streamId, foundApplicationId, gwaSetCommandType.Value))
        {
          return false;
        }
      }
      return true;
    }
  }
}
