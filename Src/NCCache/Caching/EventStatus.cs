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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class EventStatus :ICompactSerializable
    {
        private bool _cacheCleared=false;
        private bool _itemAdded = false;
        private bool _itemUpdated = false;
        private bool _itemRemoved = false;

        public EventStatus()
        {
        }

        public bool IsCacheClearedEvent
        {
            get
            {
                return _cacheCleared;
            }
            set
            {
                _cacheCleared = value;
            }
        }

        public bool IsItemAddedEvent
        {
            get
            {
                return _itemAdded;
            }
            set
            {
                _itemAdded = value;
            }
        }

        public bool IsItemUpdatedEvent
        {
            get
            {
                return _itemUpdated;
            }

            set
            {
                _itemUpdated = value;
            }
        }

        public bool IsItemRemovedEvent
        {
            get
            {
                return _itemRemoved;
            }

            set
            {
                _itemRemoved = value;
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cacheCleared = reader.ReadBoolean();
            _itemAdded = reader.ReadBoolean();
            _itemRemoved = reader.ReadBoolean();
            _itemUpdated = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_cacheCleared);
            writer.Write(_itemAdded);
            writer.Write(_itemRemoved);
            writer.Write(_itemUpdated);
        }
    }
}