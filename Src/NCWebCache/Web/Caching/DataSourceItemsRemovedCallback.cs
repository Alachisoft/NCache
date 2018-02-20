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

using System.Collections;

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Defines a callback method for notifying application when an item or items are removed
    /// from data source.
    /// </summary>
    /// <param name="result">Dictionary of key or keys along with there result. The result can be
    /// <see cref="DataSourceOpResult.Success"/> or in case of failure, an exception that is thrown
    /// during operation</param>
    /// <remarks>When doing a write behind remove operation on data source, this callback can be used to determine
    /// the result of the operation.</remarks>
    public delegate void DataSourceItemsRemovedCallback(IDictionary result);
}