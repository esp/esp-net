using System;
using System.Collections.Generic;
using System.Reflection;

namespace Esp.Net.Utils
{
    public static class ReflectionExt
    {
        public static T GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (T) GetCustomAttribute(element, typeof (T), inherit);
        }

        public static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttribute(element, attributeType, inherit);
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (IEnumerable<T>) GetCustomAttributes(element, typeof (T), inherit);
        }
        
        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType, bool inherit)
        {
            return (IEnumerable<Attribute>)Attribute.GetCustomAttributes(element, attributeType, inherit);
        }
    }
}
