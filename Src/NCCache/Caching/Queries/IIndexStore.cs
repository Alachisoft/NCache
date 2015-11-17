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
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Queries
{
    /// <summary>
    /// Actual storage of the index
    /// </summary>
    public interface IIndexStore:ISizableIndex
    {
        object Add(object key, object value);
        bool Remove(object value, object indexPosition);
        void Clear();
        IDictionaryEnumerator GetEnumerator();
        int Count { get; }
        ArrayList GetData(object key, ComparisonType comparisonType);
        System.String StoreDataType { get; }
        string Name { get; }

    }
}
