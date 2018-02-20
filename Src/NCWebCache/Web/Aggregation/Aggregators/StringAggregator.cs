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
using System.Collections;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Web.Aggregation
{
    /// <summary>
    /// Implements built in String type aggregator. 
    /// </summary>
    [Serializable]
    public class StringAggregator : IAggregator
    {
        AggregateFunctionType aggregateType;
        String result = "";

        public StringAggregator(AggregateFunctionType type)
        {
            this.aggregateType = type;
        }

        /// <summary>
        /// Performs given logic of aggregate on local node like combiner. 
        /// </summary>
        /// <param name="value">object</param>
        /// <returns>Retuns aggregated result.</returns>
        public object Aggregate(object value)
        {
            if (value is ICollection)
                return Calculate((ICollection)value);
            else
                return null;
        }

        /// <summary>
        /// Performs given logic of aggregate on all server nodes like Reduce phase operation. 
        /// </summary>
        /// <param name="value">object</param>
        /// <returns>Retuns aggregated result.</returns>
        public object AggregateAll(object value)
        {
            if (value is ICollection)
                return Calculate((ICollection)value);
            else
                return null;
        }

        private String Calculate(ICollection value)
        {
            switch (aggregateType)
            {
                case AggregateFunctionType.MIN:
                    result = (String)Calculation.Min(value, DataType.STRING);
                    break;
                case AggregateFunctionType.MAX:
                    result = (String)Calculation.Max(value, DataType.STRING);
                    break;
            }

            return result;
        }
    }
}
