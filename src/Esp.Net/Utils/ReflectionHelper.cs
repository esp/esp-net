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
    internal class ReflectionHelper
    {
        internal static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, null, null);
        }

        internal static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments, BindingFlags bindingFlags)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, null, bindingFlags);
        }

        internal static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments, BindingFlags bindingFlags, Func<ParameterInfo[], bool> paramPredicate)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, paramPredicate, bindingFlags);
        }

        internal static MethodInfo GetGenericMethodByArgumentCountInternal(
            Type declaringType, 
            string methodName,
            int numberOfTypeArguments, 
            int numberOfArguments, 
            Func<ParameterInfo[], bool> predicate,
            BindingFlags? bindingFlags)
        {
            predicate = predicate ?? (p => true);
            MethodInfo[] methodInfos = bindingFlags.HasValue 
                ? declaringType.GetMethods(bindingFlags.Value) 
                : declaringType.GetMethods();
            var query =
                from m in methodInfos
                let match = 
                    m.Name == methodName &&
                    m.GetGenericArguments().Length == numberOfTypeArguments &&
                    m.GetParameters().Length == numberOfArguments
                    && predicate(m.GetParameters())
                where match
                select m;
            return query.Single();
        }

        internal static bool SharesBaseType(Type commonBaseType, params Type[] types)
        {
            return SharesBaseType(commonBaseType, types.ToList());
        }

        internal static bool SharesBaseType(Type commonBaseType, IEnumerable<Type> types)
        {
            Guard.Requires<ArgumentNullException>(commonBaseType != null, "commonBaseType can not be null");
            bool? allTypesShareCommonBase = null;
            foreach (Type t in types)
            {
                if (!allTypesShareCommonBase.HasValue) allTypesShareCommonBase = true; // default to true if we have any types
                bool shares = false;
                var type = t;
                while (type != null)
                {
                    if (type == typeof(object)) break;
                    if (commonBaseType == type)
                    {
                        shares = true;
                        break;
                    }
                    type = type.BaseType;
                }
                if (!shares)
                {
                    allTypesShareCommonBase = false;
                    break;
                }
            }
            return allTypesShareCommonBase ?? false;
        }

        internal static bool TryGetCommonBaseType(out Type baseType, params Type[] types)
        {
            return TryGetCommonBaseType(out baseType, types.ToList());
        }

        internal static bool TryGetCommonBaseType(out Type baseType, IEnumerable<Type> types)
        {
            var inheritanceChains = new List<List<Type>>();
            foreach (Type t in types)
            {
                List<Type> chain = new List<Type>();
                var type = t;
                while (type != null)
                {
                    if(type == typeof(object)) break;
                    chain.Add(type);
                    type = type.BaseType;
                }
                chain.Reverse();
                inheritanceChains.Add(chain);
            }
            var j = 0;
            baseType = null;
            while (true)
            {
                Type commonBaseType = baseType ?? inheritanceChains[0][0];
                bool shareSameBaseType = inheritanceChains.All(chain =>
                {
                    return chain.Count >= j -1
                        ? chain[j] == commonBaseType
                        : false;
                });
                if (shareSameBaseType)
                    baseType = inheritanceChains[0][j];
                else
                    break;
                j++;
            }
            return baseType != null;
        }
    }
}