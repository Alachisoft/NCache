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
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using System.Collections;
using Alachisoft.NCache.Caching;

namespace Alachisoft.NCache.Web.Persistence
{
    class PersistenceManager
    {
        SlidingIndex<EventId> _index = new SlidingIndex<EventId>(30);
        bool isStoreEventsEnabled = false;
        Hashtable _duplicateEventLog;
        bool _checkEventDuplication = false;
        DateTime _eventDuplicationStartTime;

        public void StartEventDuplicationCheck()
        {
        }

        public bool StopEventDuplicationCheck()
        {
            return false;
        }

        private bool CheckEventDuplication(EventId eventId)
        {
            lock (this)
            {
                if (_checkEventDuplication && !StopEventDuplicationCheck())
                {
                    if (_duplicateEventLog.ContainsKey(eventId))
                        return true;
                    else
                    {
                        _duplicateEventLog.Add(eventId, null);
                    }
                }

                return false;
            }
        }


        public PersistenceManager(int interval)
        {
            _index = new SlidingIndex<EventId>(interval, true);
        }

        public bool PersistEvent(EventId evtId)
        {
            if (evtId != null)
                return _index.AddToIndex(evtId);

            return true;
        }

        public List<EventId> GetPersistedEventsList()
        {
            IEnumerator en = _index.GetCurrentData();
            EventId evtId;
            List<EventId> events = new List<EventId>();

            while (en.MoveNext())
            {
                evtId = (EventId) en.Current;
                events.Add(evtId);
            }

            return events;
        }

        public void Dispose()
        {
            _index = null;
        }
    }
}