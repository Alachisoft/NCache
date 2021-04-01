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
    public abstract class InstantaneousCounter : PerformanceCounterBase
    {
        InstantaneousFlip _currentFlip;

        public InstantaneousCounter(string name, string instance) : base(name, instance)
        {
            _currentFlip = FlipManager.Flip.Clone() as InstantaneousFlip;
        }

        public InstantaneousCounter(string category, string name, string instance) : base(category, name, instance)
        {
            _currentFlip = FlipManager.Flip.Clone() as InstantaneousFlip;
        }


        public override void Increment()
        {
            IncrementBy(1);
        }

        public override void IncrementBy(double value)
        {
            lock (this)
            {
                if (!_currentFlip.Equals(FlipManager.Flip))
                {
                    if (FlipManager.Flip.Flip == _currentFlip.Flip + 1)
                    {
                        _lastValue = _value;
                    }
                    else
                    {
                        _lastValue = 0;
                    }
                    _value = 0;
                    _currentFlip = FlipManager.Flip.Clone() as InstantaneousFlip;
                    FlipChanged();
                }
                Calculate(value);
            }
        }

        protected abstract void Calculate(double value);

        protected abstract void FlipChanged();

        public override double Value
        {
            get
            {
                UpdateIfFlipChanged();
                return _lastValue;
            }
        }

        protected void UpdateIfFlipChanged()
        {
            lock (this)
            {
                if (!_currentFlip.Equals(FlipManager.Flip))
                {
                    if (FlipManager.Flip.Flip == _currentFlip.Flip + 1)
                    {
                        _lastValue = _value;
                    }
                    else
                    {
                        _lastValue = 0;
                    }
                    _value = 0;
                    _currentFlip = FlipManager.Flip.Clone() as InstantaneousFlip;
                    FlipChanged();
                }
            }
        }
    }
}
