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

namespace Alachisoft.NCache.Web.Aggregation
{
    /// <summary>
    /// Assigns a unique Reducer for each provided key.
    /// </summary>
    [Serializable]
    public class AggregatorReducerFactory : IReducerFactory
    {
        private readonly IAggregator _aggregator;
        private readonly Type _classType;

        /// <summary>
        /// Constructor to initialize instance of class.
        /// </summary>
        /// <param name="aggregator">instance of IAggregator</param>
        /// <param name="classType">Class data type</param>
        public AggregatorReducerFactory(IAggregator aggregator, Type classType)
        {
            _aggregator = aggregator;
            _classType = classType;
        }

        /// <summary>
        /// Provides incoming element with a new instance of Reducer to merge intermediate key-value pairs from Combiner.
        /// </summary>
        /// <param name="key">Key for new Reducer</param>
        /// <returns>New instance of IReducer.</returns>
        public IReducer Create(object key)
        {
            return new AggregatorReducer(key, _aggregator, _classType);
        }
    }
}