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
using System.Threading;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class KeyInfo : ICloneable
    {
        private int _refCount;
        private HPTime _updatedTime;
        private string _updatedBy;

        public HPTime UpdatedTime
        {
            get { return _updatedTime; }
            set { _updatedTime = value; }
        }

        public int RefCount
        {
            get { return _refCount; }
        }

        public string UpdatedBy
        {
            get { return _updatedBy; }
            set { _updatedBy = value; }
        }

        public KeyInfo()
        {
            _refCount = 1;
            _updatedTime = HPTime.Now;
        }

        public int IncrementRefCount()
        {
            return Interlocked.Increment(ref _refCount);
        }

        public int DecrementRefCount()
        {
            return Interlocked.Decrement(ref _refCount);
        }

        public object Clone()
        {
            return new KeyInfo() { _refCount = this.RefCount, _updatedTime = this.UpdatedTime, _updatedBy = this._updatedBy };
        }
    }
}