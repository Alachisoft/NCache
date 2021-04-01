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
using Alachisoft.NCache.Common.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Tools.Common
{
    /// <summary>
    /// Container for dumping data structures
    /// </summary>
    internal struct DumpDSItem
    {
        /// <summary>
        /// Key on which data structure must be stored
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Specifies what data is currently stored in the container
        /// </summary>
        public EntryType DataType { get; set; }

        /// <summary>
        /// Populate this when you need to dump any values that resemble an array
        /// </summary>
        public IList<object> ListData { get; set; }

        /// <summary>
        /// Populate this when you need to dump any values that resemble a keypair
        /// </summary>
        public IDictionary<string, object> DictData { get; set; }

        /// <summary>
        /// Constructor for initializing Data Structure Dump Item
        /// </summary>
        /// <param name="key">key of cache item</param>
        /// <param name="dataType">type of data</param>
        /// <param name="listData">data of list of objects</param>
        /// <param name="dictData">data of dictionary containing keyvalue pair of string and object</param>
        public DumpDSItem(string key, EntryType dataType, IList<object> listData, IDictionary<string, object> dictData)
        {
            Key = key;
            DataType = dataType;
            ListData = listData;
            DictData = dictData;
        }
    }
}
