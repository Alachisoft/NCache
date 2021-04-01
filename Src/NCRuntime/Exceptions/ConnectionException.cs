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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// This exception is thrown whenever the connection with cache server is lost while performing an operation on an outproc cache.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    Cache cache = NCache.InitializeCache("sampleCache");
    ///     cache.Add("TestKey","sampleData");
    /// }
    /// catch(ConnectionException ex)
    /// {
    ///     
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class ConnectionException : OperationFailedException
    {
        private System.Net.IPAddress _address = null;

        private int _port;
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public ConnectionException()
        {
        }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public ConnectionException(string reason)
            : base(reason)
        {
        }
        /// <summary> 
        /// Overloaded constructor, takes the reason, ipAddress and port as parameter. 
        /// </summary>
        public ConnectionException(string reason, System.Net.IPAddress ipAddress, int port)
        {
            _address = ipAddress;
            _port = port;
        }

        /// <summary> 
        /// Overloaded constructor, takes the  ipAddress and port as parameter. 
        /// </summary>
        public ConnectionException(System.Net.IPAddress ipAddress, int port)
        {
            _address = ipAddress;
            _port = port;
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        public ConnectionException(int errorCode):base(errorCode)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        public ConnectionException(int errorCode,string reason)
          : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public ConnectionException(int errorCode, string reason ,string stackTrace)
         : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        public ConnectionException(int errorCode,string reason, System.Net.IPAddress ipAddress, int port):base(errorCode,reason)
        {
            _address = ipAddress;
            _port = port;
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        public ConnectionException(int errorCode,System.Net.IPAddress ipAddress, int port):base(errorCode)
        {
            _address = ipAddress;
            _port = port;
        }
        /// <summary>
        /// IPAddress of broken connection
        /// </summary>
        public System.Net.IPAddress IPAddress
        { get { return _address; } }

        /// <summary>
        /// Port of broken connection
        /// </summary>
        public int Port
        { get { return _port; } }
        
      
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected ConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion

    }
}