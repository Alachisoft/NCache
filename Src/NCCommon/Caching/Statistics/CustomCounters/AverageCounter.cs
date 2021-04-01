//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public class AverageCounter : InstantaneousCounter
    {
        private double _sum = 0;
        private double _totalCount = 0;
        private double _lastSum = 0;
        private double _lastTotalCount = 0;

        public AverageCounter(string name, string instance) : base(name, instance)
        {

        }

        public AverageCounter(string category, string name, string instance) : base(category, name, instance)
        {

        }

        public double Sum { get { return _lastSum; } }

        public double Total { get { return _lastTotalCount; } }

        protected override void Calculate(double value)
        {
            _sum += value;
        }

        public void IncrementBy(double value, double count)
        {
            lock (this)
            {
                base.IncrementBy(value);
                _totalCount += count;
                if (_totalCount != 0)
                    _value = _sum / _totalCount;
                else
                    _value = 0;
            }
        }

        public void IncrementBase(long count)
        {
            lock (this)
            {
                _totalCount += count;
            }
        }


        public override void IncrementBy(double value)
        {
            IncrementBy(value, 1);
        }

        protected override void FlipChanged()
        {
            _lastSum = _sum;
            _lastTotalCount = _totalCount;
            _sum = 0;
            _totalCount = 0;
        }


        public override void Decrement()
        {
        }

        public override void DecrementBy(double value)
        {
        }

        public override double Value
        {
            set
            {
                _lastValue = this._value;
                this._value = value;
            }
        }


    }
}
