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
    /// Aggregator mapper interface generates a set of intermediate key-value pairs for further refining and extraction of the data.
    /// </summary>
    [Serializable]
    public class AggregatorMapper : IMapper
    {
        private readonly IValueExtractor _valueExtractor;
        private readonly string _aggKey = "AggregatorKey";

        /// <summary>
        ///  Construtor to initialize instance of class.
        /// </summary>
        /// <param name="valueExtractor">instance of IValue Extractor</param>
        public AggregatorMapper(IValueExtractor valueExtractor)
        {
            _valueExtractor = valueExtractor;
        }

        /// <summary>
        /// For every key-value pair input, Map method is executed, to get a more specific and meaningful data. 
        /// </summary>
        /// <param name="key">Key value of cache Entry.</param>
        /// <param name="value">Value for the key</param>
        /// <param name="context">Emitted output value for each key-value pair</param>
        public void Map(object key, object value, IOutputMap context)
        {
            if (value != null)
                context.Emit(_aggKey, _valueExtractor.Extract(value));
        }

        public void Dispose()
        {
        }
    }
}