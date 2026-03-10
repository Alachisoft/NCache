using Alachisoft.NCache.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;

namespace Alachisoft.NCache.Client
{
    internal class GetServerIdentityCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetServerIdentityCommand _getServerIdentityCommand;

        public GetServerIdentityCommand()
        {
            base.name = "GetServerIdentityCommand";

            _getServerIdentityCommand = new Alachisoft.NCache.Common.Protobuf.GetServerIdentityCommand();
            _getServerIdentityCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_SERVER_IDENTITY; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        internal override bool IsKeyBased { get { return false; } }

        internal override bool SupportsAacknowledgement
        {
            get
            {
                return false;
            }
        }


        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getServerIdentityCommand = _getServerIdentityCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_SERVER_IDENTITY;
        }

        protected override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                byte[] discardingBuffer = new byte[20];
                stream.Write(discardingBuffer, 0, discardingBuffer.Length);
                byte[] acknowledgementBuffer = (SupportsAacknowledgement && inquiryEnabled) ? new byte[20] : new byte[0];
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);
                ProtoBuf.Extended.Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Command>(stream, this._command);
                int messageLen = (int)stream.Length - (size.Length + discardingBuffer.Length + acknowledgementBuffer.Length);

                size = HelperFxn.ToBytes(messageLen.ToString());
                stream.Position = discardingBuffer.Length + acknowledgementBuffer.Length;
                stream.Write(size, 0, size.Length);

                this._commandBytes = stream.ToArray();
                stream.Close();
            }
        }

        internal override byte[] ToByte(long acknowledgement, bool inquiryEnabledOnConnection)
        {
            if (_commandBytes == null || inquiryEnabled != inquiryEnabledOnConnection)
            {
                inquiryEnabled = inquiryEnabledOnConnection;
                this.CreateCommand();
                _command.commandID = _commandID;
                this.SerializeCommand();
            }

            if (SupportsAacknowledgement && inquiryEnabled)
            {
                byte[] acknowledgementBuffer = HelperFxn.ToBytes(acknowledgement.ToString());
                MemoryStream stream = new MemoryStream(_commandBytes, 0, _commandBytes.Length, true, true);

                stream.Seek(20, SeekOrigin.Begin);
                stream.Write(acknowledgementBuffer, 0, acknowledgementBuffer.Length);
                _commandBytes = stream.GetBuffer();
            }
            return _commandBytes;
        }
    }
}
