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
using System.Reflection;
using System.Runtime.Remoting;
using Alachisoft.NCache.Common.Remoting;

namespace Alachisoft.NCache.Management
{
    [CLSCompliant(false)]
    public class HostBase :IDisposable
    {
        protected MarshalByRefObject _remoteableObject;
        protected RemotingChannels _channel = new RemotingChannels();

        protected static string _appName = "NCacheExpress";

        protected string _url;

        /// <summary>
        /// Overloaded constructor.
        /// </summary>
        /// <param name="application"></param>
        static HostBase()
        {
            try
            {
                RemotingConfiguration.ApplicationName = _appName;
            }
            catch (Exception) { }
        }

        public HostBase(MarshalByRefObject remoteableObject,string url)
        {
            _remoteableObject = remoteableObject;
            _url = url;
        }
        /// <summary> </summary>
        public RemotingChannels Channels
        {
            get { return _channel; }
        }

        /// <summary> Returns the application name of this session. </summary>
        static public string ApplicationName
        {
            get { return _appName; }
        }

        protected virtual int GetHttpPort() { return 0; }
        protected virtual int GetTcpPort() { return 0; }
      

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void StartHosting(string tcpChannel, string httpChannel)
        {
            StartHosting(tcpChannel,
                GetTcpPort(),
                httpChannel,
                GetHttpPort());
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void StartHosting(string tcpChannel, string httpChannel, string ip)
        {
            StartHosting(tcpChannel,
                GetTcpPort(),
                httpChannel,
                GetHttpPort(), ip);
        }
        public void StartHosting(string tcpChannel, string ip, int port)
        {
            StartHosting(tcpChannel,
                port, ip);
        }
        public void StartHosting(string tcpChannel, int tcpPort, string ip)
        {
            _channel.RegisterTcpChannels(tcpChannel, ip, tcpPort);
          
            //muds:
            //assign the bindtoclusterip to cacheserver.clusterip
            if (_remoteableObject is CacheServer)
                ((CacheServer)_remoteableObject).ClusterIP = ip;

            RemotingServices.Marshal(_remoteableObject, _url);



        }
        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void StartHosting(string tcpChannel, int tcpPort, string httpChannel, int httpPort, string ip)
        {
            try
            {
                
                _channel.RegisterTcpChannels(tcpChannel, ip, tcpPort);
                _channel.RegisterHttpChannels(httpChannel, ip, httpPort);

                Assembly remoting =
                    Assembly.GetAssembly(typeof(System.Runtime.Remoting.RemotingConfiguration));
              

            
                //muds:
                //assign the bindtoclusterip to cacheserver.clusterip
                if (_remoteableObject is CacheServer)
                    ((CacheServer)_remoteableObject).ClusterIP   = ip;

                RemotingServices.Marshal(_remoteableObject, _url);
                
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;


            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void StartHosting(string tcpChannel, int tcpPort, string httpChannel, int httpPort)
        {
            try
            {
                
                _channel.RegisterTcpChannels(tcpChannel, tcpPort);
                _channel.RegisterHttpChannels(httpChannel, httpPort);             

                RemotingServices.Marshal(_remoteableObject, _url);
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        private void StartHosting(string tcpChannel, int tcpPort, string httpChannel, int httpPort, int sendBuffer, int receiveBuffer)
        {
            try
            {
              

                _channel.RegisterTcpChannels(tcpChannel, tcpPort);
                _channel.RegisterHttpChannels(httpChannel, httpPort);
             
                RemotingServices.Marshal(_remoteableObject, CacheServer.ObjectUri);
                RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;


            }
            catch (Exception)
            {
                throw;
            }
        }



        /// <summary>
        /// Stop this service. 
        /// </summary>
        public void StopHosting()
        {
            try
            {
               

                if (_remoteableObject != null)
                {
                    RemotingServices.Disconnect(_remoteableObject);
                    _channel.UnregisterTcpChannels();
                    _channel.UnregisterHttpChannels();


                    ((IDisposable)_remoteableObject).Dispose();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        
        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>
        /// </remarks>
        private void Dispose(bool disposing)
        {
            if (_remoteableObject != null)
            {
                ((IDisposable)_remoteableObject).Dispose();
                _remoteableObject = null;
                if (disposing) GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        
    }
}
