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
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    public class QueryDataFilters
    {
        private EventDataFilter _addDF;
        private EventDataFilter _updateDF;
        private EventDataFilter _removeDF;

        public QueryDataFilters(int add, int update, int remove)
        {
            _addDF = (EventDataFilter)add;
            _updateDF = (EventDataFilter)update;
            _removeDF = (EventDataFilter)remove;
        }

        public EventDataFilter AddDataFilter
        {
            get { return _addDF; }
            set { _addDF = value; }
        }

        public EventDataFilter UpdateDataFilter
        {
            get { return _updateDF; }
            set { _updateDF = value; }
        }

        public EventDataFilter RemoveDataFilter
        {
            get { return _removeDF; }
            set { _removeDF = value;}
        }
    }
}
