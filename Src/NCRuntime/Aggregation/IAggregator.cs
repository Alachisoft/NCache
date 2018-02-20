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

namespace Alachisoft.NCache.Runtime.Aggregation
{
    /// <summary>
    /// Performs actual grouping and analytical operations on data.
    /// Aggregator can perform following operations:
    /// Average, Sum, Min, Max, Count, Distinct.
    /// If result after aggregation execution is null than default value of built in Aggregator for that specific type is returned. 
    /// Custom aggregator, for custom data types and  custom functions like Mean, Median, Mode can also be implementated.
    /// </summary>
    public interface IAggregator
    {
        /// <summary>
        /// Performs given logic of aggregation on a on local node like combiner. 
        /// </summary>
        /// <param name="value">object</param>
        /// <returns>Retuns aggregated result.</returns>
        /// <example>
        /// Following example illustrate the implementation of Aggregate. 
        /// <code>
        /// string function;
        /// //setting current aggregator function
        /// 
        /// public IntAggregator(string function)
        /// {
        ///    this.function = function;
        /// }
        /// 
        /// //Implementing interface function
        /// 
        /// public object Aggregate(object value)
        /// {
        ///    return calculate(value);
        /// }
        /// //Function to calculate values
        /// 
        /// private object calculate(object value)
        /// {
        ///    switch (function)
        ///    {
        ///        case "MIN":
        ///            value = int.MinValue;
        ///            return value;
        ///        case "MAX":
        ///            value = int.MaxValue;
        ///            return value;
        ///        default:
        ///            return 0;
        ///    }
        ///}
        /// </code>
        /// </example>
        object Aggregate(object value);
        /// <summary>
        /// Performs given logic of aggregation on server nodes like Reduce phase operation. 
        /// </summary>
        /// <param name="value">object</param>
        /// <returns>Retuns aggregated result.</returns>
        /// <example>
        /// Following example illustrate the implementation of Aggregate. 
        /// <code>
        /// string function;
        /// //setting current aggregator function
        /// 
        /// public IntAggregator(string function)
        /// {
        ///    this.function = function;
        /// }
        /// 
        /// //Implementating interface function
        /// 
        /// public object AggregateAll(object value)
        /// {
        ///    return calculate(value); //implement inside logic.
        /// }
        /// //Function to calculate values
        /// 
        /// private object calculate(object value)
        /// {
        ///    switch (function)
        ///    {
        ///        case "MIN":
        ///            value = int.MinValue;
        ///            return value;
        ///        case "MAX":
        ///            value = int.MaxValue;
        ///            return value;
        ///        default:
        ///            return 0;
        ///    }
        ///}
        /// </code>
        /// </example>
        object AggregateAll(object value);

    }
}
