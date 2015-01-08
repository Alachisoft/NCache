// Copyright (c) 2015 Alachisoft
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
using System.Configuration;

namespace Alachisoft.NCache.Common
{
    public class ServiceConfiguration
    {
        private static string _port = "9800";
     
        private static string _sendBufferSize = "131072";
        private static string _receiveBufferSize = "131072";
        private static string _licenseLogging = "false";
        private static string _enableDualSocket = "false";
        private static string _enableNaggling = "false";
        private static string _nagglingSize = "500";
        private static string _enableDebuggingCounters = "false";
        private static string _expirationBulkRemoveSize = "10";
        private static string _expirationBulkRemoveDelay = "0";
        private static string _evictionBulkRemoveSize = "10";
        private static string _evictionBulkRemoveDelay = "0";
        private static string _bulkItemsToReplicated = "300";

        static ServiceConfiguration()
        {
            Load();
        }
        
        public static void Load()
        {
            try
            {
                System.Configuration.Configuration config =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                                
                if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"] != null)
                    _expirationBulkRemoveSize = config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"].Value;

                if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"] != null)
                    _expirationBulkRemoveDelay = config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"].Value;

                if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"] != null)
                    _evictionBulkRemoveSize = config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"].Value;

                if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"] != null)
                    _evictionBulkRemoveDelay = config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"].Value;
            }
            catch (Exception ex) { }
        }

        public static string Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public static string SendBufferSize
        {
            get { return _sendBufferSize; }
            set { _sendBufferSize = value; }
        }

        public static string ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            set { ReceiveBufferSize = value; }
        }

        public static string LicenseLogging
        {
            get { return _licenseLogging; }
            set { _licenseLogging = value; }
        }

     
        public static string EnableDualSocket
        {
            get { return _enableDualSocket; }
            set { _enableDualSocket = value; }
        }

        public static string EnableNaggling
        {
            get { return _enableNaggling; }
            set { _enableNaggling = value; }
        }

        public static string NagglingSize
        {
            get { return _nagglingSize; }
            set { _nagglingSize = value; }
        }

     
        public static string EnableDebuggingCounters
        {
            get { return _enableDebuggingCounters; }
            set { _enableDebuggingCounters = value; }
        }

        public static string ExpirationBulkRemoveSize
        {
            get { return _expirationBulkRemoveSize; }
            set { _expirationBulkRemoveSize = value; }
        }

        public static string ExpirationBulkRemoveDelay
        {
            get { return _expirationBulkRemoveDelay; }
            set { _expirationBulkRemoveDelay = value; }
        }

        public static string EvictionBulkRemoveSize
        {
            get { return _evictionBulkRemoveSize; }
            set { _evictionBulkRemoveSize = value; }
        }

        public static string EvictionBulkRemoveDelay
        {
            get { return _evictionBulkRemoveDelay; }
            set { _evictionBulkRemoveDelay = value; }
        }

        public static string BulkItemsToReplicated
        {
            get { return _bulkItemsToReplicated; }
            set { _bulkItemsToReplicated = value; }
        }
    }
}
