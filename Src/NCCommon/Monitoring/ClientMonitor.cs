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
using System.Collections;
using System.Threading;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class ClientMonitor: Runtime.Serialization.ICompactSerializable
    {
        private string _clientID;
        private string _address;
        private ArrayList _activities = new ArrayList();
        private Hashtable _currentActivities = Hashtable.Synchronized(new Hashtable());


        public ClientMonitor(string id, string address)
        {
            _clientID = id;
            _address = address;
        }

        public string ID
        {
            get { return _clientID; }
        }
        public string Address
        {
            get { return _address; }
        }
        public ClienfInfo Info
        {
            get { return new ClienfInfo(_clientID, _address); }
        }
        public void StartActivity()
        {
            ClientActivity acitvity = new ClientActivity();
            acitvity._thread = Thread.CurrentThread;
            int tId = Thread.CurrentThread.ManagedThreadId;

            lock (_currentActivities.SyncRoot)
            {
               if(!_currentActivities.ContainsKey(tId))
                  _currentActivities.Add(tId, acitvity);
            }
        }
        public void StopActivity()
        {
            int tId = Thread.CurrentThread.ManagedThreadId;
            ClientActivity activity = null;
            lock (_currentActivities.SyncRoot)
            {
                activity = _currentActivities[tId] as ClientActivity;
                _currentActivities.Remove(tId);
            }
            if (activity != null)
            {
                activity.StopActivity();
                lock (_activities.SyncRoot)
                {
                    _activities.Add(activity);
                }
            }
        }
        public void LogActivity(string method, string log)
        {
            ClientActivity activity = _currentActivities[Thread.CurrentThread.ManagedThreadId] as ClientActivity;
            if (activity != null)
            {
                activity.LogActivity(method, log);
            }
        }
        public void Clear()
        {
            lock (_activities.SyncRoot)
            {
                _activities.Clear();
            }
            lock (_currentActivities.SyncRoot)
            {
                _currentActivities.Clear();
            }
        }

        public ArrayList GetCompletedClientActivities()
        {
            ArrayList completedActivities = null;
            lock (_activities.SyncRoot)
            {
                completedActivities = _activities.Clone() as ArrayList;
            }
            return completedActivities;
        }
        
        public ArrayList GetCurrentClientActivities()
        {
            ArrayList completedActivities = new ArrayList();
            lock (_currentActivities.SyncRoot)
            {
                IDictionaryEnumerator ide = _currentActivities.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (ide.Value != null)
                        completedActivities.Add(((ICloneable)ide.Value).Clone());
                }
            }
            return completedActivities;
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _clientID = (string)reader.ReadObject();
            _address = (string)reader.ReadObject();
            _activities = (ArrayList)reader.ReadObject();
            _currentActivities = (Hashtable)reader.ReadObject();

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_clientID);
            writer.WriteObject(_address);
            writer.WriteObject(_activities);
            writer.WriteObject(_currentActivities);

        } 
        #endregion
    }
}