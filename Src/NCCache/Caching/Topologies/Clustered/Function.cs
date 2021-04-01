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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Monitoring;
using System.Threading;
using System.Diagnostics;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// An info object that wraps a function code and object. Function codes are to be
    /// defined by the clients/derivations of clustered cache.
    /// </summary>
    [Serializable]
    internal class Function : ICompactSerializable, IRentableObject,ICancellableRequest
    {
        /// <summary> The function code. </summary>
        private byte _opcode;

        /// <summary> The paramter for the function. </summary>
        private object _operand;

        /// <summary> Inhibit processing own messages. </summary>
        private bool _excludeSelf = true;

        private object _syncKey;

        private int _rentId;

        private Array _userDataPayload;

        private bool _responseEpected = false;

        private TimeSpan _clientRequestTimeout = TimeSpan.FromSeconds(90);

        [NonSerialized]
        private CancellationTokenSource _cancellationSource;

        [NonSerialized]
        private Stopwatch _executionWatch;

        private bool _cancellable;
        //Usefull for debugging (dump analysis)
        private TimeSpan _elapsedTime;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="opcode">The operation code</param>
        /// <param name="operand">Parameter for the operation</param>
        public Function(byte opcode, object operand)
        {
            _opcode = opcode;
            _operand = operand;
        }

        public Function()
        {

        }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="opcode">The operation code</param>
        /// <param name="operand">Parameter for the operation</param>
        /// <param name="excludeSelf">Flag to inhibit processing self messages</param>
        public Function(byte opcode, object operand, bool excludeSelf)
        {
            _opcode = opcode;
            _operand = operand;
            _excludeSelf = excludeSelf;

        }
        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="opcode">The operation code</param>
        /// <param name="operand">Parameter for the operation</param>
        /// <param name="excludeSelf">Flag to inhibit processing self messages</param>
        public Function(byte opcode, object operand, bool excludeSelf, object syncKey)
            : this(opcode, operand, excludeSelf)
        {
            _syncKey = syncKey;
        }

        /// <summary> The function code. </summary>
        public byte Opcode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        /// <summary> The function code. </summary>
        public object Operand
        {
            get { return _operand; }
            set { _operand = value; }
        }

        /// <summary> The function code. </summary>
        public bool ExcludeSelf
        {
            get { return _excludeSelf; }
            set { _excludeSelf = value; }
        }

        /// <summary>
        /// Gets or sets the SyncKey for the current operation.
        /// </summary>
        public object SyncKey
        {
            get { return _syncKey; }
            set { _syncKey = value; }
        }

        #region	/                 --- ICompactSerializable ---           /

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _opcode = reader.ReadByte();
            _excludeSelf = reader.ReadBoolean();
            _operand = reader.ReadObject();
            _syncKey = reader.ReadObject();
            _responseEpected = reader.ReadBoolean();
            _cancellable = reader.ReadBoolean();
            _clientRequestTimeout = (TimeSpan)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.Write(_opcode);
            writer.Write(_excludeSelf);
            writer.WriteObject(_operand);
            writer.WriteObject(_syncKey);
            writer.Write(_responseEpected);
            writer.Write(_cancellable);
            writer.WriteObject(_clientRequestTimeout);
        }

        #endregion

        #region IRentableObject Members

        public int RentId
        {
            get
            {
                return _rentId;
            }
            set
            {
                _rentId = value;
            }
        }

        #endregion

        public Array UserPayload
        {
            get { return _userDataPayload; }
            set { _userDataPayload = value; }
        }

        public bool ResponseExpected
        {
            get { return _responseEpected; }
            set { _responseEpected = value; }
        }

        public TimeSpan ClientRequestTimeout
        {
            get { return _clientRequestTimeout; }
            set { _clientRequestTimeout = value; }
        }

        public bool Cancellable
        {
            get { return _cancellable; }
            set { _cancellable = value; }
        }

        public CancellationToken CancellationToken
        {
            get
            {
                if (_cancellationSource == null)
                    _cancellationSource = new CancellationTokenSource();

                return _cancellationSource.Token;
            }
        }

        public void InitializeCanellationToken()
        {
            CancellationToken token = CancellationToken;

            if(_operand is object[])
            {
                object[] args = _operand as object[];

                for(int i=0; i<args.Length; i++)
                {
                    object argument = args[i];
                    if (argument is OperationContext)
                    {
                        ((OperationContext)argument).CancellationToken = token;

                        TimeSpan timeout = TimeSpan.FromMilliseconds(((OperationContext)argument).ClientOperationTimeout);
                        if (timeout >= _clientRequestTimeout)
                            _clientRequestTimeout = timeout;
                        break;
                    }
                }
            }
        }

        public void StartExecution()
        {
            if (_executionWatch == null)
                _executionWatch = new Stopwatch();

            _executionWatch.Start();
        }

        public void StopExecution()
        {
            if (_executionWatch != null)
                _executionWatch.Stop();
        }

        public bool IsCancelled
        {
            get
            {
                return _cancellationSource != null ? _cancellationSource.IsCancellationRequested : false;
            }
        }

        public bool HasTimedout
        {
            get
            {
                _elapsedTime = _executionWatch != null ? _executionWatch.Elapsed : TimeSpan.Zero;
                return _elapsedTime > _clientRequestTimeout ? true : false;
                
            }
        }

        public override string ToString()
        {
            return (Enum.Parse(typeof(Alachisoft.NCache.Caching.Topologies.Clustered.ClusterCacheBase.OpCodes), this.Opcode.ToString())).ToString();
        }

        public bool Cancel()
        {
            if (_cancellationSource != null && !_cancellationSource.IsCancellationRequested)
            {
                _cancellationSource.Cancel();
                return true;
            }
            return false;
        }
    }
}