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
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common
{
    public class LogManager
    {
        static LogManager()
        {
            _logger = new NCacheLogger();
            _logger.Initialize(LoggerNames.MemcacheGateway);
            string loggingLevel=System.Configuration.ConfigurationManager.AppSettings["LoggingLevel"];
            if (!string.IsNullOrEmpty(loggingLevel))
                _logger.SetLevel(loggingLevel);
            else
                _logger.SetLevel("Info");
        }

        public LogManager(string clientName)
        {
            _clientName = clientName;
        }

        private static ILogger _logger;
        public static ILogger Logger
        {
            get { return _logger; }
            set { _logger = value; }
        }

        private string _clientName = null;
        public string ClientName
        {
            get { return _clientName; }
            set { _clientName = value; }
        }


        public void Error(string message)
        {
            if(_logger.IsErrorEnabled)
                _logger.Error("Client : "+_clientName + message);
        }

        public void Error(string module, string message)
        {
            if(_logger.IsErrorEnabled)
                _logger.Error(module, "Client : " + _clientName + message);
        }

        public void Fatal(string message)
        {
            if (_logger.IsFatalEnabled)
                _logger.Fatal("Client : " + _clientName + message);
        }

        public void Fatal(string module, string message)
        {
            if (_logger.IsFatalEnabled)
                _logger.Fatal(module, "Client : " + _clientName + message);
        }

        public void Warn(string message)
        {
            if (_logger.IsWarnEnabled)
                _logger.Warn("Client : " + _clientName + message);
        }

        public void Warn(string module, string message)
        {
            if (_logger.IsWarnEnabled)
                _logger.Warn(module, "Client : " + _clientName + message);
        }

        public void Info(string message)
        {
            if (_logger.IsInfoEnabled)
                _logger.Info("Client : " + _clientName + message);
        }

        public void Info(string module, string message)
        {
            if (_logger.IsInfoEnabled)
                _logger.Info(module, "Client : " + _clientName + message);
        }

        public void Debug(string message)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug("Client : " + _clientName + message);
        }

        public void Debug(string module, string message)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug(module, "Client : " + _clientName + message);
        }
    }
}
