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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common
{
    public static class MemConfiguration
    {
        private static string _cacheName;
        private static string _textProtocolIP = "127.0.0.1";
        private static int _textProtocolPort = 11212;
        private static string _binaryProtocolIP = "127.0.0.1";
        private static int _binaryProtocolPort = 11213;
        private static int _maxCommandLength = 1024*1024;

        public static string CacheName
        {
            get { return _cacheName; }
        }

        public static int TextProtocolPort
        {
            get { return _textProtocolPort; }
        }

        public static string TextProtocolIP
        {
            get { return _textProtocolIP; }
        }

        public static int BinaryProtocolPort
        {
            get { return _binaryProtocolPort; }
        }

        public static string BinaryProtocolIP
        {
            get { return _binaryProtocolIP; }
        }

        public static int MaximumCommandLength
        {
            get { return _maxCommandLength; }
        }

        static MemConfiguration()
        {
            LoadConfiguration();
        }

        private static void LoadConfiguration()
        {
            try
            {
                _cacheName = System.Configuration.ConfigurationManager.AppSettings["CacheName"];
            }
            catch (Exception)
            {

            }
            if (string.IsNullOrEmpty(_cacheName))
                throw new ConfigurationErrorsException("CacheName cannot be null or empty in application configuraion.");

            try
            {
                _textProtocolPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["TextProtocolPort"]);
            }
            catch (Exception)
            {
                LogManager.Logger.Error("MemConfiguration.LoadConfiguration()", " Failed to parse port for text protocol. Using default value " + _textProtocolPort);
            }

            if (System.Configuration.ConfigurationManager.AppSettings["TextProtocolIP"] != null)
            {
                _textProtocolIP = System.Configuration.ConfigurationManager.AppSettings["TextProtocolIP"];
            }
            else
            {
                LogManager.Logger.Error("MemConfiguration.LoadConfiguration()", " TextProtocolIP not defined in application configuration. Using default value " + _textProtocolIP);
            }

            try
            {
                _binaryProtocolPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["BinaryProtocolPort"]);
            }
            catch (Exception)
            {
                LogManager.Logger.Error("MemConfiguration.LoadConfiguration()", " Failed to parse port for binary protocol. Using default value " + _binaryProtocolPort);
            }

            if (System.Configuration.ConfigurationManager.AppSettings["BinaryProtocolIP"] != null)
            {
                _binaryProtocolIP = System.Configuration.ConfigurationManager.AppSettings["BinaryProtocolIP"];
            }
            else
            {
                LogManager.Logger.Error("MemConfiguration.LoadConfiguration()", " BinaryProtocolIP not defined in application configuration. Using default value " + _binaryProtocolIP);
            }

            try
            {
                _maxCommandLength = 1024 * int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxCommandLength"]);
            }
            catch (Exception)
            {
                LogManager.Logger.Error("MemConfiguration.LoadConfiguration()", " Failed to parse maximum command length. Using default value " + _maxCommandLength / 1024 + " kb.");
            }
        }

    }
}
