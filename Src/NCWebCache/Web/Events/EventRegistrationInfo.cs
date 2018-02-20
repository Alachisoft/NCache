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

using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    internal class EventRegistrationInfo
    {
        private EventType _eventType;
        private EventDataFilter _filter;
        private short _registrationSequence;

        public EventRegistrationInfo()
        {
        }

        public EventRegistrationInfo(EventType eventTYpe, EventDataFilter filter, short sequenceId)
        {
            _eventType = eventTYpe;
            _filter = filter;
            _registrationSequence = sequenceId;
        }

        public EventType EventTYpe
        {
            get { return _eventType; }
            set { _eventType = value; }
        }

        public EventDataFilter DataFilter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public short RegistrationSequence
        {
            get { return _registrationSequence; }
            set { _registrationSequence = value; }
        }
    }
}
