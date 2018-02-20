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
using Alachisoft.NCache.Runtime.Aggregation;
using System.Collections;

namespace Alachisoft.NCache.Web.Aggregation
{
    /// <summary>
    ///  Interface that implements Combiner for Aggregator.
    /// </summary>
    [Serializable]
    public class AggregatorCombiner : ICombiner
    {
        private readonly IAggregator _aggregator;
        private IList _combinerList;
        private readonly Type _classType;
        private object mutex = new object();

        /// <summary>
        /// Constructor to initialize instance of class.
        /// </summary>
        /// <param name="aggregator">instance of IAggregator</param>
        /// <param name="classType">Class data type</param>
        public AggregatorCombiner(IAggregator aggregator, Type classType)
        {
            _aggregator = aggregator;
            _classType = classType;
            _combinerList = new ArrayList();
        }

        /// <summary>
        /// Reduces the task results locally so Reducer is not burdened with excessive processing.
        /// </summary>
        /// <param name="value"> Value for making grouped data for reducer. </param>
        public void Combine(object value)
        {
            if (value != null)
            {
                lock (mutex)
                {
                    _combinerList.Add(value);
                }
            }
        }

        /// <summary>
        /// When some specified chunk size is reached, combiners marks the functionality end on that chunk and send it to Reducer for further processing. 
        /// And resets its internal state for next chunk.
        /// </summary>
        /// <returns>Sends the chunk to Reducer.</returns>
        public object FinishChunk()
        {
            lock (mutex)
            {
                return _aggregator.Aggregate(_combinerList);
            }
        }

        /// <summary>
        /// Any Initialization for the parameters before actual combining begins.
        /// </summary>
        public void BeginCombine()
        {
        }

        public void Dispose()
        {
        }
    }
}