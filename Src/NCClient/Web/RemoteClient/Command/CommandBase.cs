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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.Client
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
        protected internal bool inquiryEnabled;

        protected Alachisoft.NCache.Common.Protobuf.Command _command;

        protected int _commandID = 0;
        public int CommandID
        {
            get { return _commandID; }
            set { _commandID = value; }
        }

        internal byte[] Serialized { get; set; }

        private Address _finalDestinationAddress;

        /// <summary>
        /// Final address where this command has been sent. Set after response for that command has been initialized
        /// </summary>
        public Address FinalDestinationAddress
        {
            get { return _finalDestinationAddress; }
            set { _finalDestinationAddress = value; }
        }

        public virtual bool SupportsSurrogation { get { return false; } }

        private bool _isRetry;
        /// <summary>
        /// Indicates wehter the command is a retry command (if first attempt has been failed)
        /// </summary>
        public bool IsRetry
        {
            get { return _isRetry; }
            set { _isRetry = value; }
        }

        private Request _parent;

        internal Request Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        internal bool IsInternalCommand
        {
            get { return CommandRequestType == RequestType.InternalCommand; }
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

        internal virtual bool SupportsAacknowledgement { get { return true; } }


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

        /// <summary>
        /// Indicates of command is safe to reexecute if failed while executing.
        /// Safe commands return same result if reexecuted with same parameters
        /// </summary>
        internal virtual bool IsSafe { get { return true; } }

        /// <summary>
        /// Indicades if the command is a key-based operation.
        /// This flags helps in sending command to appropriate server if more than one servers are available.
        /// </summary>

        internal virtual bool IsKeyBased { get { return true; } }

        public long AcknowledgmentId { get; internal set; }
        public bool PulseOnSend { get; internal set; }
        public bool SentOverWire { get; internal set; }
        public SendError SendError { get; internal set; }

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

        internal virtual byte[] ToByte(long acknowledgement, bool inquiryEnabledOnConnection)
        {
            if (_commandBytes == null || inquiryEnabled != inquiryEnabledOnConnection)
            {
                inquiryEnabled = inquiryEnabledOnConnection;
                this.CreateCommand();
                if (_command != null) _command.commandID = this._commandID;
                this.SerializeCommand();
            }

            if (SupportsAacknowledgement && inquiryEnabled)
            {
                byte[] acknowledgementBuffer = HelperFxn.ToBytes(acknowledgement.ToString());
                MemoryStream stream = new MemoryStream(_commandBytes, 0, _commandBytes.Length, true, true);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                _commandBytes = stream.GetBuffer();
            }
            return _commandBytes;
        }

        protected virtual void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _command);
        }

        protected virtual short GetCommandHandle()
        {
            return 0;
        }

        protected virtual void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                //Writes a section for acknowledgment buffer.
                byte[] acknowledgementBuffer = (SupportsAacknowledgement && inquiryEnabled) ? new byte[20] : new byte[0];
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);

                // Write command type, write 0 for base Command.
                byte[] commandType = HelperFxn.WriteShort(GetCommandHandle());
                stream.Write(commandType, 0, commandType.Length);

                // Writes a section for the command size.
                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);

                //Writes the command.
                SerializeCommandInternal(stream);

                int messageLen = (int)stream.Length - (size.Length + acknowledgementBuffer.Length + commandType.Length);

                //Int32.Max.String() = 10 chars long...
                size = HelperFxn.ToBytes(messageLen.ToString());

                stream.Position = 0;
                stream.Position += acknowledgementBuffer.Length;
                stream.Position += commandType.Length;
                stream.Write(size, 0, size.Length);

                this._commandBytes = stream.ToArray();
                stream.Close();
            }
        }

        /// <summary>
        /// Resets command by resetting command bytes and assigns a new commandID
        /// </summary>
        public void ResetCommand()
        {
            if (_parent != null)
            {
                this._commandBytes = null;
                this._commandID = this._parent.GetNextCommandID();
            }
        }

        /// <summary>
        /// Create a dedicated command by merging all commands provided to the function
        /// </summary>
        /// <param name="commands">Commands needed to be merged to create dedicated command</param>
        /// <returns>Dedicated command</returns>
        public static CommandBase GetDedicatedCommand(IEnumerable<CommandBase> commands)
        {
            /*
             * usama@30092014 
             * This function will be called only for bulk commands
             * If commands are non-key commands, each command in passed commands will be the same,
             * If commands are key based bulk command, each command will have seperate set of keys. Need to merge all keys to create a dedicated command.
             */
            CommandBase dedicatedCommand = null;
            List<CommandBase> commandsList = new List<CommandBase>(commands);
            dedicatedCommand = commandsList[0].GetMergedCommand(commandsList);
            dedicatedCommand._commandBytes = null;
            dedicatedCommand.ClientLastViewId = Broker.ForcedViewId;
            dedicatedCommand.Parent.Commands.Clear();
            dedicatedCommand.Parent.AddCommand(new Address(IPAddress.Any, 9800), dedicatedCommand);
            return dedicatedCommand;
        }

        public byte[] GetSerializedSurrogateCommand()
        {
            byte[] serializedbytes = null;
            using (MemoryStream stream = new MemoryStream())
            {
                this.CreateCommand();
                if(_command != null)
                {
                    ProtoBuf.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, _command);
                }
                serializedbytes = stream.ToArray();
            }
            return serializedbytes;
        }

        protected virtual CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            return commands != null && commands.Count > 0 ? commands[0] : null;
        }

        protected string RebuildCommandWithTagInfo(Hashtable tagInfo)
        {
            System.Text.StringBuilder cmdString = new System.Text.StringBuilder();

            cmdString.AppendFormat("{0}\"", tagInfo["type"] as string);
            ArrayList tagsList = tagInfo["tags-list"] as ArrayList;
            cmdString.AppendFormat("{0}\"", tagsList.Count);

            IEnumerator tagsEnum = tagsList.GetEnumerator();
            while (tagsEnum.MoveNext())
            {
                if (tagsEnum.Current != null)
                {
                    cmdString.AppendFormat("{0}\"", tagsEnum.Current);
                }
                else
                {
                    cmdString.AppendFormat("{0}\"", NC_NULL_VAL);
                }
            }

            return cmdString.ToString();
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
                    if (valuesEnum.Current != null) //(Remove confusion between a null value and empty value
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
