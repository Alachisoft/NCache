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
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public class RateOfCounter : InstantaneousCounter
    {
        public RateOfCounter(string name, string instance) : base(name, instance)
        {

        }

        public RateOfCounter(string category, string name, string instance) : base(category, name, instance)
        {

        }

        protected override void Calculate(double value)
        {
            _value += value;
        }


        protected override void FlipChanged()
        {
        }

        public override void Decrement()
        {
        }


        public override void DecrementBy(double value)
        {
        }

        public override double Value
        {
            get
            {
                UpdateIfFlipChanged();
                return _lastValue;
            }
            set { _value = value; }
        }
    }
}
