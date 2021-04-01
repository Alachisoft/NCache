#if SERVER
using Alachisoft.NCache.SocketServer.Pipelining;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Alachisoft.NCache.SocketServer
{
    public partial class ConnectionManager
    {
        Thread selector;
        List<Socket> _clients = new List<Socket>();
        Dictionary<string, Pipe> _pipes = new Dictionary<string, Pipe>();

        public void StartSelection()
        {
            selector = new Thread(new ThreadStart(ReadDataFromSeockets))
            {
                IsBackground = true,
                Name = "Selector"
            };

            selector.Start();
        }

        public void AddClient(Socket socket, ClientManager clientManager)
        {
            var key = GetKey(socket);
            Pipe pipe = new Pipe(new PipeOptions(null, null, null, PauseWriterThreshold, ResumeWriterThreshold));

            RequestReader requestReader = new RequestReader(clientManager, _cmdManager);

            lock (_clients)
            {
                _clients.Add(socket);
                _pipes.Add(key, pipe);

                if (_clients.Count == 1)
                    Monitor.Pulse(_clients);
            }

            ReadPipeAsync(pipe.Reader, clientManager, requestReader);
        }

        private void ReadDataFromSeockets()
        {
            List<Socket> seletableClients = new List<Socket>();
            List<ArraySegment<byte>> segments = new List<ArraySegment<byte>>();
            while (true)
            {
                try
                {
                    seletableClients.Clear();

                    lock (_clients)
                    {
                        if (_clients.Count == 0)
                            Monitor.Wait(_clients);

                        seletableClients.AddRange(_clients);
                    }

                    try
                    {
                        if (seletableClients.Count > 0)
                            Socket.Select(seletableClients, null, null, 1000);
                    }
                    catch (ThreadInterruptedException)
                    {
                        //new client has been added
                    }

                    if (seletableClients.Count > 0)
                    {
                        for (int i = 0; i < seletableClients.Count; i++)
                        {
                            var socket = seletableClients[i];
                            var key = GetKey(socket);
                            Pipe pipe = _pipes[key];

                            Memory<byte> memory = pipe.Writer.GetMemory(512);

                            int bytesRead = 0;
                            if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> arraySegment))
                            {
                                try
                                {
                                    segments.Clear();
                                    segments.Add(arraySegment);
                                    bytesRead = socket.Receive(segments, SocketFlags.None);

                                    if (bytesRead == 0)
                                    {
                                        //this client is dead
                                        lock (_clients)
                                        {
                                            _clients.Remove(socket);
                                            _pipes.Remove(key);
                                            continue;
                                        }
                                    }
                                }
                                catch(Exception)
                                {
                                    //this client is dead
                                    lock (_clients)
                                    {
                                        _clients.Remove(socket);
                                        _pipes.Remove(key);
                                        continue;
                                    }
                                }
                                // Tell the PipeWriter how much was read from the Socket
                                pipe.Writer.Advance(bytesRead);
                                pipe.Writer.FlushAsync();
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    int a = 1;
                }
            }
        }

        private string GetKey(Socket socket)
        {
            var endPoint = (IPEndPoint)socket.RemoteEndPoint;
            return $" {endPoint.Address.ToString()}:{endPoint.Port}";
        }
    }
}
#endif

