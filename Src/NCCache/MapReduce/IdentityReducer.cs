// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.MapReduce
{
    class IdentityReducer : Alachisoft.NCache.Runtime.MapReduce.IReducer
    {
        private object key;
        private IList list = new ArrayList();

        public IdentityReducer(object key)
        {
            // TODO: Complete member initialization
            this.key = key;
        }
      
        public void Reduce(object value)
        {
            list.Add(value);
        }

        public Alachisoft.NCache.Runtime.MapReduce.KeyValuePair FinishReduce()
        {
            Alachisoft.NCache.Runtime.MapReduce.KeyValuePair context = new Runtime.MapReduce.KeyValuePair();
            context.Key = key;
            context.Value = list;
            return context;
        }

        public void BeginReduce()
        { }

        public void Dispose()
        { }
    }
}
