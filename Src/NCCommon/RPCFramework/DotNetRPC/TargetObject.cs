//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System.Collections.Generic;
using System.Reflection;

namespace Alachisoft.NCache.Common.RPCFramework.DotNetRPC
{
    public class TargetObject<Target> : IRPCTargetObject<Target>
    {
        private Target _target;

        public TargetObject(Target targetObject)
        {
            _target = targetObject;
        }

        public Target GetTargetObject()
        {
            return _target;
        }

        public ITargetMethod GetMethod(string methodName, int overload)
        {
            ITargetMethod targetMethod = null;
            if (_target != null)
            {
                MethodInfo[] members = _target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);

                for (int i = 0; i < members.Length; i++)
                {
                    MethodInfo member = members[i];

                    object[] customAttributes = member.GetCustomAttributes(typeof(TargetMethodAttribute), true);

                    if (customAttributes != null && customAttributes.Length > 0)
                    {
                        TargetMethodAttribute targetMethodAttribute = customAttributes[0] as TargetMethodAttribute;
                       
                        if (targetMethodAttribute.Method.Equals(methodName) && targetMethodAttribute.Overload == overload)
                        {
                           targetMethod = new TargetMethod(_target, member, targetMethodAttribute.Method, targetMethodAttribute.Overload);
                        }
                    }

                }
            }
            return targetMethod;
        }

        public ITargetMethod[] GetAllMethods()
        {
            List<ITargetMethod> methods = new List<ITargetMethod>();
            if (_target != null)
            {
                MethodInfo[] members = _target.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public);

                for(int i =0; i< members.Length; i++)
                {
                    MethodInfo member = members[i];

                    object[] customAttributes = member.GetCustomAttributes(typeof(TargetMethodAttribute),true);

                    if (customAttributes != null && customAttributes.Length >0)
                    {
                        TargetMethodAttribute targetMethodAttribute = customAttributes[0] as TargetMethodAttribute;
                        TargetMethod targetMethod = new TargetMethod(_target, member, targetMethodAttribute.Method, targetMethodAttribute.Overload);
                        methods.Add(targetMethod);
                    }

                }
            }

            return methods.ToArray();
        }
    }
}
