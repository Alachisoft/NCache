#if SERVER || NETCORE
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.SocketServer.MultiBufferReceive;
using System;
using System.Buffers;
using System.IO;
using System.Linq;
using Alachisoft.NCache.Common.Util;
using System.Threading;
using Alachisoft.NCache.Common.Stats;

namespace Alachisoft.NCache.SocketServer.Pipelining
{
    internal class RequestReader
    {
        private int _headerLength;
        private RequestInfo _partialRequest;
        private ClientManager _clientManager;
        private ICommandManager _cmdManager;
        private CommandProcessorPool _commandProcessorPool;

        private int? _expectedLength = null;
        private long _acknowledgementId = -1;
        private long _feed = 0;

        public RequestReader(ClientManager clientManager, ICommandManager commandManager, IRequestProcessor requestProcessor)
        {
            _headerLength = ConnectionManager.MessageSizeHeader;
            
            _clientManager = clientManager;
            _cmdManager = commandManager;
            if (ServiceConfiguration.UseCustomThreadPool)
            {
                _commandProcessorPool = new CommandProcessorPool(
                    Environment.ProcessorCount * ServiceConfiguration.CustomPipeliningThreadPoolFactor,
                    requestProcessor
                );

                _commandProcessorPool.Start();
            }
        }
        
        private bool ReadPartialRequest(ref ReadOnlySequence<byte> buffer)
        {
            if (_partialRequest == null) return true;

            _partialRequest.Read(ref buffer);
            if (_partialRequest.IsCompleted)
            {
                ReadOnlySequence<byte> readOnlySequence = new ReadOnlySequence<byte>(_partialRequest.DataBuffer);
                ParseCommand(ref readOnlySequence);
                _partialRequest = null;

                return true;
            }

            return false;
        }

        private void ReadRequest(ref ReadOnlySequence<byte> buffer)
        {
            if (_expectedLength == null)
            {
                if (!ReadHeader(ref buffer)) return;
            }

            ReadCommand(ref buffer);
        }

        private void ReadAcknowledgement(ref ReadOnlySequence<byte> buffer)
        {
            int ackIdLength = ConnectionManager.AckIdBufLen;
            _acknowledgementId = HelperFxn.ToInt64(buffer.Slice(0, ackIdLength).ToArray(), 0, ackIdLength);
            buffer = buffer.Slice(ackIdLength, buffer.End);
        }

        private bool ReadHeader(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length >= _headerLength)
            {
                _expectedLength = RequestDeserializer.ToInt32(buffer.Slice(0, _headerLength).ToArray(), 0, _headerLength);
                buffer = buffer.Slice(_headerLength, buffer.End);

                return true;
            }

            return false;
        }

        private void ReadCommand(ref ReadOnlySequence<byte> buffer)
        {
            if (_expectedLength != null)
            {
                if (buffer.Length >= _expectedLength)
                {
                    ParseCommand(ref buffer);
                }
                else if (_expectedLength >= ConnectionManager.PauseWriterThreshold)
                {
                    byte[] tempBuffer = new byte[_expectedLength.Value];
                    byte[] remainingBuffer = buffer.ToArray();

                    Buffer.BlockCopy(remainingBuffer, 0, tempBuffer, 0, remainingBuffer.Length);
                    _partialRequest = new RequestInfo(tempBuffer, tempBuffer.Length - remainingBuffer.Length);
                    buffer = buffer.Slice(buffer.Length, buffer.End);
                }
            }
        }

        private void ParseCommand(ref ReadOnlySequence<byte> buffer)
        {
            if (_clientManager.SupportAcknowledgement)
                ReadAcknowledgement(ref buffer);

            short cmdType = 0;

            if (_clientManager.ClientVersion >= 5000)
            {
                var cmdTypeBuffer = buffer.Slice(0, 2);

                if (cmdTypeBuffer.First.Span.Length >= 2)
                    cmdType = HelperFxn.ConvertToShort(cmdTypeBuffer.First.Span);
                else
                    cmdType = HelperFxn.ConvertToShort(cmdTypeBuffer.ToArray());

                buffer = buffer.Slice(2, buffer.End);
            }

            var commandLengthBuffer = buffer.Slice(0, _headerLength);
            var cmdLength = RequestDeserializer.ToInt32(commandLengthBuffer.ToArray(), 0, _headerLength);
            buffer = buffer.Slice(_headerLength, buffer.End);

            var commandBuffer = buffer.Slice(0, cmdLength);
            object command = null;
            using (var stream = new MemoryStream(commandBuffer.ToArray()))
                command = RequestDeserializer.Deserialize(cmdType, stream);

            buffer = buffer.Slice(cmdLength, buffer.End);

            if (CommandHelper.IsBasicCRUDOperation((Common.Protobuf.Command.Type)cmdType))
            {
                _cmdManager.ProcessCommand(_clientManager, command, cmdType, _acknowledgementId, null, buffer.Length > 0);
            }
            else
            {
                //Due to response pipelining, old responses of basic CRUD operations are queued for sending
                //therefore before executing this long running command, we should send those responses
                _clientManager.SendPendingResponses(true);

                if (ServiceConfiguration.UseCustomThreadPool)
                {
                    var longRunningCmd = new LongRunningCommand()
                    {
                        Command = command,
                        CommandType = cmdType,
                        AcknowledgementId = _acknowledgementId,
                        ClientManager = _clientManager,
                    };

                    _commandProcessorPool.EnqueuRequest(longRunningCmd, Interlocked.Increment(ref _feed));
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessCommandAsync), new LongRunningCommand() { Command = command, CommandType = cmdType, AcknowledgementId = _acknowledgementId });
                }
            }

            _expectedLength = null;
            _acknowledgementId = -1;
        }

       

        private void ProcessCommandAsync(object state)
        {
            try
            {
                LongRunningCommand command = state as LongRunningCommand;
                _cmdManager.ProcessCommand(_clientManager, command.Command, command.CommandType, command.AcknowledgementId, null, false);
            }
            catch (Exception)
            {
            }
        }

        internal void Read(ref ReadOnlySequence<byte> buffer)
        {
            if (!ReadPartialRequest(ref buffer))
                return;

            do
            {
                ReadRequest(ref buffer);
            } while (_expectedLength == null && buffer.Length >= _headerLength);
        }
        public void Dispose()
        {
            _headerLength = 0;
            _partialRequest = null;
            _clientManager = null;
            _cmdManager = null;
            _expectedLength = null;
            _acknowledgementId = -1;

            if (ServiceConfiguration.UseCustomThreadPool && _commandProcessorPool != null)
            {
                _commandProcessorPool.Stop();
            }
        }

    }
}
#endif


