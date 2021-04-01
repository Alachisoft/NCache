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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Alachisoft.NCache.Management
{
    public class CachePortsConfigManger
    {
        private CachePortsConfig _cachePortsConfig;
        Hashtable _cachePortsTable;
        List<string> _configuredCaches;
        object _lock = new object();

        public List<string> ConfiguredCaches
        {
            get { return _configuredCaches; }
            set { _configuredCaches = value; }
        }

        public Hashtable CachePortTable
        {
            get { return _cachePortsTable; }
        }

        private ArrayList GetOccupiedPorts()
        {
            return new ArrayList(_cachePortsTable.Values);
        }

        public CachePortsConfigManger()
        {
            _cachePortsConfig = new CachePortsConfig();
            _cachePortsTable = new Hashtable();
            _configuredCaches = new List<string>();
        }

        public void Initialize()
        {
            try
            {
                _cachePortsTable = _cachePortsConfig.ReadConfiguration();
              
            }
            catch (Exception ex)
            {

            }

        }

        public void AssignRunningPorts (string cacheId, int managementPort)
        {
            try
            {
                lock (_lock)
                {
                    if (!_cachePortsTable.ContainsKey(cacheId.ToLower()))
                        _cachePortsTable.Add(cacheId, managementPort); // update port
                    else
                        _cachePortsTable[cacheId.ToLower()] = managementPort;
                    _cachePortsConfig.AddConfiguration(cacheId.ToLower(), managementPort);

                }
            }
            catch
            {

            }
        }
        public void SynchronizeTable ()
        {
            try
            {
                if (_cachePortsTable.Count <= 0)
                {
                    this.AssignPortsIfNotExist();
                    _cachePortsTable = _cachePortsConfig.ReadConfiguration();
                }
                this.SynchronizePortsConfiguration(_configuredCaches);
                _cachePortsConfig.InitializeConfigTableAtStart(_cachePortsTable);
            }
            catch
            {

            }
        }
      
        private void AssignPortsIfNotExist()
        {
            try
            {
                if (this.ConfiguredCaches == null || this.ConfiguredCaches.Count <= 0)
                    return;
                else
                {
                    lock (_lock)
                    {
                        Hashtable cachePorts = new Hashtable();
                        ArrayList occupiedPorts = new ArrayList();
                        foreach (string cache in this.ConfiguredCaches)
                        {
                            if (!cachePorts.Contains(cache.ToLower()))
                            {
                                int managementPort = ManagementPortHandler.GenerateManagementPort(occupiedPorts);
                                cachePorts.Add(cache, managementPort);
                                occupiedPorts.Add(managementPort);
                            }
                        }
                        _cachePortsConfig.WriteConfiguration(cachePorts);
                    }
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while assigning ports : " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        private void SynchronizePortsConfiguration (List<string> configuredCaches)
        {
            try
            {
                ArrayList occupiedPorts = this.GetOccupiedPorts();
                lock (_lock)
                {
                    foreach (string cacheId in configuredCaches)
                    {
                        if (!_cachePortsTable.Contains(cacheId.ToLower()))
                        {
                            int managementPort = ManagementPortHandler.GenerateManagementPort(occupiedPorts);
                            occupiedPorts.Add(managementPort);
                            _cachePortsConfig.AddConfiguration(cacheId, managementPort);
                            _cachePortsTable.Add(cacheId.ToLower(), managementPort);

                        }
                    }
                    IDictionaryEnumerator enm = _cachePortsTable.GetEnumerator();
                    List<string> toRemove = new List<string>();
                    while (enm.MoveNext())
                    {
                        if (!configuredCaches.Contains(enm.Key.ToString()))
                        {
                            toRemove.Add(enm.Key.ToString().ToLower());
                        }
                    }
                    if (toRemove.Count > 0)
                    {
                        foreach (string cache in toRemove)
                        {
                            _cachePortsConfig.RemoveConfiguration(cache);
                            _cachePortsTable.Remove(cache);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while synchronizing ports configuration : " + ex.ToString(), EventLogEntryType.Error);
            }

        }

        public int RegisterCachePort (string cacheId)
        {
            int managementPort = 0;
            try
            {
                lock (_lock)
                {
                    if (!_cachePortsTable.Contains(cacheId.ToLower()))
                    {
                        managementPort = ManagementPortHandler.GenerateManagementPort(this.GetOccupiedPorts());
                        _cachePortsConfig.AddConfiguration(cacheId, managementPort);
                       
                    }
                }
              
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while registering new port : " + ex.ToString(), EventLogEntryType.Error);
            }
            return managementPort;
        }

        public void UnRegisterCachePort (string cacheId)
        {
            try
            {
                lock (_lock)
                {
                    if (_cachePortsTable.Contains(cacheId.ToLower()))
                    {
                        _cachePortsConfig.RemoveConfiguration(cacheId.ToLower());
                        _cachePortsTable.Remove(cacheId.ToLower());
                    }
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while unregistering ports : " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        public int GetCachePort(string cacheId)
        {
            if (!string.IsNullOrEmpty(cacheId))
            {
                if (!_cachePortsTable.Contains(cacheId.ToLower()))
                    return this.RegisterCachePort(cacheId.ToLower());
                else
                    return (int) _cachePortsTable[cacheId.ToLower()];
            }
            return 0;

        }

        public void Dispose ()
        {
            try
            {
                _configuredCaches = null;
                _cachePortsTable = null;
            }
            catch
            {

            }
        }

    }
}
