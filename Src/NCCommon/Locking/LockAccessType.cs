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

namespace Alachisoft.NCache.Common.Locking
{
    public enum LockAccessType:byte
    {
        /// <summary>Indicates that lock is to be acquired.</summary>
        ACQUIRE = 1,
        /// <summary>Perform the operation only if item is not locked but dont acquire the lock</summary>
        DONT_ACQUIRE,
        /// <summary>Indicates that lock is to be released.</summary>
        RELEASE,
        /// <summary>Indicates that lock is not to be released.</summary>
        DONT_RELEASE,
        /// <summary>Perform the operation as if there is no lock.</summary>
        IGNORE_LOCK,
        /// <summary>Optimistic locking; update the item in the cache only if the version is same.</summary>
        
        //muds:
        //this helps to preserve the version of the cache item when in case of client cache we
        //remove the local copy of the item before updating the remote copy of the cache item.
        //this is for internal use only.
        PRESERVE_VERSION,
        DEFAULT
    }
}
