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
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// Asynchronous notification dispatcher.
    /// </summary>
    internal class PollTask : AsyncProcessor.IAsyncTask
    {
        /// <summary> The listener class </summary>
        private ClusterCacheBase _parent;

        private OperationContext _operationContext;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="data"></param>
        public PollTask(ClusterCacheBase parent, OperationContext operationContext)
        {
            _parent = parent;
            _operationContext = operationContext;
           
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>
        void AsyncProcessor.IAsyncTask.Process()
        {
            try
            {
                if (_parent != null)
                {
                    Function func = new Function((int)ClusterCacheBase.OpCodes.DryPoll, new object[] { _operationContext }, true);
                    _parent.RaiseGeneric(func);

                }
            }
            catch (Exception)
            {

            }

        }
    }
}