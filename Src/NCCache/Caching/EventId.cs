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
using Alachisoft.NCache.Persistence;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class EventId : ICloneable, ICompactSerializable
    {
        private string _eventUniqueId;
        private long _operationCounter;
        private int _eventCounter;
        private EventType _eventType;
        private string _queryId;
        private int _hashCode = -1;

        public EventId()
        {

        }

        public EventId(string eventUniqueId, long operationCounter, int eventCounter)
        {
            _eventUniqueId = eventUniqueId;
            _operationCounter = operationCounter;
            _eventCounter = eventCounter;
        }

        public string EventUniqueID
        {
            get
            {
                return _eventUniqueId;
            }

            set
            {
                _eventUniqueId = value;
            }
        }

        public long OperationCounter
        {
            get
            {
                return _operationCounter;
            }

            set
            {
                _operationCounter = value;
            }
        }

        public int EventCounter
        {
            get
            {
                return _eventCounter;
            }

            set
            {
                _eventCounter = value;
            }
        }
        public EventType EventType
        {
            get
            {
                return _eventType;
            }

            set
            {
                _eventType = value;
            }
        }

        public string QueryId
        {
            get { return _queryId; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    _queryId = value;
            }
        }

        public static EventId CreateEventId(OperationID opId)
        {
            EventId eventId = new EventId();
            if (opId != null)
            {
                eventId._eventUniqueId = opId.OperationId;
                eventId._operationCounter = opId.OpCounter;
            }
            return eventId;
        }

        public override int GetHashCode()
        {
            if (_hashCode == -1 && _eventUniqueId == null)
                return base.GetHashCode();
            else if(_hashCode == -1)
                _hashCode = (_eventUniqueId + _eventCounter.ToString() + ":" + OperationCounter.ToString() + ":" + _eventType.ToString() + ":" + _queryId).GetHashCode();

            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            EventId eventId = (EventId)obj;
            if (this.EventUniqueID == eventId.EventUniqueID && 
                this.EventCounter == eventId.EventCounter && 
                this.OperationCounter == eventId.OperationCounter && 
                this.EventType == eventId.EventType &&
                this._queryId == eventId._queryId)
            {
                return true;
            }
            return false;
        }
        #region ICloneable Members

        public object Clone()
        {
            EventId ei = new EventId();
            lock (this)
            {
                ei._eventUniqueId = _eventUniqueId;
                ei._operationCounter = _operationCounter;
                ei._eventCounter = _eventCounter;
                ei._eventType = _eventType;
                ei._queryId = _queryId;
            }
            return ei;
        }

        #endregion

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _eventCounter = reader.ReadInt32();
            _eventUniqueId = (string)reader.ReadObject();
            _operationCounter = reader.ReadInt64();
            _eventType = (EventType)reader.ReadInt32();
            _queryId = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_eventCounter);
            writer.WriteObject(_eventUniqueId);
            writer.Write(_operationCounter);
            writer.Write((int)_eventType);
            writer.WriteObject(_queryId);
        }
    }
}
