using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SpeckleCore;

namespace SpeckleStructuralGSA.Test
{
  public static class Helper
  {
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
     
      var fileStream = new FileStream(directory + testDataFileName, FileMode.Open, FileAccess.Read);
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

    public static string ResolveFullPath(string fileName, string directory)
    {
      //Directory should have a trailing slash

      return (directory + fileName);
    }

    public static void WriteFile(string data, string fileName, string directory)
    {
      var stream = new MemoryStream();
      var writer = new StreamWriter(stream);
      writer.Write(data);
      writer.Flush();

      stream.Position = 0;
      stream.Seek(0, SeekOrigin.Begin);

      var fileStream = File.Create(ResolveFullPath(fileName, directory));
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
  }
}
