using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SpeckleCore;

namespace SpeckleUtil
{
  public static class Helper
  {
    public static List<Type> GetLoadedSpeckleTypes()
    {
      //Find all structural types which have a ToNative static method
      var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetTypes().Any(t => typeof(ISpeckleInitializer).IsAssignableFrom(t))).ToList();
      var speckleTypes = new List<Type>();
      foreach (var assembly in assemblies)
      {
        var types = assembly.GetTypes();
        foreach (var t in types)
        {
          if (typeof(SpeckleObject).IsAssignableFrom(t))
          {
            speckleTypes.Add(t);
          }
        }
      }
      return speckleTypes;
    }

    //Originally retrieved from https://stackoverflow.com/questions/299515/reflection-to-identify-extension-methods.  This also exists as a private
    //method in SpeckleCore.
    //Re-arranged here from query to multi-line code format to facilitate stepping through lines of code during debugging.
    public static IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType, string methodName)
    {
      var methodNameFormatted = methodName.ToLower();
      var returnMethodInfos = new List<MethodInfo>();
      var types = assembly.GetTypes();
      var typesFiltered = types.Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested);
      foreach (var type in typesFiltered)
      {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var methodsFiltered = methods.Where(m => m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false));

        foreach (var method in methodsFiltered)
        {
          var methodParameters = method.GetParameters();
          if (methodParameters != null && methodParameters.Count() > 0)
            if (methodParameters[0].ParameterType == extendedType)
            {
              if (string.Equals(method.Name, methodName, StringComparison.InvariantCultureIgnoreCase))
              {
                returnMethodInfos.Add(method);
              }
            }
        }
      }

      return returnMethodInfos;
    }
  }
}
