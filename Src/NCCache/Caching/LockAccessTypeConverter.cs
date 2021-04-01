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
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.Caching
{
    public class LockAccessTypeConverter
    {
        public static string ToString(LockAccessType accessType)
        {
            switch (accessType)
            {
                case LockAccessType.ACQUIRE:
                    return "1";

                case LockAccessType.DONT_ACQUIRE:
                    return "2";

                case LockAccessType.RELEASE:
                    return "3";

                case LockAccessType.DONT_RELEASE:
                    return "4";

                case LockAccessType.IGNORE_LOCK:
                    return "5";

                case LockAccessType.PRESERVE_VERSION:
                    return "9";
            }
            return string.Empty;
        }

        public static LockAccessType FromString(string str)
        {
            switch (str)
            { 
                case "1":
                    return LockAccessType.ACQUIRE;

                case "2":
                    return LockAccessType.DONT_ACQUIRE;

                case "3":
                    return LockAccessType.RELEASE;

                case "4":
                    return LockAccessType.DONT_RELEASE;

                case "5":
                    return LockAccessType.IGNORE_LOCK;

                case "9":
                    return LockAccessType.PRESERVE_VERSION;
            }
            return LockAccessType.DEFAULT;
        }
    }
}