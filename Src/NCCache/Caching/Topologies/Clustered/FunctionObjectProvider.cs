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
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    public class FunctionObjectProvider : ObjectProvider
    {
        public FunctionObjectProvider() { }
        public FunctionObjectProvider(int initialsize) : base(initialsize) { }

        protected override IRentableObject CreateObject()
        {
            return new Function();
        }
        public override string Name
        {
            get { return "FunctionObjectProvider"; }
        }
        protected override void ResetObject(object obj)
        {
            Function f = obj as Function;
            if (f != null)
            {
                f.Operand = 0;
                f.Operand = null;
                f.ExcludeSelf = true;
            }
        }

        public override Type ObjectType
        {
            get
            {
                if (_objectType == null) _objectType = typeof(Function);
                return _objectType;
            }
        }
    }
}