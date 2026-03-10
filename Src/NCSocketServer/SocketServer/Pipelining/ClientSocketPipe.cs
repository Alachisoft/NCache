using Alachisoft.NCache.SocketServer.MultiBufferSend;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer.Pipelining
{
    public class ClientSocketPipe
    {
        private PipeWriter _writer;
        private Stream _stream;

        public ClientSocketPipe(Stream stream)
        {
            _stream = stream;
            _writer = PipeWriter.Create(_stream, new StreamPipeWriterOptions(leaveOpen: false));
        }


#region Pipelining of Sending Data to Client

        public async void SendResponsePipelining(IList response, ClientManager clientManager, bool waitForOtherResponses = false)
        {
            try
            {
                if (clientManager.ClientSocket == null)
                {
                    return;
                }

                lock (clientManager.queueLock)
                {
                    if (response != null)
                    {
                        clientManager.AddResponse(response);
                    }

                    if (clientManager.ChunkSending || waitForOtherResponses || clientManager.ResCount == 0)
                    {
                        return;
                    }

                    clientManager.ChunkSending = true;
                }

                await WriteResponsesToPipe(clientManager); // Ensure it is awaited
            }
            catch (Exception e)
            {
                ConnectionManager.DisposeClient(clientManager);
            }
        }
        private async Task WriteResponsesToPipe(ClientManager clientManager)
        {
            while (true)
            {
                int totalSize = 0;
                Queue<ResponseBuffers> responsesToWrite = null;

                // Accumulate the size of all the responses in the queue.
                lock (clientManager.queueLock)
                {
                    if (clientManager.ResponseBuffersQueue.Count == 0)
                    {
                        clientManager.ChunkSending = false;
                        break;
                    }
                    responsesToWrite = new Queue<ResponseBuffers>();
                    while (clientManager.ResponseBuffersQueue.Count > 0)
                    {
                        ResponseBuffers responseBuffers = clientManager.ResponseBuffersQueue.Dequeue();
                        totalSize += responseBuffers.Size;
                        responsesToWrite.Enqueue(responseBuffers);
                        clientManager.ResCount--;
                    }
                }

                if (totalSize > 0)
                {
                    //Writer Header
                    WriteHeaderToSpan(totalSize);

                    //WritE REsponses
                    WriteResponsesToPipeSpan(totalSize, responsesToWrite);

                    // Flush data to the client.
                    await _writer.FlushAsync();

                }
            }

        }

        private void WriteHeaderToSpan(int totalSize)
        {
            // Request a span of 10 bytes from the writer.
            Span<byte> span = _writer.GetSpan(10);

            // Convert totalSize to a string.
            string sizeString = totalSize.ToString();
#if NETCORE
            unsafe
            {
                // Get a pointer to the memory span.
                fixed (byte* bPtr = span)
                {
                    // Get a pointer to the string data.
                    fixed (char* cPtr = sizeString)
                    {
                        // Encode the string directly into the memory span using the pointer.
                        int bytesWritten = UTF8Encoding.UTF8.GetBytes(cPtr, sizeString.Length, bPtr, span.Length);

                        // If fewer than 10 bytes were written, fill the remaining space with zeros.
                        for (int i = bytesWritten; i < 10; i++)
                        {
                            bPtr[i] = 0;
                        }
                    }
                }
            }
#endif


            // Advance the writer by 10 bytes to commit the header to the pipeline.
            _writer.Advance(10);
        }

        private void WriteResponsesToPipeSpan(int totalSize, Queue<ResponseBuffers> responsesQueue)
        {
            // Request a block of memory from the PipeWriter.
            Memory<byte> bufferSpan = _writer.GetMemory(totalSize);
            int totalBytes = 0;

            while (responsesQueue.Count > 0)
            {
                ResponseBuffers responseBuffers = responsesQueue.Dequeue();
                foreach (byte[] responseBuffer in responseBuffers.GetBuffer())
                {
                    responseBuffer.CopyTo(bufferSpan.Slice(totalBytes));
                    totalBytes += responseBuffer.Length;
                }
            }

            // Inform the PipeWriter that we've written 'totalBytes' of data.
            _writer.Advance(totalBytes);
        }

#endregion

        public void Dispose()
        {
            try
            {
                _writer.Complete();
                _stream.Dispose();
            }
            catch (Exception ex) { }

        }
    }
}
