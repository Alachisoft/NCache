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
namespace Alachisoft.NCache.Common.DataStructures
{
    public class XReference
    {
        private long _sourceId;
        private DateTime _creationTime = DateTime.Now;

        public XReference(long requestId)
        {
            this._sourceId = requestId;
        }
        public long SourceId { get { return _sourceId; } }

        public DateTime CreationTime { get { return _creationTime; } }

        public override int GetHashCode()
        {
            return _sourceId.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is long)
            {
                return _sourceId == (long)obj;
            }
            else if (obj is XReference)
            {
                return _sourceId == ((XReference)obj)._sourceId;
            }
            return false;
        }
    }
}
