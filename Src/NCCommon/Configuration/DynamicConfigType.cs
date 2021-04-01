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
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Configuration
{
    class DynamicConfigType
    {
        Type _type;
        bool _isArray;

        public DynamicConfigType() { }
        public DynamicConfigType(Type type)
        { 
            _type = type;
        }

        public DynamicConfigType(Type type, bool isArray)
            : this(type)
        {
            _isArray = isArray;
        }

        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public bool IsArray
        {
            get { return _isArray; }
            set { _isArray = value; }
        }
    }
}
