#region copyright
// Copyright 2015 Dev Shop Limited
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Esp.Net.Utils
{
    internal static class ReflectionExt
    {
        internal static T GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (T) GetCustomAttribute(element, typeof (T), inherit);
        }

        internal static Attribute GetCustomAttribute(this MemberInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttribute(element, attributeType, inherit);
        }

        internal static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (IEnumerable<T>) GetCustomAttributes(element, typeof (T), inherit);
        }

        internal static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType, bool inherit)
        {
            return (IEnumerable<Attribute>)Attribute.GetCustomAttributes(element, attributeType, inherit);
        }

        internal static IEnumerable<MethodInfo> GetMethodsRecursive(this Type type, BindingFlags bindingAttr)
        {
            if(type == null) yield break;
            var children = type.GetMethods(bindingAttr);
            foreach (MethodInfo methodInfo in children)
            {
                yield return methodInfo;
            }
            var grandChildren = type.BaseType.GetMethodsRecursive(bindingAttr);
            foreach (MethodInfo methodInfo in grandChildren)
            {
                yield return methodInfo;
            }
        }
    }
}
