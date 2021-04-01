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

namespace Alachisoft.NCache.Common.RPCFramework
{
    public class RPCMethodCache<TargetObject>
    {
        IRPCTargetObject<TargetObject> _targetObject;
        IDictionary<string, ITargetMethod> _cache = new Dictionary<string, ITargetMethod>();

        public RPCMethodCache(IRPCTargetObject<TargetObject> targetObject)
        {
            _targetObject = targetObject;
            PopulateCache();
        }

        private void PopulateCache()
        {
            if (_targetObject != null)
            {
                ITargetMethod[] methods = _targetObject.GetAllMethods();

                for (int i = 0; i < methods.Length; i++)
                {
                    ITargetMethod method = methods[i];
                    string key = GetCacheKey(method.GetMethodName(),method.GetOverlaod());
                    if (!_cache.ContainsKey(key))
                        _cache.Add(key, method);
                
                }
            }
        }

        private static string GetCacheKey(string methodName, int overload)
        {
            return methodName + "$" + overload;
        }

        public ITargetMethod GetTargetMethod(string methodName, int overload)
        {
            string key = GetCacheKey(methodName, overload);

            if (_cache.ContainsKey(key))
                return _cache[key];
            else
                return null;
        }

        public IRPCTargetObject<TargetObject> GetTargetObject()
        {
            return _targetObject;
        }
    }
}
