// Copyright (c) 2018 Alachisoft
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

using System.Threading;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Runtime.Exceptions;
using Exception = System.Exception;

namespace Alachisoft.NCache.Web.Communication
{
    sealed class ReconnectTask : AsyncProcessor.IAsyncTask
    {
        private short _retries = 3;
        private Connection _connection;
        private Broker _parent;

        public ReconnectTask(Broker parent, Connection connection)
        {
            connection.IsReconnecting = true;
            this._parent = parent;
            this._connection = connection;
        }

        #region IAsyncTask Members

        public void Process()
        {
            try
            {
                if (this._connection == null) return;
                if (this._connection.IsConnected) return;
                while (_retries-- > 0)
                {
                    Thread.Sleep(2000); //waite for 2 seconds before retrying

                    try
                    {
                        Exception exception = null;

                        if (this._parent.TryConnecting(this._connection, ref exception))
                            break;

                        if (exception != null)
                        {
                            if (exception.Message.StartsWith("System.Exception: Cache is not registered"))
                            {
                                Connection connection = this._parent.TryPool();
                                if (connection != null && connection.IsConnected)
                                {
                                    this._parent.GetHashmap(connection);
                                    if (this._parent.Logger.IsErrorLogsEnabled)
                                    {
                                        this._parent.Logger.NCacheLog.Error("ReconnectTask.Process",
                                            "Connection [" + this._connection.IpAddress + "] Exception->" +
                                            exception.ToString());
                                    }
                                }

                                break;
                            }

                            if (exception.Message.StartsWith("System.Exception: Cache is not running") &&
                                _retries == 0) // then wait till the retries
                            {
                                Connection connection = this._parent.TryPool();
                                if (connection != null && connection.IsConnected)
                                {
                                    this._parent.GetHashmap(connection);
                                    if (this._parent.Logger.IsErrorLogsEnabled)
                                    {
                                        this._parent.Logger.NCacheLog.Error("ReconnectTask.Process",
                                            "Connection [" + this._connection.IpAddress + "] Exception->" +
                                            exception.ToString());
                                    }
                                }

                                break;
                            }

                            if (this._parent.Logger.IsErrorLogsEnabled)
                            {
                                this._parent.Logger.NCacheLog.Error("ReconnectTask.Process",
                                    "Connection [" + this._connection.IpAddress + "] Exception " +
                                    exception.ToString());
                            }
                        }
                    }
                    catch (OperationNotSupportedException ons)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        if (_parent.Logger.IsErrorLogsEnabled)
                            _parent.Logger.NCacheLog.Error("ReconnectTask.Process", e.ToString());
                        break;
                    }
                }
            }
            finally
            {
                if (_parent.Logger.NCacheLog != null) _parent.Logger.NCacheLog.Flush();
                if (this._connection != null) this._connection.IsReconnecting = false;
            }
        }

        #endregion
    }
}
