#if SERVER || NETCORE
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
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer
{
    public partial class ConnectionManager
    {
        internal static int PauseWriterThreshold = ServiceConfiguration.PauseWriterThreshold; 
        internal static int ResumeWriterThreshold = PauseWriterThreshold/2;

        async Task<Task> ProcessClientRequest(Socket socket, ClientManager clientManager)
        {
            var pipe = new Pipe(new PipeOptions(null, null, null, PauseWriterThreshold, ResumeWriterThreshold));
            RequestReader requestReader = new RequestReader(clientManager, _cmdManager);
            Task writing = FillPipeAsync(socket, pipe.Writer, clientManager);
            Task reading = ReadPipeAsync(pipe.Reader, clientManager, requestReader);
            return Task.WhenAll(reading, writing);
        }

        async Task FillPipeAsync(Socket socket, PipeWriter writer, ClientManager clientManager)
        {
            try
            {
                const int minimumBufferSize = 4 * 1024;

                while (true)
                {
                    // Allocate at least 512 bytes from the PipeWriter
                    Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                    if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> arraySegment))
                    {
                        if (!socket.Connected)
                            break;

                        int bytesRead = await socket.ReceiveAsync(arraySegment, SocketFlags.None);

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
                    }


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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);
            }
            catch (Exception e)
            {
                var clientIsDisposed = clientManager.IsDisposed;
                DisposeClient(clientManager);

                if(!clientIsDisposed)
                    AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + " Error " + e.ToString());

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
                //  clientManager.StopCommandExecution();
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
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + so_ex.ToString());

                DisposeClient(clientManager);

            }
            catch (Exception e)
            {
                var clientIsDisposed = clientManager.IsDisposed;
                DisposeClient(clientManager);

                if (!clientIsDisposed)
                    AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);

                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("ConMgr.RecvClbk", "Error :" + e.ToString());
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("ConnectionManager.ReceiveCallback", clientManager.ToString() + " Error " + e.ToString());

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
    }
}
#endif

