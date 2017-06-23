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
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Integrations.Memcached.Provider
{
    public interface IMemcachedProvider
    {

        OperationResult InitCache(string cacheID);

        OperationResult Set(string key, uint flags, long expirationTimeInSeconds, object dataBlock);
        OperationResult Add(string key, uint flags, long expirationTimeInSeconds, object dataBlock);
        OperationResult Replace(string key, uint flags, long expirationTimeInSeconds, object dataBlock);
        OperationResult CheckAndSet(string key, uint flags, long expirationTimeInSeconds, ulong casUnique, object dataBlock);
       
        List<GetOpResult> Get(string[] keys);
        OperationResult Delete(string key,ulong casUnique);

        OperationResult Append(string key, object dataToAppend, ulong casUnique);
        OperationResult Prepend(string key, object dataToPrepend, ulong casUnique);

        MutateOpResult Increment(string key, ulong value, object initialValue, long expirationTimeInSeconds, ulong casUnique);
        MutateOpResult Decrement(string key, ulong value, object initialValue, long expirationTimeInSeconds, ulong casUnique);

        OperationResult Flush_All(long expirationTimeInSeconds);
        OperationResult Touch(string key, long expirationTimeInSeconds);
      
        OperationResult GetVersion();
        OperationResult GetStatistics(string argument);

        OperationResult ReassignSlabs(int sourceClassID, int destinationClassID);
        OperationResult AutomoveSlabs(int option);
        OperationResult SetVerbosityLevel(int verbosityLevel);

        void Dispose();
        
    }
}
