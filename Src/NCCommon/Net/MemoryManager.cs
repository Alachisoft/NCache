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
using System.Collections;

namespace Alachisoft.NCache.Common
{
    public class MemoryManager
    {
        private Hashtable _objectProviders = new Hashtable();

        public void RegisterObjectProvider(ObjectProvider proivder)
        {
            if (!_objectProviders.Contains(proivder.ObjectType))
                _objectProviders.Add(proivder.ObjectType, proivder);
        }

        public ObjectProvider GetProvider(Type objType)
        {
            if (_objectProviders.Contains(objType))
                return (ObjectProvider)_objectProviders[objType];
            return null;
        }
      
    }
}