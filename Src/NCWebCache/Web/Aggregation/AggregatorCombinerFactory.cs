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
using Alachisoft.NCache.Runtime.Aggregation;
using Alachisoft.NCache.Runtime.MapReduce;

namespace Alachisoft.NCache.Web.Aggregation
{
    /// <summary>
    /// Assigns a unique Combiner for each provided key.
    /// </summary>
    [Serializable]
    public class AggregatorCombinerFactory : ICombinerFactory
    {
        private readonly IAggregator _aggregator;
        private readonly Type _classType;

        /// <summary>
        /// Constructor to initialize instance of class.
        /// </summary>
        /// <param name="aggregator">instance of IAggregator</param>
        /// <param name="classType">Class data type</param>
        public AggregatorCombinerFactory(IAggregator aggregator, Type classType)
        {
            _aggregator = aggregator;
            _classType = classType;
        }

        /// <summary>
        ///  Provides incoming element with a new instance of Combiner to merge intermediate key-value pairs from Mapper.
        /// </summary>
        /// <param name="key">Key for new Combiner</param>
        /// <returns>New instance of ICombiner.</returns>
        public ICombiner Create(object key)
        {
            return new AggregatorCombiner(_aggregator, _classType);
        }
    }
}