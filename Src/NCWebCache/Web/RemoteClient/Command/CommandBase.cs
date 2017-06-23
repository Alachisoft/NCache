// Copyright (c) 2017 Alachisoft
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
using System.IO;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Web.Caching.Util;
using Alachisoft.NCache.Caching.AutoExpiration;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.Web.Command
{
    abstract class CommandBase
    {
        private byte[] _value = null;
        protected byte[] _commandBytes = null;
        private long requestId = -1;
        private string[] bulkKeys;

        private CommandResponse _result = null;

        internal string _cacheId;
        internal static string NC_NULL_VAL = "NLV";

        internal protected string name;
        internal protected long clientLastViewId = -1;
        internal protected string intendedRecipient;
        internal protected string key;
        internal protected bool isAsync = false;
        internal protected bool asyncCallbackSpecified = false;
        internal protected string ipAddress = string.Empty;

        protected Alachisoft.NCache.Common.Protobuf.Command _command;
       

        private Request _parent;

        internal Request Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        internal abstract RequestType CommandRequestType
        {
            get;
        }

        internal abstract CommandType CommandType
        {
            get;
        }

        internal Alachisoft.NCache.Common.Protobuf.Command.Type Type
        {
            get { return _command.type; }
        }

        internal protected virtual long RequestId
        {
            get { return requestId; }
            set { requestId = value; }
        }

        internal protected long ClientLastViewId
        {
            get { return this.clientLastViewId; }
            set { this.clientLastViewId = value; }
        }
        internal protected string IntendedRecipient
        {
            get { return this.intendedRecipient; }
            set { this.intendedRecipient = value; }
        }
        internal protected CommandResponse Response
        {
            get { return _result; }
            set { _result = value; }
        }

        internal protected string Key
        {
            get { return key; }
        }

        internal protected string[] BulkKeys
        {
            get { return bulkKeys; }
            set { bulkKeys = value; }
        }

        internal protected byte[] Value
        {
            get { return _value; }
            set { this._value = value; }
        }


        internal protected string CommandName
        {
            get { return name; }
        }

        internal protected void ConstructCommand(string cmdString, byte[] data)
        {
            byte[] command = HelperFxn.ToBytes(cmdString);
            _commandBytes = new byte[2 * (Connection.CmdSizeHolderBytesCount + Connection.ValSizeHolderBytesCount) + command.Length + data.Length];

            byte[] commandSize = HelperFxn.ToBytes(command.Length.ToString());
            byte[] dataSize = HelperFxn.ToBytes(data.Length.ToString());


            commandSize.CopyTo(_commandBytes, 0);
            dataSize.CopyTo(_commandBytes, Connection.CmdSizeHolderBytesCount);
            //we copy the command size two times to avoid if an corruption occurs.
            commandSize.CopyTo(_commandBytes, Connection.TotSizeHolderBytesCount);
            dataSize.CopyTo(_commandBytes, Connection.TotSizeHolderBytesCount + Connection.CmdSizeHolderBytesCount);

            command.CopyTo(_commandBytes, 2 * Connection.TotSizeHolderBytesCount);
            data.CopyTo(_commandBytes, (2 * Connection.TotSizeHolderBytesCount) + command.Length);
        }

        protected abstract void CreateCommand();

        public virtual byte[] ToByte()
        {
            if (_commandBytes == null)
            {
                this.CreateCommand();
                this.SerializeCommand();
            }
            return _commandBytes;
        }

        public virtual void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ///Write discarding buffer that socketserver reads
                byte[] discardingBuffer = new byte[20];
                stream.Write(discardingBuffer, 0, discardingBuffer.Length);

                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, this._command);
                int messageLen = (int)stream.Length - (size.Length + discardingBuffer.Length);

                size = HelperFxn.ToBytes(messageLen.ToString());
                stream.Position = discardingBuffer.Length;
                stream.Write(size, 0, size.Length);

                this._commandBytes = stream.ToArray();
                stream.Close();
            }
        }

        public void ResetBytes()
        {
            _commandBytes = null;
        }


        protected string RebuildCommandWithQueryInfo(Hashtable queryInfo)
        {
            System.Text.StringBuilder cmdString = new System.Text.StringBuilder();

            IDictionaryEnumerator queryInfoDic = queryInfo.GetEnumerator();
            while (queryInfoDic.MoveNext())
            {
                cmdString.AppendFormat("{0}\"", queryInfoDic.Key);
                ArrayList values = (ArrayList)queryInfoDic.Value;
                cmdString.AppendFormat("{0}\"", values.Count);

                IEnumerator valuesEnum = values.GetEnumerator();
                while (valuesEnum.MoveNext())
                {
                    if (valuesEnum.Current != null) // (Remove confusion between a null value and empty value
                    {
                        if (valuesEnum.Current is System.DateTime)
                        {
                            System.Globalization.CultureInfo enUs = new System.Globalization.CultureInfo("en-US");
                            cmdString.AppendFormat("{0}\"", ((DateTime)valuesEnum.Current).ToString(enUs));
                        }
                        else
                            cmdString.AppendFormat("{0}\"", valuesEnum.Current);
                    }
                    else
                    {
                        cmdString.AppendFormat("{0}\"", NC_NULL_VAL);
                    }
                }
            }
            return cmdString.ToString();
        }
    }
}
