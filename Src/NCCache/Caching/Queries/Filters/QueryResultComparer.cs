// Copyright (c) 2015 Alachisoft
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
using System.Collections;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    internal class QueryResultComparer : IComparer
    {
        bool _ascending = false;

        public QueryResultComparer(bool ascending)
        {
            _ascending = ascending;
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            int result = 0;
            int a = (int)x;
            int b = (int)y;

            if (_ascending)
                result = a > b ? 1 : -1;
            else
                result = a < b ? 1 : -1;

            return result;
        }

        #endregion
    }
}
