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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// An info object that wraps multiple function codes and objects. Function codes are to be
    /// defined by the clients/derivations of clustered cache.
    /// </summary>
    [Serializable]
    internal class AggregateFunction : ICompactSerializable, IRentableObject
    {
        private Array _funcs;
        private int _rentId;
        public AggregateFunction(params Function[] funcs)
        {
            _funcs = Array.CreateInstance(typeof(Function), funcs.Length);
            funcs.CopyTo(_funcs, 0);
        }

        public Array Functions
        {
            get { return _funcs; }
            set { _funcs = value; }
        }

        #region	/                 --- ICompactSerializable ---           /

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _funcs = (Array)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.WriteObject(_funcs);
        }

        #endregion

        #region IRentableObject Members

        /// <summary>
        /// Gets or sets the rent id.
        /// </summary>
        public int RentId
        {
            get
            {
                return _rentId;
            }
            set
            {
                _rentId = value;
            }
        }

        #endregion
    }
}