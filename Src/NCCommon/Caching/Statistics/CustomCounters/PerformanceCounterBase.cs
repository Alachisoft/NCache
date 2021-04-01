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
using System.Text;

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public abstract class PerformanceCounterBase : PerformanceCounter
    {
        private string _name;
        private string _instanceName;
        private string _category;
        protected double _value;
        protected double _lastValue;

        public string Name { get { return _name; } }

        public string InstanceName { get { return _instanceName; } }

        public string Category { get { return _category; } }

        public abstract double Value { get; set; }

        public PerformanceCounterBase(string name, string instance)
        {
            _name = name;
            _instanceName = instance;
            _category = null;
            _value = 0;
            _lastValue = 0;
        }

        public PerformanceCounterBase(string category, string name, string instance)
        {
            _name = null;
            _instanceName = instance;
            _category = category;
            _value = 0;
            _lastValue = 0;
        }

        public abstract void Decrement();

        public abstract void DecrementBy(double value);

        public abstract void Increment();

        public abstract void IncrementBy(double value);

        public void Reset()
        {
            lock (this)
            {
                _value = _lastValue = 0;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            stringBuilder.Append(_name != null ? "Name :" + _name : "");
            stringBuilder.Append(_instanceName != null ? "; Instance :" + _instanceName : "");
            stringBuilder.Append(_category != null ? "; Category :" + _category + "]" : "");
            return stringBuilder.ToString();
        }

    }
}
