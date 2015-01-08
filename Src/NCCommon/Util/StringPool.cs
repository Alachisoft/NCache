// Copyright (c) 2015 Alachisoft
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
﻿﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{

    public class StringPool
    {
        private static Hashtable _pool = new Hashtable();
        private static object _sync_lock = new object();

        /// <summary>
        /// Gets a larg buffer from the pool.
        /// </summary>
        /// <returns></returns>
        public static String PoolString(String str)
        {
            lock (_sync_lock)
            {
                if (!_pool.Contains(str))
                {
                    _pool[str] = str;
                }

                return _pool[str] as string;
            }
        }

        /// <summary>
        /// Releases all the buffers.
        /// </summary>
        public static void Clear()
        {
            lock (_sync_lock)
            {
                _pool.Clear();
            }
        }
    }
}
