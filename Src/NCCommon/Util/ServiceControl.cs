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
using System.ServiceProcess;

namespace Alachisoft.NCache.Common.Util
{
    /// <summary>
    /// Represents the Win32 NCache service and allows you to connect to a running 
    /// or stopped service, manipulate it, or get information about it.
    /// </summary>
    public class ServiceControl : IDisposable
    {
        /// <summary> Win32 service controller. </summary>
        private ServiceController sc;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machine">machine name</param>
        public ServiceControl(string machine):this(machine,"NCacheSvc")
        {
        }

        public ServiceControl(string machine,string service)
        {
            sc = new ServiceController(service, machine);
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
            if (sc != null)
            {
                sc.Dispose();
                sc = null;
            }
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

        /// <summary>
        /// returs true if the service is known to be running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Paused:
                    case ServiceControllerStatus.PausePending:
                    case ServiceControllerStatus.Stopped:
                    case ServiceControllerStatus.StopPending:
                    case ServiceControllerStatus.StartPending:
                    case ServiceControllerStatus.ContinuePending:
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Waits for the service to reach the specified status.
        /// </summary>
        /// <param name="status">The status to wait for.</param>
        /// <param name="timeout">The amount of time to wait for 
        /// the service to reach the specified status.</param>
        public void WaitForStatus(ServiceControllerStatus status, TimeSpan timeout)
        {
            sc.WaitForStatus(status, timeout);
        }

        /// <summary>
        /// Waits for the service to reach the status of Started.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for 
        /// the service to reach the specified status.</param>
        public void WaitForStart(TimeSpan timeout)
        {
            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return;
                case ServiceControllerStatus.PausePending:
                    sc.WaitForStatus(ServiceControllerStatus.Paused, timeout);
                    goto case ServiceControllerStatus.Paused;
                case ServiceControllerStatus.Paused:
                    sc.Continue();
                    break;

                case ServiceControllerStatus.StopPending:
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    goto case ServiceControllerStatus.Stopped;
                case ServiceControllerStatus.Stopped:
                    sc.Start();
                    break;
            }
            sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
        }

        /// <summary>
        /// Waits for the service to reach the status of Stopped.
        /// </summary>
        /// <param name="timeout">The amount of time to wait for 
        /// the service to reach the specified status.</param>
        public void WaitForStop(TimeSpan timeout)
        {
            switch (sc.Status)
            {
                case ServiceControllerStatus.StartPending:
                    sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                    goto case ServiceControllerStatus.Running;

                case ServiceControllerStatus.PausePending:
                    sc.WaitForStatus(ServiceControllerStatus.Paused, timeout);
                    goto case ServiceControllerStatus.Running;

                case ServiceControllerStatus.Paused:
                case ServiceControllerStatus.Running:
                    sc.Stop();
                    break;
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
    }
}
