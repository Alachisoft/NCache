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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.MapReduce
{
    /// <summary>
    /// Interface to implement Combiner for MapReduce.
    /// </summary>
    public interface ICombiner : IDisposable
    {
        /// <summary>
        /// Any Intialization for the parameters before actual combining begins.
        /// </summary>
        /// <example>
        /// Following code demonstrates implementation of BeginCombine method. 
        /// <code>
        /// int count=0;
        /// public void BeginCombine()
        /// {
        ///    //Initialize
        /// }
        /// </code>
        /// </example>
        void BeginCombine();
        /// <summary>
        /// Combines the task results locally so Reducer is not burdened with excessive processing. 
        /// </summary>
        /// <param name="value">Value for making grouped data for reducer.</param>
        /// <example>
        /// Following example demostrates how to implement Combine.
        /// <code>
        /// public void Combine(object value)
        /// {
        ///    count += int.Parse(value.ToString());
        /// }
        /// </code>
        /// </example>
        /// 
        
        void Combine(object value);
        /// <summary>
        /// When some specified chunk size is reached, combiners marks the functionality end on that chunk and send it to Reducer for further processing. 
        /// And resets its internal state for next chunk.
        /// </summary>
        /// <example>
        /// Following example demostrates how to implement FinishChunk. 
        /// <code>
        /// public object FinishChunk()
        /// {
        ///   return count;
        /// }
        /// </code>
        /// </example>
        /// <returns>Sends the chunk to Reducer.</returns>
        object FinishChunk();
    }
}
