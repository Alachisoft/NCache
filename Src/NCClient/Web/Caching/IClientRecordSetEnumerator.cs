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
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Client
{
    internal interface IClientRecordSetEnumerator : IDisposable
    {
        /// <summary>
        /// Gets the current KeyValue Pair in the RecordSet.
        /// </summary>
        ClusteredArray<object> Current { get; }
        /// <summary>
        /// Advances the enumerator to the next <see cref="Alachisoft.NCache.Common.DataStructures.RecordRow"/> in the RecordSet.
        /// </summary>
        /// <returns> true if the enumerator was successfully advanced to the next RecordRow; false 
        /// if the enumerator has passed the end of the RecordSet.</returns>
        bool MoveNext();

        int FieldCount { get; }
    }
}
