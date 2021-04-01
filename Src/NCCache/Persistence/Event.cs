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
using System.Collections.Generic;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Persistence
{
   [Serializable]
   public class Event :ICompactSerializable
    {
        private EventId _eventId;
        private EventInfo _eventInfo;

        public Event()
        {
            _eventId = new EventId();
            _eventInfo = new EventInfo();
        }

        public Event(EventId eId, EventInfo eInfo)
        {
            _eventId = eId;
            _eventInfo = eInfo;
        }


        public EventId PersistedEventId
        {
            get
            {
                return _eventId;
            }
            set
            {
                _eventId = value;
            }
        }

        public EventInfo PersistedEventInfo
        {
            get
            {
                return _eventInfo;
            }
            set
            {
                _eventInfo = value;
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _eventId = (EventId)reader.ReadObject();
            _eventInfo = (EventInfo)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_eventId);
            writer.WriteObject(_eventInfo);
        }
    }

 
}
