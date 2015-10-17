#region copyright
// Copyright 2015 Keith Woods
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
using System.Linq;
using System.Reflection;

namespace Esp.Net.Utils
{
    internal class ReflectionHelper
    {
        public static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, null, null);
        }

        public static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments, BindingFlags bindingFlags)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, null, bindingFlags);
        }

        public static MethodInfo GetGenericMethodByArgumentCount(Type declaringType, string methodName, int numberOfTypeArguments, int numberOfArguments, BindingFlags bindingFlags, Func<ParameterInfo[], bool> paramPredicate)
        {
            return GetGenericMethodByArgumentCountInternal(declaringType, methodName, numberOfTypeArguments, numberOfArguments, paramPredicate, bindingFlags);
        }

        public static MethodInfo GetGenericMethodByArgumentCountInternal(
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
                where
                    m.Name == methodName &&
                    m.GetGenericArguments().Length == numberOfTypeArguments &&
                    m.GetParameters().Length == numberOfArguments
                    && predicate(m.GetParameters())
                select m;
            return query.Single();
        }
    }
}