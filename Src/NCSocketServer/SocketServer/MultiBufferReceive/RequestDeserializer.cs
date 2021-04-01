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

using Alachisoft.NCache.SocketServer.Util;
using System;
using System.IO;
using System.Threading;
using Alachisoft.NCache.Common.Stats;
using System.Text;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal sealed class RequestDeserializer : IDisposable
    {
        private static Type[] _commandTypes = new Type[150];

        public static Type[] CommandTypes { get { return _commandTypes; } }

        private readonly object _mutex;

        int _expecteDataLength;
        private CommandStream _commandStream;
        private CurrentCommand _commandInfo;

        private ClientManager _clientManager;

        private bool _isBusyDeserializing, _hasReturnedAThread;

        public byte[] LengthBuffer { get; private set; } = new byte[ConnectionManager.MessageSizeHeader];

        public bool BusyProcessing
        {
            get { lock (_mutex) { return _isBusyDeserializing; } }
            set { lock (_mutex) { _isBusyDeserializing = value; } }
        }

        static RequestDeserializer()
        {
            _commandTypes[0] = typeof(Common.Protobuf.Command);
            _commandTypes[(int)Common.Protobuf.Command.Type.INSERT] = typeof(Common.Protobuf.InsertCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET] = typeof(Common.Protobuf.GetCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.INSERT_BULK] = typeof(Common.Protobuf.BulkInsertCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_NEXT_CHUNK] = typeof(Common.Protobuf.GetNextChunkCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_BULK] = typeof(Common.Protobuf.BulkGetCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_READER_CHUNK] = typeof(Common.Protobuf.GetReaderNextChunkCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.DELETE] = typeof(Common.Protobuf.DeleteCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REMOVE] = typeof(Common.Protobuf.RemoveCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.COUNT] = typeof(Common.Protobuf.CountCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.CLEAR] = typeof(Common.Protobuf.ClearCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.CONTAINS_BULK] = typeof(Common.Protobuf.ContainsBulkCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.ADD] = typeof(Common.Protobuf.AddCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.ADD_BULK] = typeof(Common.Protobuf.BulkAddCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.ADD_ATTRIBUTE] = typeof(Common.Protobuf.AddAttributeCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_CACHE_ITEM] = typeof(Common.Protobuf.GetCacheItemCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_BULK_CACHEITEM] = typeof(Common.Protobuf.BulkGetCacheItemCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_GROUP_NEXT_CHUNK] = typeof(Common.Protobuf.GetGroupNextChunkCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REMOVE_BULK] = typeof(Common.Protobuf.BulkRemoveCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.DELETE_BULK] = typeof(Common.Protobuf.BulkDeleteCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REMOVE_TOPIC] = typeof(Common.Protobuf.RemoveTopicCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_TOPIC] = typeof(Common.Protobuf.GetTopicCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_MESSAGE] = typeof(Common.Protobuf.GetMessageCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.MESSAGE_COUNT] = typeof(Common.Protobuf.MessageCountCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.MESSAGE_PUBLISH] = typeof(Common.Protobuf.MessagePublishCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.DISPOSE] = typeof(Common.Protobuf.DisposeCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.DISPOSE_READER] = typeof(Common.Protobuf.DisposeReaderCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.DELETEQUERY] = typeof(Common.Protobuf.DeleteQueryCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.LOCK] = typeof(Common.Protobuf.LockCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.UNLOCK] = typeof(Common.Protobuf.UnlockCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.ISLOCKED] = typeof(Common.Protobuf.IsLockedCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.LOCK_VERIFY] = typeof(Common.Protobuf.LockVerifyCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.UNREGISTER_BULK_KEY_NOTIF] = typeof(Common.Protobuf.UnRegisterBulkKeyNotifCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.UNREGISTER_KEY_NOTIF] = typeof(Common.Protobuf.UnRegisterKeyNotifCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REGISTER_BULK_KEY_NOTIF] = typeof(Common.Protobuf.RegisterBulkKeyNotifCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REGISTER_KEY_NOTIF] = typeof(Common.Protobuf.RegisterKeyNotifCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.UNSUBSCRIBE_TOPIC] = typeof(Common.Protobuf.UnSubscribeTopicCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.SUBSCRIBE_TOPIC] = typeof(Common.Protobuf.SubscribeTopicCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.RAISE_CUSTOM_EVENT] = typeof(Common.Protobuf.RaiseCustomEventCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.MESSAGE_ACKNOWLEDGMENT] = typeof(Common.Protobuf.MesasgeAcknowledgmentCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_PRODUCT_VERSION] = typeof(Common.Protobuf.GetProductVersionCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.POLL] = typeof(Common.Protobuf.PollCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.PING] = typeof(Common.Protobuf.PingCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.TOUCH] = typeof(Common.Protobuf.TouchCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.REGISTER_POLLING_NOTIFICATION] = typeof(Common.Protobuf.RegisterPollingNotificationCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.SYNC_EVENTS] = typeof(Common.Protobuf.SyncEventsCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_SERIALIZATION_FORMAT] = typeof(Common.Protobuf.GetSerializationFormatCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.INQUIRY_REQUEST] = typeof(Common.Protobuf.InquiryRequestCommand);
            _commandTypes[(int)Common.Protobuf.Command.Type.GET_CONNECTED_CLIENTS] = typeof(Common.Protobuf.GetConnectedClientsCommand);

        }

        internal RequestDeserializer(ClientManager clientManager)
        {
            _mutex = new object();
            _clientManager = clientManager;
            _commandStream = new CommandStream();
            _commandInfo = new CurrentCommand();
        }

        internal bool DeserializeCommand(out object command, out short cmdType, out bool waitForResponse)
        {
            // TODO: Take care of UsageStats stats = new UsageStats(); stats.BeginSample();
            command = null;
            cmdType = 0;
            waitForResponse = false;

            try
            {
                do
                {
                    switch (_commandInfo.State)
                    {
                        case CommandState.ReadLength:
                            if (ReadLength2())
                            {
                                if (ReadCommand(out command, out cmdType, out waitForResponse))
                                    return true;
                            }
                            break;
                        case CommandState.ReadCommand:
                            if (ReadCommand(out command, out cmdType, out waitForResponse))
                                return true;
                            break;
                    }

                } while (HasMoreData());
            }
            finally
            {
            }

            lock (_mutex)
            {
                if (HasMoreData()) return true;
                _isBusyDeserializing = false;
                _hasReturnedAThread = false;
            }
            return false;
        }

        private bool ReadLength2()
        {
            _expecteDataLength = ConnectionManager.MessageSizeHeader;
            if (!_commandStream.EnsureData(ConnectionManager.MessageSizeHeader)) return false;

            _commandStream.CommandLength = ConnectionManager.MessageSizeHeader;
            _commandStream.Position = 0;

            _commandStream.Read(LengthBuffer, 0, ConnectionManager.MessageSizeHeader);

            int commandLength = ToInt32(LengthBuffer, 0, LengthBuffer.Length);

            _commandInfo.State = CommandState.ReadCommand;
            _commandInfo.CommandLength = commandLength;

            return true;
        }

        public static int ToInt32(byte[] buffer, int offset, int size)
        {
            int cInt = 0;
            try
            {
                cInt = Convert.ToInt32(UTF8Encoding.UTF8.GetString(buffer, offset, size));
            }
            catch (System.Exception)
            {
                throw;
            }

            return cInt;
        }

        private bool ReadCommand(out object command, out short cmdType, out bool waitForResponse)
        {
            command = default(object);
            cmdType = _commandInfo.CommandType;
            waitForResponse = false;
            //In case there is no partial command is in reading process
            if (_commandInfo.CommandBuffer == null)
            {
                bool readCommandType = _clientManager.ClientVersion >= 5000;
                int nextHeaderLength = ConnectionManager.MessageSizeHeader;
                if (readCommandType) nextHeaderLength += sizeof(short);

                _expecteDataLength = _commandInfo.CommandLength;
                if (!_commandStream.EnsureData(nextHeaderLength)) return false;

                _commandStream.CommandLength = sizeof(short);
                _commandStream.Position = 0;

                if (readCommandType)
                {
                    byte[] type = new byte[2];
                    _commandStream.Read(type, 0, type.Length);
                    _commandInfo.CommandType = HelperFxn.ConvertToShort(type);
                }
                else
                    _commandInfo.CommandType = 0;

                cmdType = _commandInfo.CommandType;
                _commandStream.CommandLength = ConnectionManager.MessageSizeHeader;
                _commandStream.Position = 0;

                _commandStream.Read(LengthBuffer, 0, ConnectionManager.MessageSizeHeader);

                int commandSize = ToInt32(LengthBuffer, 0, ConnectionManager.MessageSizeHeader);
                _expecteDataLength -= nextHeaderLength;
                _commandInfo.CommandBuffer = new byte[commandSize];
                _commandInfo.BufferOffset = 0;
            }

            if(_commandStream.HasAnyData())
            {
                _commandStream.Position = 0;
                int streamLength = (int)_commandStream.AvailableData;
                int dataToCopy = _expecteDataLength <= streamLength ? _expecteDataLength : streamLength;

                _commandStream.CommandLength = dataToCopy;
                _commandStream.Read(_commandInfo.CommandBuffer,_commandInfo.BufferOffset, dataToCopy);

                _expecteDataLength -= dataToCopy;
                _commandInfo.BufferOffset += dataToCopy;
            }

            //command is full read from main command stream so let's parse it
            if (_expecteDataLength == 0)
            {
                using (MemoryStream stream = new MemoryStream(_commandInfo.CommandBuffer))
                {
                    cmdType = _commandInfo.CommandType;
                    //get ready for next command to parse; reset before command deserialize
                    _commandInfo.CommandBuffer = null;
                    _commandInfo.BufferOffset = 0;
                    _commandInfo.CommandType = 0;

                    if (_clientManager.ClientVersion >= 5000 )
                    {
                        command = ProtoBuf.Serializer.NonGeneric.Deserialize(_commandTypes[cmdType], stream);
                    }
                    else
                    {
                        command = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Command>(stream);
                    }
                }

                _commandInfo.State = CommandState.ReadLength;
                _commandInfo.CommandLength = 0;
                waitForResponse = true;
                return true;
            }
            return false;
        }

        public bool HasAnyData()
        {
            lock (_mutex)
            {
                return _commandStream.HasAnyData();
            }
        }

        public bool HasMoreData()
        {
            lock (_mutex)
            {
                return _commandStream.EnsureData(_expecteDataLength);

                if (!_hasReturnedAThread)
                {
                    return false;
                }

                _hasReturnedAThread = false;
            }

            return true;
        }

        private bool ReadLength()
        {
            if (!_commandStream.EnsureData(ConnectionManager.MessageSizeHeader)) return false;

            _commandStream.CommandLength = ConnectionManager.MessageSizeHeader;
            _commandStream.Position = 0;

            _commandStream.Read(LengthBuffer, 0, ConnectionManager.MessageSizeHeader);

            _commandInfo.CommandLength = HelperFxn.ToInt32(LengthBuffer, 0, LengthBuffer.Length);

            _commandInfo.State = CommandState.ReadAcknowledgement;

            return true;
        }

        private bool ReadAcknowledgement()
        {
            if (_clientManager.SupportAcknowledgement)
            {
                if (!_commandStream.EnsureData(ConnectionManager.AckIdBufLen)) return false;

                _commandStream.CommandLength = ConnectionManager.AckIdBufLen;
                _commandStream.Position = 0;

                byte[] ackBuffer = new byte[ConnectionManager.AckIdBufLen];
                _commandStream.Read(ackBuffer, 0, ConnectionManager.AckIdBufLen);

                _commandInfo.AcknowledgementId = HelperFxn.ToInt64(ackBuffer, 0, ConnectionManager.AckIdBufLen);
            }

            _commandInfo.State = CommandState.ReadCommand;
            return true;
        }

        int _noOfparsedCommand;

        private bool MoreDataArrived()
        {
            if (!_commandStream.EnsureData(1))
            {
                lock (_mutex)
                {
                    if (!_commandStream.EnsureData(1))
                    {
                        _isBusyDeserializing = false;
                        return false;
                    }

                    _hasReturnedAThread = false;
                }
            }
            return true;
        }

        internal void AddCommandBuffer(CommandBuffer cmdBuffer)
        {
            _commandStream.AddCommandBuffer(cmdBuffer);
        }

        public static object Deserialize(short cmdType, Stream stream)
        {
            return ProtoBuf.Serializer.NonGeneric.Deserialize(_commandTypes[cmdType], stream);
        }

        public void Dispose()
        {
            _commandStream.Dispose();
        }
    }
}
