using System;
using System.Threading;

namespace Alachisoft.NGroups.Protocols.pbcast
{
    public class ViewPromise
    {
        /// <summary>The result of the request</summary>
        Object _result = null;
        /// <summary>Used to wait on the result</summary>
        Object _mutex = new Object();
        /// <summary>How many responses expected</summary>
        int _countExpected;
        /// <summary>How many responses received</summary>
        int _countReceived;

        public ViewPromise(int count)
        {
            this._countExpected = count;
        }

        /// <summary>
        /// If result was already submitted, returns it immediately, else blocks until
        /// results becomes available. 
        /// </summary>
        /// <param name="timeout">Maximum time to wait for result.</param>
        /// <returns>Promise result</returns>
        public Object WaitResult(long timeout)
        {
            Object ret = null;

            lock (_mutex)
            {
                if (_result != null && (_countExpected == _countReceived))
                {
                    ret = _result;
                    _result = null;
                    return ret;
                }
                if (timeout <= 0)
                {
                    try
                    {
                        Monitor.Wait(_mutex);
                    }
                    catch (Exception ex)
                    {
                        Trace.error("Promise.WaitResult", "exception=" + ex.ToString());
                    }
                }
                else
                {
                    try
                    {
                        Monitor.Wait(_mutex, (int)timeout, true);
                    }
                    catch (Exception ex)
                    {
                        Trace.error("Promise.WaitResult:", "exception=" + ex.ToString());
                    }
                }
                if (_result != null && (_countExpected == _countReceived))
                {
                    ret = _result;
                    _result = null;
                    return ret;
                }
                return null;
            }
        }

        /// <summary>
        /// Sets the result and notifies any threads waiting for it
        /// </summary>
        /// <param name="obj">Result of request</param>
        public void SetResult(Object obj)
        {
            lock (_mutex)
            {
                _result = obj;
                _countReceived++;
                if (_countExpected == _countReceived)
                {
                    Monitor.PulseAll(_mutex);
                }
            }
        }


        /// <summary>
        /// Clears the result and causes all waiting threads to return
        /// </summary>
        public void Reset()
        {
            lock (_mutex)
            {
                _result = null;
                Monitor.PulseAll(_mutex);
            }
        }

        /// <summary>
        /// String representation of the result
        /// </summary>
        /// <returns>String representation of the result</returns>
        public override String ToString()
        {
            return "result=" + _result + " countReceived=" + _countReceived + " countExpected=" + _countExpected;
        }

        /// <summary>
        /// Checks whether all the nodes responded
        /// </summary>
        public bool AllResultsReceived()
        {
            return _countExpected == _countReceived;
        }

    }
}
