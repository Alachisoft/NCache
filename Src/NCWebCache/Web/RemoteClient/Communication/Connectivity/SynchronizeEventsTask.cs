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

using Alachisoft.NCache.Common.Threading;
using Exception = System.Exception;

namespace Alachisoft.NCache.Web.Communication
{
    sealed class SynchronizeEventsTask : AsyncProcessor.IAsyncTask
    {
        private Connection _connection;
        private Broker _parent;

        public SynchronizeEventsTask(Broker parent, Connection connection)
        {
            this._parent = parent;
            this._connection = connection;
        }

        #region IAsyncTask Members

        public void Process()
        {
            try
            {
                _parent.SynchronizeEvents(_connection);
            }
            catch (Exception e)
            {
                if (_parent.Logger.IsErrorLogsEnabled)
                    _parent.Logger.NCacheLog.Error("SynchronizeEventsTask.Process", e.ToString());
            }
        }

        #endregion
    }
}
