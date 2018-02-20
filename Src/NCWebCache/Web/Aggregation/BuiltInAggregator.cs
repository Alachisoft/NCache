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

using Alachisoft.NCache.Runtime.Aggregation;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Web.Aggregation
{
    /// <summary>
    /// Sets current Built in aggregator instance.
    /// Performs actual grouping and analytical operations on data.
    /// IAggregator can perform following operations
    /// Average, Sum, Min, Max, Count, Distinct.
    /// </summary>
    public class BuiltInAggregator
    {
        /// <summary>
        /// Constructor to initialize instance of class with Integer Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator IntegerSum()
        {
            return new IntegerAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Count type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator Count()
        {
            return new CountAggregator();
        }

        /// <summary>
        /// Constructor to initialize instance of class with Distinct type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator Distinct()
        {
            return new DistinctAggregator();
        }

        /// <summary>
        /// Constructor to initialize instance of class with Double Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DoubleSum()
        {
            return new DoubleAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Float Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator FloatSum()
        {
            return new FloatAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Decimal Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DecimalSum()
        {
            return new DecimalAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Big Integer Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
#if NET40
        public static IAggregator BigIntegerSum()
        {
            return new BigIntegerAggregator(AggregateFunctionType.SUM);
        }
#endif
        /// <summary>
        /// Constructor to initialize instance of class with Long Integer Sum type aggregator.
        /// </summary>
        /// <returns></returns>
        public static IAggregator LongSum()
        {
            return new LongAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Short Integer Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator ShortSum()
        {
            return new ShortAggregator(AggregateFunctionType.SUM);
        }

        /// <summary>
        /// Constructor to initialize instance of class with Integer Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator IntegerAvg()
        {
            return new IntegerAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Integer Sum type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DoubleAvg()
        {
            return new DoubleAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Float Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator FloatAvg()
        {
            return new FloatAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Decimal Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DecimalAvg()
        {
            return new DecimalAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Big INteger Integer Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
#if NET40
        public static IAggregator BigIntegerAvg()
        {
            return new BigIntegerAggregator(AggregateFunctionType.AVG);
        }
#endif
        /// <summary>
        /// Provides instance of class with Long Integer Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator LongAvg()
        {
            return new LongAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Short Average type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator ShortAvg()
        {
            return new ShortAggregator(AggregateFunctionType.AVG);
        }

        /// <summary>
        /// Provides instance of class with Integer Min type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator IntegerMin()
        {
            return new IntegerAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Double Min type aggregator.
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DoubleMin()
        {
            return new DoubleAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Float Min type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator FloatMin()
        {
            return new FloatAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Decimal Min type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DecimalMin()
        {
            return new DecimalAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Big Integer Min type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
#if NET40
        public static IAggregator BigIntegerMin()
        {
            return new BigIntegerAggregator(AggregateFunctionType.MIN);
        }
#endif
        /// <summary>
        /// Provides instance of class with Long Integer Min type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator LongMin()
        {
            return new LongAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Short Integer Min type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator ShortMin()
        {
            return new ShortAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Integer Maximum type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator IntegerMax()
        {
            return new IntegerAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Double Maximum type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DoubleMax()
        {
            return new DoubleAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Float Maximum type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator FloatMax()
        {
            return new FloatAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Double Max type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DecimalMax()
        {
            return new DecimalAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Maximum Big Integer type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
#if NET40
        public static IAggregator BigIntegerMax()
        {
            return new BigIntegerAggregator(AggregateFunctionType.MAX);
        }
#endif
        /// <summary>
        ///  Provides instance of class with Maximum Long type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator LongMax()
        {
            return new LongAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Maximum Short type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator ShortMax()
        {
            return new ShortAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Mximum String type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator StringMax()
        {
            return new StringAggregator(AggregateFunctionType.MAX);
        }

        /// <summary>
        /// Provides instance of class with Minumum string type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator StringMin()
        {
            return new StringAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        /// Provides instance of class with Minimum date time type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DateTimeMin()
        {
            return new DateAggregator(AggregateFunctionType.MIN);
        }

        /// <summary>
        ///Provides instance of class with Maximum date time type aggregator
        /// </summary>
        /// <returns>IAggregator instance.</returns>
        public static IAggregator DateTimeMax()
        {
            return new DateAggregator(AggregateFunctionType.MAX);
        }
    }
}