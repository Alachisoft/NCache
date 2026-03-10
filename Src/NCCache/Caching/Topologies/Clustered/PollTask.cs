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