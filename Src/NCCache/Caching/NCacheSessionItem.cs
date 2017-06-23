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
using System.Text;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;


namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Just a wrapper class that ISessionCache uses to wrap its data.
    /// We wrap the NCacheSessionStore provided data into this class so that we
    /// can differentiate between the session items and other cache items. This way 
    /// we mantain a session item count inside the cache. In express edition, users can
    /// have max of 500 concurrent sessions in the cache. This session item count helps 
    /// us to impose this restriction.
    /// Use of a wrapper class eliminates the need of an extra flag to indicate the 
    /// session item. This way, the public interface for object caching and session caching
    /// remains the same.
    /// </summary>
    
    public class NCacheSessionItem : ICompactSerializable
    {
        private object _data;

        public NCacheSessionItem(object data)
        {
            _data = data;
        }

        public object Data
        {
            get { return _data; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _data = reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_data);
        }

        #endregion
    }
}
