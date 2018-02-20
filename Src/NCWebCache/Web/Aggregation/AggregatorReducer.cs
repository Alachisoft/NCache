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
    /// Apply aggregation and compilation on final result. 
    /// </summary>
    [Serializable]
    public class AggregatorReducer : IReducer
    {
        private readonly IAggregator _aggregator;
        private readonly IList _reducerList;
        private readonly object _aggkey;
        private readonly Type _classType;

        /// <summary>
        /// Constructor to initialize instance of class. 
        /// </summary>
        /// <param name="key">key value</param>
        /// <param name="aggregator">IAggregator instance</param>
        /// <param name="classType">Class data type.</param>
        public AggregatorReducer(object key, IAggregator aggregator, Type classType)
        {
            _aggregator = aggregator;
            _reducerList = new ArrayList();
            _aggkey = key;
            _classType = classType;
        }

        /// <summary>
        /// Reduces the key-value pair to further meaning full pairs.
        /// </summary>
        /// <param name="value">Value for the specified key.</param>
        public void Reduce(object value)
        {
            if (value != null)
            {
                _reducerList.Add(value);
            }
        }

        /// <summary>
        /// Provides final result of map reduce task. 
        /// </summary>
        /// <returns>Return key-value pair.</returns>
        public Alachisoft.NCache.Runtime.MapReduce.KeyValuePair FinishReduce()
        {
            Alachisoft.NCache.Runtime.MapReduce.KeyValuePair context = new KeyValuePair();
            context.Key = _aggkey;
            context.Value = _aggregator.AggregateAll(_reducerList);
            return context;
        }

        /// <summary>
        /// Statritng point for initialization of reducer.
        /// </summary>
        public void BeginReduce()
        {
        }

        public void Dispose()
        {
        }
    }
}