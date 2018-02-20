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


using System;
using System.Collections;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Local
{
    internal class OpLogManager : IDisposable
    {

        HashVector _loggers = new HashVector();
        bool _logEnteries;
        bool _allowOPAfterBuckeTxfrd = true;       
        private ILogger _ncacheLog;
        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public OpLogManager(bool logEnteries, CacheRuntimeContext context)
        {
            _logEnteries = logEnteries;
            _ncacheLog = context.NCacheLog;
            
        }

        public void StartLogging(int bucket, LogMode loggingMode)
        {
            lock (this)
            {
                if (!_loggers.Contains(bucket))
                {
                    _loggers.Add(bucket, new OperationLogger(bucket, loggingMode));
                }
                else
                {
                    OperationLogger logger = _loggers[bucket] as OperationLogger;
                    logger.LoggingMode = loggingMode;
                    logger.BucketTransfered = false;
                    logger.Clear();
                }
            }
        }

        public bool IsLoggingEnbaled(int bucket, LogMode logMode)
        {
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    OperationLogger logger = _loggers[bucket] as OperationLogger;
                    return logger.LoggingMode == logMode;
                }
            }
            return false;
        }

        public void StopLogging(int bucket)
        {
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    OperationLogger logger = _loggers[bucket] as OperationLogger;
                    logger.BucketTransfered = _allowOPAfterBuckeTxfrd; ;
                    logger.Clear();
                }
            }
        }

        public void StopLogging(ArrayList buckets)
        {
            if (buckets != null)
            {
                foreach (int bucket in buckets) StopLogging(bucket);
            }
        }

        public void RemoveLogger(int bucket)
        {
            lock (this)
            {
                _loggers.Remove(bucket);
            }
        }

        /// <summary>
        /// Logs the operation
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="key"></param>
        /// <param name="entry"></param>
        /// <param name="type"></param>
        /// <param name="logMode"></param>
        /// <returns>True, in case operation is logged otherwise false</returns>
        public bool LogOperation(int bucket, object key, Object entry, OperationType type)
        {
            OperationLogger logger = null;
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    logger = _loggers[bucket] as OperationLogger;
                }
            }
           
            if(logger != null)
            {
                if (_logEnteries)
                    logger.LogOperation(key, entry, type);
                else
                    logger.LogOperation(key, null, type);
                return true;
            }
            return false;
        }

        /// <summary>
        /// if bucket has been transfered to an another node than operation are not allowed.
        /// </summary>
        /// <param name="bucket"></param>
        /// <returns></returns>
        public bool IsOperationAllowed(int bucket)
        {
            OperationLogger logger = null;
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    logger = _loggers[bucket] as OperationLogger;
                }
            }

            if (logger != null && logger.BucketTransfered)
            {
                if (OperationContext.IsReplicationOperation) //replication operations are not filtered
                    return true;

                return false;
            }

            return true;
        }

        public Hashtable GetLogTable(int bucket)
        {
            Hashtable result = null;
            OperationLogger logger = null;
           
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    logger = _loggers[bucket] as OperationLogger;
                }
            }

            if (logger != null)
            {
                result = logger.LoggedKeys.Clone() as Hashtable;
                logger.Clear();
            }
            return result;
        }

        public Hashtable GetLoggedEnteries(int bucket)
        {
            Hashtable result = null;
            OperationLogger logger = null;
            lock (this)
            {
                if (_loggers.Contains(bucket))
                {
                    logger = _loggers[bucket] as OperationLogger;
                }
            }
            if (logger != null)
            {

                result = logger.LoggedEnteries.Clone() as Hashtable;
                logger.Clear();
            }

            return result;
        }


        #region IDisposable Members

        public void Dispose()
        {
            lock (this)
            {
                if (_loggers != null)
                {
                    foreach (OperationLogger logger in _loggers.Values)
                    {
                        logger.Clear();
                    }
                    _loggers.Clear();
                }
            }
        }

        #endregion
    }
}