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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer;
using Alachisoft.NCache.Integrations.Memcached.ProxyServer.Common;
using Alachisoft.NCache.Integrations.Memcached.Provider;

namespace Alachisoft.NCache.Memcached
{
    public partial class MemcachedProxyServiceForNCache : ServiceBase
    {
        private TcpNetworkGateway _tcpTextGateWay;
        private TcpNetworkGateway _tcpBinaryGateWay;

        public MemcachedProxyServiceForNCache()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                string textProtocolIP = MemConfiguration.TextProtocolIP;
                int textPort = MemConfiguration.TextProtocolPort;
                string binaryProtocolIP = MemConfiguration.BinaryProtocolIP;
                int binaryPort = MemConfiguration.BinaryProtocolPort;

                if (textPort == binaryPort)
                {
                    throw new ArgumentException("Ports cannot be same for text and binary protocol.");
                }

                _tcpTextGateWay = new TcpNetworkGateway(textProtocolIP, textPort, Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway.ProtocolType.Text);
                _tcpTextGateWay.StartListenForClients();
                LogManager.Logger.Info("Proxy server for text protocol started at IP: " + textProtocolIP + " port: " + textPort);


                _tcpBinaryGateWay = new TcpNetworkGateway(binaryProtocolIP, binaryPort, Alachisoft.NCache.Integrations.Memcached.ProxyServer.NetworkGateway.ProtocolType.Binary);
                _tcpBinaryGateWay.StartListenForClients();
                LogManager.Logger.Info("Proxy server for binary protocol started at IP: " + binaryProtocolIP + " port: " + binaryPort);
            }
            catch (Exception e)
            {
                LogManager.Logger.Fatal("Service", "\t Failed to start proxy server. Exception : " + e);
                throw;
            }
        }

        protected override void OnStop()
        {
            _tcpTextGateWay.StopListenForClients();
            _tcpBinaryGateWay.StopListenForClients();
            CacheFactory.DisposeCacheProvider();
        }
    }
}
