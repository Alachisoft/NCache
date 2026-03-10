using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.SocketServer.MultiBufferReceive;
using Alachisoft.NCache.SocketServer.Pipelining;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.SocketServer.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
#if SERVER || NETCORE
using System.IO.Pipelines;
#endif
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Alachisoft.NCache.SocketServer
{
    public partial class ConnectionManager : IRequestProcessor
    {
#if SERVER || NETCORE
        internal static int PauseWriterThreshold = ServiceConfiguration.PauseWriterThreshold; 
        internal static int ResumeWriterThreshold = PauseWriterThreshold/2;

        async Task<Task> ProcessClientRequest(Socket socket, ClientManager clientManager, Stream stream = null)
        {
            clientManager.InitializeClientStream();
            var pipe = new Pipe(new PipeOptions(null, null, null, PauseWriterThreshold, ResumeWriterThreshold));
            RequestReader requestReader = new RequestReader(clientManager, _cmdManager,this);
            Task writing = FillPipeAsync(socket, pipe.Writer, clientManager);
            Task reading = ReadPipeAsync(pipe.Reader, clientManager, requestReader);
            return Task.WhenAll(reading, writing);
        }
#endif
        public void Process(ICommand procCommand)
        {
            _cmdManager.ProcessCommand(procCommand.ClientManager, procCommand.Command, procCommand.CommandType, procCommand.AcknowledgementId, procCommand.Stats, false);
        }
#if SERVER || NETCORE
        async Task FillPipeAsync(Socket socket, PipeWriter writer, ClientManager clientManager)
        {
            try
            {
                const int minimumBufferSize = 4 * 1024;

                while (true)
                {
                    // Allocate at least 512 bytes from the PipeWriter
                    Memory<byte> memory = writer.GetMemory(minimumBufferSize);
                    int bytesRead = 0;
                    if (!socket.Connected)
                        break;

                    if (clientManager.HasSecureConnection)
                    {
                        var buffer = memory.ToArray();
                        bytesRead = await clientManager.ClientStream.ReadAsync(buffer, 0, buffer.Length);
                        buffer.AsSpan().Slice(0, bytesRead).CopyTo(memory.Span);
                    }
                    else if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> arraySegment))
                    {
                        bytesRead = await socket.ReceiveAsync(arraySegment, SocketFlags.None);
                    }

                    clientManager.AddToClientsBytesRecieved(bytesRead);

                    if (SocketServer.IsServerCounterEnabled)
                    {
                        PerfStatsColl.IncrementBytesReceivedPerSecStats(bytesRead);
                    }

                    if (bytesRead == 0)
                    {
                        DisposeClient(clientManager);
                        break;
                    }
                    // Tell the PipeWriter how much was read from the Socket
                    writer.Advance(bytesRead);

                    // Make the data available to the PipeReader
                    FlushResult result = await writer.FlushAsync();

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }

                // Tell the PipeReader that there's no more data coming
                writer.Complete();
                DisposeClient(clientManager);
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.FillPipeAsync", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
            }
            catch (Exception e)
            {
                DisposeClient(clientManager);

                if (!clientManager.IsDisposed)
                    AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.FillPipeAsync", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.FillPipeAsync", clientManager.ToString() + " Error " + e.ToString());

                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder();
                        log.GenerateConnectionManagerLog(clientManager, e.ToString());
                    }
                }
                catch
                {

                }

            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.StopClientActivity(clientManager.ClientID);
            }
        }

        async Task ReadPipeAsync(PipeReader reader, ClientManager clientManager, RequestReader requestReader)
        {
            try
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync();

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    clientManager.MarkActivity();
                    requestReader.Read(ref buffer);


                    // Tell the PipeReader how much of the buffer we have consumed
                    reader.AdvanceTo(buffer.Start, buffer.End);

                    // Stop reading if there's no more data coming
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }

                // Mark the PipeReader as complete
                reader.Complete();
                DisposeClient(clientManager);
            }
            catch (SocketException so_ex)
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.FillPipeAsync", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);

            }
            catch (Exception e)
            {
                var clientIsDisposed = clientManager.IsDisposed;
                DisposeClient(clientManager);

                if (!clientIsDisposed)
                    AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.FillPipeAsync", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.FillPipeAsync", clientManager.ToString() + " Error " + e.ToString());

                try
                {
                    if (Management.APILogging.APILogManager.APILogManger != null && Management.APILogging.APILogManager.EnableLogging)
                    {
                        APILogItemBuilder log = new APILogItemBuilder();
                        log.GenerateConnectionManagerLog(clientManager, e.ToString());
                    }
                }
                catch
                {

                }

                

            }
            finally
            {
                if (ServerMonitor.MonitorActivity) ServerMonitor.StopClientActivity(clientManager.ClientID);
            }
        }
#endif
    }
}


