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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class ClientNotificationMgr
    {
        private HashVector<string, NotificationEntry> _interestedKeys; 
        private HPTime _lastPollTime;
        private HashVector<string, KeyInfo> _updatedKeys;    
        private HashVector<string, KeyInfo> _removedKeys;    
        private short _callbackId;
        private string _clientId;
        private string _clearKey = "$$CacheClear$$";

        public short CallbackId { get { return _callbackId; } }

        public HashVector<string, NotificationEntry> InterestedKeys { get { return _interestedKeys; } }

        public HPTime LastPollTime { get { return _lastPollTime; } }

        public ClientNotificationMgr(string clientId, ref HashVector<string, KeyInfo> updatedKeys, ref HashVector<string, KeyInfo> removedKeys)
        {
            _updatedKeys = updatedKeys;
            _removedKeys = removedKeys;
            _interestedKeys = new HashVector<string, NotificationEntry>();
            _lastPollTime = HPTime.Now;
            _clientId = clientId;
        }

        public void RegisterPollNotification(short callbackId)
        {
            _callbackId = callbackId;
        }

        public bool AddKey(string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (_interestedKeys)
            {
                if (!_interestedKeys.ContainsKey(key))
                {
                    _interestedKeys.Add(key, new NotificationEntry(updateCallback, removeCallback) { RegistrationTime = HPTime.Now });
                    return true;
                }
                else
                {
                    NotificationEntry entry = _interestedKeys[key];
                    entry.SetNotifications(updateCallback, removeCallback);
                    _interestedKeys[key] = entry;
                }
            }

            return false;
        }

        public bool RemoveKey(string key)
        {
            lock (_interestedKeys)
            {
                if (_interestedKeys.ContainsKey(key))
                {
                    _interestedKeys.Remove(key);
                    return true;
                }
            }
            return false;
        }

        public void ClearKeys()
        {
            lock (_interestedKeys)
            {
                foreach (KeyValuePair<string, NotificationEntry> entry in _interestedKeys)
                {
                    if (entry.Value.NotifyOnUpdate)
                        if (_updatedKeys.ContainsKey(entry.Key))
                            DecrementOrRemove(ref _updatedKeys, entry.Key);

                    if (entry.Value.NotifyOnRemove)
                        if (_removedKeys.ContainsKey(entry.Key))
                            DecrementOrRemove(ref _removedKeys, entry.Key);
                }

                _interestedKeys.Clear();
            }
        }

        public PollingResult GetChangedKeys(ILogger logger)
        {
            PollingResult result = new PollingResult();
            HPTime newPollTime = HPTime.Now;
            lock (_interestedKeys)
            {
                IDictionaryEnumerator enumerator = _interestedKeys.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Key as string;
                    
                    // add to result if clearkey exists in interest
                    if (key.Equals(_clearKey))
                    {
                        result.UpdatedKeys.Add(_clearKey);
                        DecrementOrRemove(ref _updatedKeys, _clearKey);
                    }

                    NotificationEntry enntry = (NotificationEntry)enumerator.Value;
                    if (enntry.NotifyOnUpdate)
                    {
                        if (_updatedKeys.ContainsKey(key))
                        {
                            //change must be detected if a client had registered prior to change occured
                            if (_updatedKeys[key].UpdatedTime.CompareTo(enntry.RegistrationTime) > 0 &&
                                _updatedKeys[key].UpdatedTime.CompareTo(_lastPollTime) >= 0)
                            {

                                if (_updatedKeys[key].UpdatedBy != null && string.Compare(_updatedKeys[key].UpdatedBy, _clientId, true) != 0)
                                    result.UpdatedKeys.Add(key);
                                else if (_updatedKeys[key].UpdatedBy == null)
                                    result.UpdatedKeys.Add(key);

                                DecrementOrRemove(ref _updatedKeys, key);
                            }
                        }
                    }
                    if (enntry.NotifyOnRemove)
                    {
                        if (_removedKeys.ContainsKey(key))
                        {
                            //change must be detected if a client had registered prior to change occured
                            if (_removedKeys[key].UpdatedTime.CompareTo(enntry.RegistrationTime) > 0 &&
                                _removedKeys[key].UpdatedTime.CompareTo(_lastPollTime) >= 0)
                            {
                                if (_removedKeys[key].UpdatedBy != null && string.Compare(_removedKeys[key].UpdatedBy, _clientId, true) != 0)
                                    result.RemovedKeys.Add(key);
                                else if (_removedKeys[key].UpdatedBy == null)
                                    result.RemovedKeys.Add(key);
                                DecrementOrRemove(ref _removedKeys, key);
                            }
                        }
                    }

                }

                _lastPollTime = newPollTime;
                //Remove keys that are deleted from interestedKeys here
                foreach (string key in result.RemovedKeys)
                {
                    _interestedKeys.Remove(key);
                }

                if (_interestedKeys.ContainsKey(_clearKey))
                    _interestedKeys.Remove(_clearKey);
            }
            return result;
        }

        public bool AreKeysModified()
        {
            lock (_interestedKeys)
            {
                if (PullBaseNotificationManager.LastChangeTime.CompareTo(_lastPollTime) < 0)
                    return false;

                IDictionaryEnumerator enumerator = _interestedKeys.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = enumerator.Key as string;

                    // return true if clearkey exists in interest
                    if (key.Equals(_clearKey))
                    {
                        return true;
                    }

                    if (((NotificationEntry)enumerator.Value).NotifyOnUpdate)
                    {
                        if (_updatedKeys.ContainsKey(key))
                        {
                            if (_updatedKeys[key].UpdatedTime.CompareTo(((NotificationEntry)enumerator.Value).RegistrationTime) > 0 &&
                                _updatedKeys[key].UpdatedTime.CompareTo(_lastPollTime) >= 0)
                            {
                                if (_updatedKeys[key].UpdatedBy != null && string.Compare(_updatedKeys[key].UpdatedBy, _clientId, true) != 0)
                                    return true;
                                else if (_updatedKeys[key].UpdatedBy == null)
                                    return true;

                                return false;
                            }
                        }
                    }
                    if (((NotificationEntry)enumerator.Value).NotifyOnRemove)
                    {
                        if (_removedKeys.ContainsKey(key))
                        {
                            if (_removedKeys[key].UpdatedTime.CompareTo(((NotificationEntry)enumerator.Value).RegistrationTime) > 0 &&
                                _removedKeys[key].UpdatedTime.CompareTo(_lastPollTime) >= 0)
                            {
                                if (_removedKeys[key].UpdatedBy != null && string.Compare(_removedKeys[key].UpdatedBy, _clientId, true) != 0)
                                    return true;
                                else if (_removedKeys[key].UpdatedBy == null)
                                    return true;

                                return false;
                            }
                        }
                    }

                }
            }
            return false;
        }

        //assumption contains checks are outside this function
        private void DecrementOrRemove(ref HashVector<string, KeyInfo> keyVector, string key)
        {
            lock (keyVector)
            {
                if (keyVector.ContainsKey(key))
                {
                    KeyInfo kInfo = keyVector[key];

                    if (kInfo.DecrementRefCount() <= 0)
                        keyVector.Remove(key);
                }
            }
        }
    }
}