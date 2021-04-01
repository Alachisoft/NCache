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
using System.Diagnostics;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Monitoring.CustomEventEntryLogging;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Common.Monitoring
{
	/// <summary>
	/// Contains the informatio about events that occurred in event viewer.
	/// </summary>
	[Serializable]
	public class EventViewerEvent : ICloneable, ICompactSerializable
	{
		private long _instanceId;
		private DateTime _timeGenerated;
		private string _source;
		private string _message;
		private string _node;
		private EventLogEntryType _type = EventLogEntryType.Information;

		/// <summary>
		/// Default constructor. 
		/// </summary>
		public EventViewerEvent() { }

		/// <summary>
		/// Initializes a new instance of EventViewerEvent.
		/// </summary>
		/// <param name="source">Sourece of the event.</param>
		/// <param name="eventId">Event id</param>
		/// <param name="eventDescription">Event details</param>
		/// <param name="time">Time of the occurance of event.</param>
		/// <param name="node">Node at which event occurred</param>
		/// <param name="type">Type of the event</param>
		public EventViewerEvent(EventLogEntry logEntry)
		{
			_source = logEntry.Source;
			_instanceId = logEntry.InstanceId;
			_timeGenerated = logEntry.TimeGenerated;
			_type = logEntry.EntryType;
			_message = logEntry.Message;
            _node = logEntry.MachineName;
		}

        /// <summary>
		/// Initializes a new instance of EventViewerEvent.
		/// </summary>
		/// <param name="source">Sourece of the event.</param>
		/// <param name="eventId">Event id</param>
		/// <param name="eventDescription">Event details</param>
		/// <param name="time">Time of the occurance of event.</param>
		/// <param name="node">Node at which event occurred</param>
		/// <param name="type">Type of the event</param>
		public EventViewerEvent(CustomEventEntry customEventEntry)
        {
            _source = customEventEntry.Source;
            _instanceId = customEventEntry.EventId;
            _timeGenerated = customEventEntry.TimeStamp;
            _type = customEventEntry.Level;
            _message = customEventEntry.Message;
        }

        /// <summary>
        /// Gets the Id of the event.
        /// </summary>
        public long InstanceID
		{
			get { return _instanceId; }
		}

		/// <summary>
		/// Gets the source of the event.
		/// </summary>
		public string Source
		{
			get { return _source; }
		}

		/// <summary>
		/// Gets the time of the event.
		/// </summary>
		public DateTime TimeGenerated
		{
			get { return _timeGenerated; }
		}

		/// <summary>
		/// Gets the detail of the event.
		/// </summary>
		public string Message
		{
			get { return _message; }
		}

		/// <summary>
		/// Gets the type of the event.
		/// </summary>
		public EventLogEntryType EventType
		{
			get { return _type; }
		}

		public string Machine
		{
			get { return _node; }
			set { _node = value; }
		}


		#region ICloneable Members

		public object Clone()
		{
			EventViewerEvent eventViewerEntry = new EventViewerEvent();
				eventViewerEntry._instanceId = this.InstanceID;
				eventViewerEntry._message = this.Message;
				eventViewerEntry._type = this.EventType;
				eventViewerEntry._source = this.Source;
				eventViewerEntry._timeGenerated = this.TimeGenerated;
				eventViewerEntry._node = this.Machine;
			return eventViewerEntry;
		}

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _instanceId = reader.ReadInt64();
            _timeGenerated = reader.ReadDateTime();
            _source = reader.ReadObject() as string;
            _message = reader.ReadObject() as string;
            _node = reader.ReadObject() as string;
            _type = (EventLogEntryType)reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_instanceId);
            writer.Write(_timeGenerated);
            writer.WriteObject(_source);
            writer.WriteObject(_message);
            writer.WriteObject(_node);
            writer.Write((int)_type);
        }

        #endregion
    }
}
