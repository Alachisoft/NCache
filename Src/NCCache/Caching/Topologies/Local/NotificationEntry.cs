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
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal struct NotificationEntry
    {
        private bool _notifyOnUpdate;
        private bool _notifyOnRemove;
        private HPTime _registrationTime;

        public bool NotifyOnUpdate { get { return _notifyOnUpdate; } }

        public bool NotifyOnRemove { get { return _notifyOnRemove; } }

        public HPTime RegistrationTime { get { return _registrationTime; } set { _registrationTime = value; } }

        public NotificationEntry(CallbackInfo updateCallback, CallbackInfo removeCallback)
            : this()
        {
            SetNotifications(updateCallback, removeCallback);
        }

        public void SetNotifications(CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            if (updateCallback != null && updateCallback.CallbackType == CallbackType.PullBasedCallback)
                _notifyOnUpdate = true;
            if (removeCallback != null && removeCallback.CallbackType == CallbackType.PullBasedCallback)
                _notifyOnRemove = true;
        }
    }
}