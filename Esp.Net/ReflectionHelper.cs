using System;
using System.Linq;
using System.Reflection;

namespace Esp.Net
{
    public class ReflectionHelper
    {
        public static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments)
        {
            var query =
                from m in declaringType.GetMethods()
                where
                    m.Name == methodName &&
                    m.GetGenericArguments().Length == numberOfTypeArguments &&
                    m.GetParameters().Length == numberOfArguments
                select m;
            return query.Single();
        } 
    }
}