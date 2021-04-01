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
using Alachisoft.NCache.Common.DataStructures;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Persistence
{
    public class PersistenceManager
    {
        //Event _event = new Event();
        SlidingIndex<Event> _index = new SlidingIndex<Event>(30);
        DateTime _startTime;

        public PersistenceManager(int perInterval) 
        {
            _index = new SlidingIndex<Event>(perInterval);
            _startTime = DateTime.Now;
        }

        public bool HasCompleteData()
        {
            TimeSpan timeElapsed = DateTime.Now -_startTime;
            return timeElapsed.TotalSeconds >= _index.GetInterval();
        }

        public void AddToPersistedEvent(Event evt)
        {
            if(evt != null)
                _index.AddToIndex(evt);
        }

        public List<Event> GetFilteredEventsList(string clientID, Hashtable events, EventStatus registeredEventStatus)
        {
            IEnumerator en = _index.GetCurrentData();
            Event evt;
            List<Event> filteredEvents = new List<Event>();

            while (en.MoveNext())
            {
                evt = (Event)en.Current;
                if (!events.Contains(evt.PersistedEventId))
                {
                    switch (evt.PersistedEventId.EventType)
                    {
                        case EventType.CACHE_CLEARED_EVENT:
                            if(registeredEventStatus.IsCacheClearedEvent)
                                filteredEvents.Add(evt);
                            break;      
                        case EventType.ITEM_REMOVED_EVENT:    
                            if(registeredEventStatus.IsItemRemovedEvent)
                                filteredEvents.Add(evt);
                            break;  
                        case EventType.ITEM_ADDED_EVENT:     
                            if(registeredEventStatus.IsItemAddedEvent)
                                filteredEvents.Add(evt);
                            break;
                        case EventType.ITEM_UPDATED_EVENT:
                             if(registeredEventStatus.IsItemUpdatedEvent)
                                filteredEvents.Add(evt);
                             break;
                                              
                        case EventType.ITEM_REMOVED_CALLBACK:
                        case EventType.ITEM_UPDATED_CALLBACK:
                             foreach (CallbackInfo cbInfo in evt.PersistedEventInfo.CallBackInfoList)
                             {
                                 if (cbInfo != null && cbInfo.Client != null && cbInfo.Client.Equals(clientID))
                                 {
                                     if (!filteredEvents.Contains(evt))
                                     {
                                         filteredEvents.Add(evt);
                                     }
                                 }
                             }
                             break;

                       
                    }
                }

            }

            return filteredEvents;
        }

        public void Dispose()
        {
            _index = null;
        }
    }
}
