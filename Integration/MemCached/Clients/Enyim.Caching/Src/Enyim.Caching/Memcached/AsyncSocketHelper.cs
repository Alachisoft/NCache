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
//#define DEBUG_IO
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	public partial class PooledSocket
	{
		/// <summary>
		/// Supports exactly one reader and writer, but they can do IO concurrently
		/// </summary>
		private class AsyncSocketHelper
		{
			private const int ChunkSize = 65536;

			private PooledSocket socket;
			private SlidingBuffer asyncBuffer;

			private SocketAsyncEventArgs readEvent;
#if DEBUG_IO
			private int doingIO;
#endif
			private int remainingRead;
			private int expectedToRead;
			private AsyncIOArgs pendingArgs;

			private int isAborted;
			private ManualResetEvent readInProgressEvent;

			public AsyncSocketHelper(PooledSocket socket)
			{
				this.socket = socket;
				this.asyncBuffer = new SlidingBuffer(ChunkSize);

				this.readEvent = new SocketAsyncEventArgs();
				this.readEvent.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncReadCompleted);
				this.readEvent.SetBuffer(new byte[ChunkSize], 0, ChunkSize);

				this.readInProgressEvent = new ManualResetEvent(false);
			}

			/// <summary>
			/// returns true if io is pending
			/// </summary>
			/// <param name="p"></param>
			/// <returns></returns>
			public bool Read(AsyncIOArgs p)
			{
				var count = p.Count;
				if (count < 1) throw new ArgumentOutOfRangeException("count", "count must be > 0");
#if DEBUG_IO
				if (Interlocked.CompareExchange(ref this.doingIO, 1, 0) != 0)
					throw new InvalidOperationException("Receive is already in progress");
#endif
				this.expectedToRead = p.Count;
				this.pendingArgs = p;

				p.Fail = false;
				p.Result = null;

				if (this.asyncBuffer.Available >= count)
				{
					PublishResult(false);

					return false;
				}
				else
				{
					this.remainingRead = count - this.asyncBuffer.Available;
					this.isAborted = 0;

					this.BeginReceive();

					return true;
				}
			}

			public void DiscardBuffer()
			{
				this.asyncBuffer.UnsafeClear();
			}

			private void BeginReceive()
			{
				while (this.remainingRead > 0)
				{
					this.readInProgressEvent.Reset();

					if (this.socket.socket.ReceiveAsync(this.readEvent))
					{
						// wait until the timeout elapses, then abort this reading process
						// EndREceive will be triggered sooner or later but its timeout
						// may be higher than our read timeout, so it's not reliable
						if (!readInProgressEvent.WaitOne(this.socket.socket.ReceiveTimeout))
							this.AbortReadAndTryPublishError(false);

						return;
					}

					this.EndReceive();
				}
			}

			void AsyncReadCompleted(object sender, SocketAsyncEventArgs e)
			{
				if (this.EndReceive())
					this.BeginReceive();
			}

			private void AbortReadAndTryPublishError(bool markAsDead)
			{
				if (markAsDead)
					this.socket.isAlive = false;

				// we've been already aborted, so quit
				// both the EndReceive and the wait on the event can abort the read
				// but only one should of them should continue the async call chain
				if (Interlocked.CompareExchange(ref this.isAborted, 1, 0) != 0)
					return;

				this.remainingRead = 0;
				var p = this.pendingArgs;
#if DEBUG_IO
				Thread.MemoryBarrier();

				this.doingIO = 0;
#endif

				p.Fail = true;
				p.Result = null;

				this.pendingArgs.Next(p);
			}

			/// <summary>
			/// returns true when io is pending
			/// </summary>
			/// <returns></returns>
			private bool EndReceive()
			{
				this.readInProgressEvent.Set();

				var read = this.readEvent.BytesTransferred;
				if (this.readEvent.SocketError != SocketError.Success
					|| read == 0)
				{
					this.AbortReadAndTryPublishError(true);//new IOException("Remote end has been closed"));

					return false;
				}

				this.remainingRead -= read;
				this.asyncBuffer.Append(this.readEvent.Buffer, 0, read);

				if (this.remainingRead <= 0)
				{
					this.PublishResult(true);

					return false;
				}

				return true;
			}

			private void PublishResult(bool isAsync)
			{
				var retval = this.pendingArgs;

				var data = new byte[this.expectedToRead];
				this.asyncBuffer.Read(data, 0, retval.Count);
				pendingArgs.Result = data;
#if DEBUG_IO
				Thread.MemoryBarrier();
				this.doingIO = 0;
#endif

				if (isAsync)
					pendingArgs.Next(pendingArgs);
			}
		}
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kisk�, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
