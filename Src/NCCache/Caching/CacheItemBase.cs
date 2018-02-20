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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class CacheItemBase: Runtime.Serialization.ICompactSerializable
    {
        /// <summary> The actual object provided by the client application </summary>
        private object _v = null;

        protected CacheItemBase() { }

        /// <summary>
        /// Default constructor. No call back.
        /// </summary>
        public CacheItemBase(object obj)
        {
            if (obj is byte[]) obj = UserBinaryObject.CreateUserBinaryObject((byte[])obj);
            _v = obj;
        }

        /// <summary> 
        /// The actual object provided by the client application 
        /// </summary>
        public virtual object Value
        {
            get { return _v; }
            set { _v = value; }
        }

        #region ICompact Serializable
        public void Deserialize(CompactReader reader)
        {
            _v = reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_v);
        } 
        #endregion
    }
}