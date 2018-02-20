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
    /// Provides basic implementation of Aggregator. 
    /// </summary>
    [Serializable]
    public sealed class AggregatorTask
    {
        private readonly MapReduceTask _aggregatorTask;
        private readonly IValueExtractor _extractor;
        private readonly IAggregator _aggregator;
        private AggregatorCombinerFactory _aggregatorCombinerFactory = null;
        private AggregatorMapper _aggregatorMapper = null;
        private AggregatorReducerFactory _aggregatorReducerFactory = null;
        private Type _aggregatorInputType = typeof(object);

        /// <summary>
        /// Initialize an instance of the class.
        /// </summary>
        /// <param name="valueExtractor">instance of IValueExtractor</param>
        /// <param name="aggregator">instance ofIAggregator</param>
        public AggregatorTask(IValueExtractor valueExtractor, IAggregator aggregator)
        {
            _aggregatorTask = new MapReduceTask();
            _extractor = valueExtractor;
            _aggregator = aggregator;
        }

        /// <summary>
        /// Create Map Reduce Task using given Mapper, Combiner and Reducer for currrent aggregator.
        /// </summary>
        /// <returns></returns>
        public MapReduceTask CreateMapReduceTask()
        {
            _aggregatorMapper = new AggregatorMapper(_extractor);
            _aggregatorCombinerFactory = new AggregatorCombinerFactory(_aggregator, _aggregatorInputType);
            _aggregatorReducerFactory = new AggregatorReducerFactory(_aggregator, _aggregatorInputType);
            _aggregatorTask.Mapper = _aggregatorMapper;
            _aggregatorTask.Combiner = _aggregatorCombinerFactory;
            _aggregatorTask.Reducer = _aggregatorReducerFactory;
            return _aggregatorTask;
        }

        /// <summary>
        /// Returns current Built in Aggregator type. 
        /// </summary>
        public Type BuiltInAggregatorType
        {
            get { return AggregatorType; }
        }

        /// <summary>
        /// Returns instance of built in Aggregator 
        /// </summary>
        public Type AggregatorType
        {
            get
            {
                Type typeClass = _aggregator.GetType();
                if (IsTypeOf(_aggregator.GetType()))
                {
                    if (_aggregator.GetType().Equals(typeof(IntegerAggregator)))
                        typeClass = typeof(int);
                    else if (_aggregator.GetType().Equals(typeof(DoubleAggregator)))
                        typeClass = typeof(double);
                    else if (_aggregator.GetType().Equals(typeof(LongAggregator)))
                        typeClass = typeof(long);
                    else if (_aggregator.GetType().Equals(typeof(ShortAggregator)))
                        typeClass = typeof(short);
                    else if (_aggregator.GetType().Equals(typeof(DecimalAggregator)))
                        typeClass = typeof(decimal);
                    else if (_aggregator.GetType().Equals(typeof(FloatAggregator)))
                        typeClass = typeof(float);
                    else if (_aggregator.GetType().Equals(typeof(StringAggregator)))
                        typeClass = typeof(string);
                    else if (_aggregator.GetType().Equals(typeof(DateAggregator)))
                        typeClass = typeof(DateTime);
                }

                return typeClass;
            }
        }

        private bool IsTypeOf(Type classType)
        {
            return classType.Equals(typeof(IntegerAggregator))
                   || classType.Equals(typeof(DoubleAggregator))
                   || classType.Equals(typeof(LongAggregator))
                   || classType.Equals(typeof(ShortAggregator))
                   || classType.Equals(typeof(DecimalAggregator))
                   || classType.Equals(typeof(IntegerAggregator))
                   || classType.Equals(typeof(FloatAggregator))
                   || classType.Equals(typeof(StringAggregator))
                   || classType.Equals(typeof(DateAggregator))
                   || classType.Equals(typeof(CountAggregator))
                   || classType.Equals(typeof(DistinctAggregator));
        }
    }
}