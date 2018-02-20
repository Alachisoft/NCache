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
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.MapReduce;

namespace Alachisoft.NCache.MapReduce
{
    public class OutputMap : IOutputMap
    {
        private Dictionary<object, List<object>> outputMap = null;
        
        public OutputMap()
        {
            outputMap = new Dictionary<object,List<object>>();
        }

        public void Emit(object key, object value)
        {
            if (this.MapperOutput.ContainsKey(key))
            {
                this.MapperOutput[key].Add(value);
            }
            else
            {
                List<object> list = new List<object>();
                list.Add(value);
                this.MapperOutput.Add(key, list);
            }
        }

        public Dictionary<object, List<object>> MapperOutput
        {
            get { return outputMap; }
        }
    }
}
