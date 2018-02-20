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
using Alachisoft.NCache.Runtime.Processor;

namespace Alachisoft.NCache.Processor
{
    public class NCacheMutableEntry : IMutableEntry
    {
        private string key = null;
        private object value = null;

        private bool isAvailableInCache = false;
        private bool isUpdated = false;
        private bool isRemoved = false;

        public NCacheMutableEntry(string key, object value)
        {
            this.key = key;

            if (value != null)
            {
                this.value = value;
                isAvailableInCache = true;
            }
        }

        public string Key
        {
            get { return this.key; }
        }

        public object Value
        {
            get { return this.value; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value cannot be null.");
                this.value = value;
                isUpdated = true;
                isRemoved = false;
            }
        }

        public object UnWrap(Type type)
        {
            return Convert.ChangeType(value, type);
        }

        public bool Exists()
        {
            return value != null;
        }

        public void Remove()
        {
            value = null;
            isRemoved = true;
            isUpdated = false;
        }

        public bool IsUpdated
        {
            get 
            {
                return isUpdated;
            }
        }

        public bool IsRemoved
        {
            get
            {
                return isAvailableInCache && isRemoved;
            }
        }
    }
}
