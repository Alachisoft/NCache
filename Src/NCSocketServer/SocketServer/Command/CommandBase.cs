// Copyright (c) 2015 Alachisoft
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
using System.Text;
using System.Threading;
using Alachisoft.NCache.SocketServer;

using Alachisoft.NCache.Runtime.Exceptions;
using System.Collections.Generic; 
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    abstract class CommandBase
    {
        protected string immatureId = "-2";
        protected object _userData;

        protected int forcedViewId = -5;

        internal virtual OperationResult OperationResult{get {return OperationResult.Failure;}}
        public virtual int Operations { get { return 1; } }

        protected IList<byte[]> _serializedResponsePackets = new List<byte[]>();

        public virtual IList<byte[]> SerializedResponsePackets
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

        //PROTOBUF
        abstract public void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command);

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
    }
}
