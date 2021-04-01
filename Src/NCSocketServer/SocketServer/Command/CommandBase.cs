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
using System.Text;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Client;
using System.Diagnostics;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{

    abstract class CommandBase : SimpleLease, IDisposable,ICancellableRequest

    {
        Stopwatch _watch = new Stopwatch();
        protected string immatureId = "-2";
        protected object _userData;
        protected long _requestTimeout;
        protected int forcedViewId = -5;
        protected CancellationTokenSource _cancellationTokenSource;
        internal virtual OperationResult OperationResult{get {return OperationResult.Failure;}}
        public virtual int Operations { get { return 1; } }

        protected IList _serializedResponsePackets = new List<object>();
        //Usefull for debugging (Dump analysis)
        private TimeSpan _elapsedTime;

        internal const string oldClientsGroupType = "$OldClients$"; //Group Type for client before NCache 5.0

        public virtual IList SerializedResponsePackets
        {
            get { return _serializedResponsePackets; }
        }
        
       
        public virtual bool CanHaveLargedata { get { return false; } }
        public virtual bool IsBulkOperation { get { return false; } }
        
        public virtual object UserData
        {
            get { return _userData; }
            set { _userData = value; }
        }

        public long RequestTimeout { get { return _requestTimeout; } set { _requestTimeout = value; } }

        public bool IsCancelled
        {
            get
            {
                return _cancellationTokenSource != null? _cancellationTokenSource.IsCancellationRequested: false;
            }
        }

        public bool HasTimedout
        {
            get
            {
                _elapsedTime = _watch != null ? _watch.Elapsed : TimeSpan.Zero;
                return _elapsedTime.TotalMilliseconds > _requestTimeout;
            }
        }

        //PROTOBUF
        abstract public void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command);



        // For Counters
        public virtual void IncrementCounter(StatisticsCounter collector, long value) 
        {

        }

        public virtual string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = this.GetType().Name;
            details.Append("Command Type: " + this.GetType().Name);
            return details.ToString();
        }

        /// <summary>
        /// Update the indexes passed to the next and current delimiter
        /// </summary>
        /// <param name="command">source string</param>
        /// <param name="delim">dlimiter</param>
        /// <param name="beginQuoteIndex">current delimiter index</param>
        /// <param name="endQuoteIndex">next delimiters index</param>
        protected void UpdateDelimIndexes(ref string command, char delim, ref int beginQuoteIndex, ref int endQuoteIndex)
        {
            beginQuoteIndex = endQuoteIndex;
            endQuoteIndex = command.IndexOf(delim, beginQuoteIndex + 1);
        }

        protected string ExceptionPacket(Exception exc, string requestId)
        {
            byte exceptionId = 0;

            if (exc is OperationFailedException) exceptionId = (int)ExceptionType.OPERATIONFAILED;
            else if (exc is Runtime.Exceptions.AggregateException) exceptionId = (int)ExceptionType.AGGREGATE;
            else if (exc is ConfigurationException) exceptionId = (int)ExceptionType.CONFIGURATION;

            else if (exc is SecurityException) exceptionId = (int)ExceptionType.SECURITY;

            else if (exc is OperationNotSupportedException) exceptionId = (int)ExceptionType.NOTSUPPORTED;

            else exceptionId = (int)ExceptionType.GENERALFAILURE;

            return "EXCEPTION \"" + requestId + "\"" + exceptionId + "\"";
        }

        protected byte[] ExceptionMessage(Exception exc)
        {
            if (exc is Runtime.Exceptions.AggregateException)
            {
                Exception[] innerExceptions = ((Runtime.Exceptions.AggregateException)exc).InnerExceptions;
                if (innerExceptions[0] != null)
                    return Util.HelperFxn.ToBytes(innerExceptions[0].ToString());
            }
            return Util.HelperFxn.ToBytes(exc.ToString());
        }

        protected byte[] ParsingExceptionMessage(Exception exc)
        {
            return Util.HelperFxn.ToBytes("ParsingException: " + exc.ToString());
        }

        public CancellationToken CancellationToken
        {
            get
            {
                if (_cancellationTokenSource == null)
                    _cancellationTokenSource = new CancellationTokenSource();

                return _cancellationTokenSource.Token;
            }
        }


        public void StartWatch ()
        {
           if (!_watch.IsRunning)
            {
                _watch.Start();
            }
        }


        public long ElapsedTIme()
        {
           return _watch.ElapsedMilliseconds;
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Dispose();
            if (_watch != null)
                _watch.Stop();
        }

        public bool Cancel()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }
            return false;
        }

        #region ILeasable

        public override void ResetLeasable()
        {
            _watch.Reset();
            _userData = null;
            immatureId = "-2";
            forcedViewId = -5;
            _elapsedTime = default;
            _requestTimeout = default;
            _cancellationTokenSource = default;
            _serializedResponsePackets.Clear();
        }

        public override void ReturnLeasableToPool()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}