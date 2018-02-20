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
using Alachisoft.NCache.Runtime.MapReduce;

namespace Alachisoft.NCache.MapReduce
{
    [Serializable]
    public class Filter
    {
        private IKeyFilter keyFilter = null;
        private QueryFilter queryFilter = null;
        private Runtime.MapReduce.IKeyFilter _kFilter;

        public Filter(IKeyFilter keyFilter)
        { this.keyFilter = keyFilter; }

        public Filter(QueryFilter queryFilter)
        { this.queryFilter = queryFilter; }


        public QueryFilter QueryFilter
        {
            get { return queryFilter; }
            set { queryFilter = value; }
        }
        
        public IKeyFilter KeyFilter
        {
            get { return keyFilter; }
            set { keyFilter = value; }
        }

    }
}
