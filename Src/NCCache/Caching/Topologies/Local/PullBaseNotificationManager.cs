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
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Events;
using Alachisoft.NCache.Caching.Events;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class PullBaseNotificationManager : IDisposable
    {
        private HashVector<string, ClientNotificationMgr> _clients;
        private HashVector<string, KeyInfo> _updatedKeys;
        private HashVector<string, KeyInfo> _removedKeys;
        private HashVector<string, KeyInfo> _registeredKeys;
        private Thread _pollRequestThread;
        private int _pollRequestInterval;
        private int _deadClientInterval;
        private bool _isDisposing = false;

        private ICacheEventsListener _listner;
        private ILogger _logger;

        private static HPTime _lastChangeTime = HPTime.Now;

        public static HPTime LastChangeTime
        {
            get { return _lastChangeTime; }
            set { _lastChangeTime = value; }
        }

        private string _clearKey = "$$CacheClear$$";

        public PullBaseNotificationManager(ICacheEventsListener listner, ILogger logger)
        {
            _pollRequestInterval = ServiceConfiguration.NotificationEventInterval * 1000;    //convert in miliseconds
            _deadClientInterval = 30 * 60 * 1000;  //30Minutes
            _clients = new HashVector<string, ClientNotificationMgr>();
            _updatedKeys = new HashVector<string, KeyInfo>();
            _removedKeys = new HashVector<string, KeyInfo>();
            _registeredKeys = new HashVector<string, KeyInfo>();
            _listner = listner;

            _pollRequestThread = new Thread(SendPollRequestTask);
            _pollRequestThread.Name = "PullRequestThread";
            _pollRequestThread.IsBackground = true;
            _pollRequestThread.Start();

            _logger = logger;
        }

        public void RegisterKeyNotification(string clientId, string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (this)
            {
                if (!_clients.ContainsKey(clientId))
                {
                    _clients.Add(clientId, new ClientNotificationMgr(clientId, ref _updatedKeys, ref _removedKeys));
                }

                if (_clients[clientId].AddKey(key, updateCallback, removeCallback))
                {
                    if (_registeredKeys.ContainsKey(key))
                    {
                        _registeredKeys[key].IncrementRefCount();
                    }
                    else
                    {
                        _registeredKeys.Add(key, new KeyInfo());
                    }
                }
            }
        }

        public void UnRegisterKeyNotification(string clientId, string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            if (clientId == null)
                return;

            lock (this)
            {
                if (!_clients.ContainsKey(clientId))
                    return;

                if (_clients[clientId].RemoveKey(key))
                {
                    if (_registeredKeys.ContainsKey(key))
                    {
                        if (_registeredKeys[key].DecrementRefCount() <= 0)
                        {
                            _registeredKeys.Remove(key);

                            if (_updatedKeys.ContainsKey(key))
                            {
                                _updatedKeys.Remove(key);
                            }
                            if (_removedKeys.ContainsKey(key))
                            {
                                _removedKeys.Remove(key);
                            }
                        }
                    }
                }
            }
        }

        public void UnRegisterKeyNotification(string key, CallbackInfo updateCallback, CallbackInfo removeCallback)
        {
            lock (this)
            {
                IDictionaryEnumerator enumerator = _clients.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ((ClientNotificationMgr)enumerator.Value).RemoveKey(key);
                }
            }
        }


        public void KeyUpdated(string key, bool isUseroperation, string clientId)
        {
            lock (this)
            {

                KeyInfo keyInfo;
                if (_registeredKeys.TryGetValue(key, out keyInfo))
                {
                    _lastChangeTime = HPTime.Now; // set last change time
                    

                     keyInfo.UpdatedTime = HPTime.Now;
                     keyInfo = keyInfo.Clone() as KeyInfo;
                     keyInfo.UpdatedBy = clientId;

                    _updatedKeys[key] = keyInfo;
                }
            }
        }

        public void KeyRemoved(string key, bool isUserOperation, string clientId)
        {
            lock (this)
            {
                if (!isUserOperation)
                {
                    UnRegisterKeyNotification(key, null, null);
                    return;
                }
                if (_registeredKeys.ContainsKey(key))
                {
                    _lastChangeTime = HPTime.Now; // set last change time

                    KeyInfo keyInfo = _registeredKeys[key];
                    keyInfo.UpdatedTime = HPTime.Now;
                    keyInfo = keyInfo.Clone() as KeyInfo;
                    keyInfo.UpdatedBy = clientId;

                    if (!_removedKeys.ContainsKey(key))
                    {
                        _removedKeys.Add(key, keyInfo);
                    }
                    else
                    {
                        _removedKeys[key] = keyInfo;
                    }

                    if (_updatedKeys.ContainsKey(key))
                        _updatedKeys.Remove(key);
                }
            }
        }

        public void ClearCache(bool raisenotification)
        {
            lock (this)
            {
                foreach (KeyValuePair<string, ClientNotificationMgr> kvp in _clients)
                {
                    kvp.Value.ClearKeys();
                }
                _registeredKeys.Clear();
                _removedKeys.Clear();
                _updatedKeys.Clear();
            }

            if (raisenotification)
            {
                lock (this)
                {
                    IDictionaryEnumerator ide = _clients.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        _lastChangeTime = HPTime.Now;
                        ((ClientNotificationMgr)ide.Value).AddKey(_clearKey, null, null);

                        if (!_registeredKeys.ContainsKey(_clearKey))
                            _registeredKeys.Add(_clearKey, new KeyInfo());

                        KeyInfo keyInfo = _registeredKeys[_clearKey];

                        keyInfo.UpdatedTime = HPTime.Now;
                        keyInfo = keyInfo.Clone() as KeyInfo;
                        keyInfo.UpdatedBy = ide.Key as string;

                        if (!_updatedKeys.ContainsKey(_clearKey))
                        {
                            _updatedKeys.Add(_clearKey, keyInfo);
                        }
                        else
                        {
                            _updatedKeys[_clearKey] = keyInfo;
                        }
                    }
                }
            }
        }

        public void ClearClientKeys(string clientId)
        {
            lock (this)
            {
                if (!_clients.ContainsKey(clientId))
                    return;

                foreach (KeyValuePair<string, NotificationEntry> kvp in _clients[clientId].InterestedKeys)
                {
                    KeyInfo kInfo = _registeredKeys[kvp.Key];
                    if (kInfo != null)
                    {
                        if (kInfo.DecrementRefCount() <= 0)
                            _registeredKeys.Remove(kvp.Key);
                    }
                }
                _clients[clientId].ClearKeys();
            }
        }

        public void RegisterPollNotification(short callbackId, OperationContext context)
        {
            string clientId = context.GetValueByField(OperationContextFieldName.ClientId) as string;

            lock (this)
            {
                if (clientId != null && _clients.ContainsKey(clientId))
                {
                    _clients[clientId].RegisterPollNotification(callbackId);
                }
            }
        }

        public void RemoveClient(string clientId)
        {
            lock (this)
            {
                if (!_clients.ContainsKey(clientId))
                    return;

                ClearClientKeys(clientId);
                _clients.Remove(clientId);
            }
        }

        public PollingResult Poll(string clientId)
        {
            lock (this)
            {
                if (!_clients.ContainsKey(clientId))
                    return new PollingResult();
                
                PollingResult result = _clients[clientId].GetChangedKeys(_logger);
                if (result.UpdatedKeys.Contains(_clearKey))
                    _registeredKeys.Remove(_clearKey); // remove if clearkey is going down to client.

                return result;
            }
        }
        
        #region SendEventsToClient
        private void SendPollRequestTask()
        {
            try
            {
                // Waiting 30 minutes to declare a client dead.
                // No of notifications to be sent in 30 minutes
                float deadClientTest = (float)_deadClientInterval / _pollRequestInterval;

                HPTime lastDeadClientCheckTime = HPTime.Now;
                bool looped = false;
                while (!_isDisposing)
                {
                    try
                    {
                        for (float i = 0; i < deadClientTest; i++)
                        {
                            looped = true;
                            if (_isDisposing)
                                break;
                            SendPollRequest();
                            lock (this)
                            {
                                Monitor.Wait(this, _pollRequestInterval);
                            }
                        }

                        if(looped)
                            CleanDeadClients(lastDeadClientCheckTime);

                        lastDeadClientCheckTime = HPTime.Now;
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (ThreadInterruptedException) { }
            catch (Exception e)
            {
                NCacheLog.Error("PullBasedNotificationManager.SendPollRequest", e.ToString());
            }
        }

        private void SendPollRequest()
        {
            if (_listner != null)
            {
                lock (this)
                {
                    IDictionaryEnumerator enumerator = _clients.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ClientNotificationMgr clientNotifMgr = enumerator.Value as ClientNotificationMgr;
                        if (clientNotifMgr != null && clientNotifMgr.AreKeysModified())
                        {
                            _listner.OnPollNotify(enumerator.Key as string, clientNotifMgr.CallbackId, EventTypeInternal.ClientCache);
                        }
                    }
                }
            }
        }

        private void CleanDeadClients(HPTime lastDeadClientTimeCheck)
        {
            try
            {
                lock (this)
                {
                    List<string> deadClients = new List<string>();
                    foreach (KeyValuePair<string, ClientNotificationMgr> kvp in _clients)
                    {
                        ClientNotificationMgr client = kvp.Value;
                        if (client.LastPollTime.CompareTo(lastDeadClientTimeCheck) < 0)
                        {
                            deadClients.Add(kvp.Key);
                            RemoveClient(kvp.Key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NCacheLog.Error("PullBaseNotificationManager.CleanDeadClients()", " Exception: " + ex);
            }
        }
        #endregion

        public void Dispose()
        {
            _isDisposing = true;
            lock (this)
                Monitor.Pulse(this); // awake the poll
            try
            {
                if (_pollRequestThread != null)
#if !NETCORE
                    _pollRequestThread.Abort();
#elif NETCORE
                    _pollRequestThread.Interrupt();
#endif

                _pollRequestThread = null; // dispose the thread.
            }
            catch (Exception) { }
        }
    }
}
