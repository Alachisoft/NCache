// Copyright (c) 2017 Alachisoft
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Integrations.Memcached.Provider;

namespace HashCacheProvider
{
    class StoreObject
    {
        public uint Flags
        {
            get;
            set;
        }

        public object Value
        {
            get;
            set;
        }
    }
    public static class HashedCache
    {
        private static Hashtable _ht = new Hashtable();

        public static OperationResult Set(string key, object value, uint flags)
        {
            StoreObject obj = new StoreObject();
            obj.Flags = flags;
            obj.Value = value;
            lock (_ht)
            {
                _ht[key] = obj;
            }
            OperationResult result = new OperationResult();
            result.ReturnResult = Result.SUCCESS;
            ulong cas=1;
            result.Value = cas;

            return result;
        }

        public static List<GetOpResult> Get(string [] keys)
        {
            List<GetOpResult> results = new List<GetOpResult>();

            foreach (string key in keys)
            {
                StoreObject obj = null;
                lock (_ht)
                {
                    obj = (StoreObject)_ht[key];
                }

                if (obj != null)
                {
                    GetOpResult r = new GetOpResult();
                    r.Value = obj.Value;
                    r.Key = key;
                    r.Flag = obj.Flags;
                    r.Version = 1;
                    r.ReturnResult = Result.SUCCESS;
                    results.Add(r);
                }
            }

            return results;
        }

    
    }
}
