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
#if !NETCORE
using Alachisoft.NCache.Common.Remoting;
#endif
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Management;

namespace Alachisoft.NCache.ServiceControl
{
    /// <summary>
    /// Base class for services.
    /// </summary>
    [CLSCompliant(false)]
    public abstract class ServiceBase : IDisposable
    {
        /// <summary> Server name. </summary>
        protected string _serverName = Environment.MachineName;
        /// <summary> Use TCP channel for remoting. </summary>
        protected bool _useTcp = true;
        /// <summary> Remoting port. </summary>
        protected long _port;

#if !NETCORE
        /// <summary> </summary>
        protected RemotingChannels _channel;
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        public ServiceBase()
        { }

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="server">name of machine where the service is running.</param>
        /// <param name="port">port used by the remote server.</param>
        /// <param name="useTcp">use tcp channel for remoting.</param>
        public ServiceBase(string server, long port, bool useTcp)
        {
            ServerName = server;
            Port = port;
            UseTcp = useTcp;
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
            if (disposing) GC.SuppressFinalize(this);
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

        /// <summary> Server name. </summary>
        public string ServerName
        {
            get { return _serverName; }
            set { _serverName = value; }
        }

        /// <summary> Use TCP channel for remoting. </summary>
        public bool UseTcp
        {
            get { return _useTcp; }
            set { _useTcp = value; }
        }

        /// <summary> Remoting port. </summary>
        public long Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// Starts the service on target machine.
        /// </summary>
        protected virtual void Start(TimeSpan timeout,string service)
        {
            try
            {
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    throw new Exception("Failed to start service. Please try the operation manually.");
#endif
                using (Common.Util.ServiceControl sc = new Common.Util.ServiceControl(_serverName, service))
                {
                    if (!sc.IsRunning)
                    {
                        sc.WaitForStart(timeout);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        /// <summary>
        /// Starts the stop service on target machine.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="service"></param>
        protected virtual void Stop(TimeSpan timeout,string service)
        {
            try
            {
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    throw new Exception("Failed to stop service. Please try the operation manually.");
#endif
                using (Common.Util.ServiceControl sc = new Common.Util.ServiceControl(_serverName, service))
                {
                    if (sc.IsRunning)
                    {
                        sc.WaitForStop(timeout);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }
       
        /// <summary>
        /// Starts the restart service on target machine.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="service"></param>
        protected virtual void Restart(TimeSpan timeout, string service)
        {
            try
            {
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    throw new Exception("Failed to restart service. Please try the operation manually.");
#endif
                using (Common.Util.ServiceControl sc = new Common.Util.ServiceControl(_serverName, service))
                {
                    if (sc.IsRunning)
                    {
                        sc.WaitForStop(timeout);
                        sc.WaitForStart(timeout);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        protected virtual bool isServiceRunning(TimeSpan timeout, string service)
        {
            bool isRunning = false;
            try
            {
                using (Common.Util.ServiceControl sc = new Common.Util.ServiceControl(_serverName, service))
                {
                    if (sc.IsRunning)
                    {
                        isRunning= true;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
            return isRunning;
        }
    }
}