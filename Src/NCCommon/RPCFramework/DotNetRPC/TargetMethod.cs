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
using System;
using System.Reflection;

namespace Alachisoft.NCache.Common.RPCFramework.DotNetRPC
{
    public class TargetMethod :ITargetMethod
    {
        MethodInfo _method;
        string _specifiedMethodName;
        int _overlaod;
        object _target;

        public TargetMethod(object targetObject, MethodInfo methodInfo,string methodName,int overload)
        {
            _target = targetObject;
            _method = methodInfo;
            _specifiedMethodName = methodName;
            _overlaod = overload;
        }

        public string GetMethodName()
        {
            return _specifiedMethodName;
        }

        public int GetOverlaod()
        {
            return _overlaod;
        }

        public object GetMethodReflectionInfo()
        {
            return _method;
        }

        public int GetNumberOfArguments()
        {
            return _method.GetParameters().Length;
        }

        public object Invoke(object[] arguments)
        {
            try
            {
                return _method.Invoke(_target, arguments);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                {
                    Exception ex = e.InnerException;
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    else
                        throw ex;
                } 
            }
            return null;
        }
    }
}
