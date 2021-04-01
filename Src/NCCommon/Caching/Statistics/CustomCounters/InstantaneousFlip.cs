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

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public class InstantaneousFlip : ICloneable
    {
        private long _flip;

        public long Flip { get { return _flip; } }

        public InstantaneousFlip(long flip)
        {
            _flip = flip;
        }

        public void Increment()
        {
            _flip++;
        }

        public object Clone()
        {
            return new InstantaneousFlip(_flip);
        }

        public override bool Equals(object obj)
        {
            InstantaneousFlip other = obj as InstantaneousFlip;
            if (other != null)
            {
                return Flip == other.Flip;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _flip.GetHashCode();
        }
    }
}
