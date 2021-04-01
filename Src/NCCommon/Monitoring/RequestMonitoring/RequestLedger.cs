using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class RequestLedger
    {
        private string _requestSource;
        ConcurrentDictionary<long, ICancellableRequest> _requests;
        private ILogger _logger;

        public RequestLedger(string requestSource,ILogger logger)
        {
            _requestSource = requestSource;
            _logger = logger;
            _requests = new ConcurrentDictionary<long, ICancellableRequest>();
        }

        internal void AddRequet(long requestId, ICancellableRequest command)
        {
            if (requestId> 0)
            {
                try
                {
                    _requests.TryAdd(requestId, command);
                }
                catch
                {

                }
            }
        }

        public ICancellableRequest RemoveRequest (long requestId)
        {
            ICancellableRequest command =null;
            if (requestId>0)
            {
                try
                {
                    _requests.TryRemove(requestId, out command);
                }
                catch
                {

                }
            }

            return command;
        }

        public void CancelTimedoutRequests()
        {
             foreach (KeyValuePair<long, ICancellableRequest> ledgerValue in _requests)
            {
                ICancellableRequest command = ledgerValue.Value;
                long requestID = ledgerValue.Key;
                if (!command.IsCancelled)
                {
                    if(command.HasTimedout)
                    {
                        if (command.Cancel())
                            if (_logger != null)
                                _logger.CriticalInfo("Cache.CancelExecution()", "Command : " + command.ToString() + " Request ID : " + requestID + " has been cancelled for : " + _requestSource);
                    }
                }
            }
        }


        public void Dispoe ()
        {
            if (_requests != null)
            {
                _requests.Clear();
                _logger = null;
            }
        }

    }
}
