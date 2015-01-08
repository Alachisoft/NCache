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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Enyim.Caching.Configuration;
using Enyim.Collections;
using System.Security;
using Enyim.Caching.Memcached.Protocol.Binary;
using System.Runtime.Serialization;
using System.IO;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Represents a Memcached node in the pool.
	/// </summary>
	[DebuggerDisplay("{{MemcachedNode [ Address: {EndPoint}, IsAlive = {IsAlive} ]}}")]
	public class MemcachedNode : IMemcachedNode
	{
		private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(MemcachedNode));
		private static readonly object SyncRoot = new Object();

		private bool isDisposed;

		private IPEndPoint endPoint;
		private ISocketPoolConfiguration config;
		private InternalPoolImpl internalPoolImpl;
		private bool isInitialized;

		public MemcachedNode(IPEndPoint endpoint, ISocketPoolConfiguration socketPoolConfig)
		{
			this.endPoint = endpoint;
			this.config = socketPoolConfig;

			if (socketPoolConfig.ConnectionTimeout.TotalMilliseconds >= Int32.MaxValue)
				throw new InvalidOperationException("ConnectionTimeout must be < Int32.MaxValue");

			this.internalPoolImpl = new InternalPoolImpl(this, socketPoolConfig);
		}

		public event Action<IMemcachedNode> Failed;
		private INodeFailurePolicy failurePolicy;

		protected INodeFailurePolicy FailurePolicy
		{
			get { return this.failurePolicy ?? (this.failurePolicy = this.config.FailurePolicyFactory.Create(this)); }
		}

		/// <summary>
		/// Gets the <see cref="T:IPEndPoint"/> of this instance
		/// </summary>
		public IPEndPoint EndPoint
		{
			get { return this.endPoint; }
		}

		/// <summary>
		/// <para>Gets a value indicating whether the server is working or not. Returns a <b>cached</b> state.</para>
		/// <para>To get real-time information and update the cached state, use the <see cref="M:Ping"/> method.</para>
		/// </summary>
		/// <remarks>Used by the <see cref="T:ServerPool"/> to quickly check if the server's state is valid.</remarks>
		public bool IsAlive
		{
			get { return this.internalPoolImpl.IsAlive; }
		}

		/// <summary>
		/// Gets a value indicating whether the server is working or not.
		/// 
		/// If the server is back online, we'll ercreate the internal socket pool and mark the server as alive so operations can target it.
		/// </summary>
		/// <returns>true if the server is alive; false otherwise.</returns>
		public bool Ping()
		{
			// is the server working?
			if (this.internalPoolImpl.IsAlive)
				return true;

			// this codepath is (should be) called very rarely
			// if you get here hundreds of times then you have bigger issues
			// and try to make the memcached instaces more stable and/or increase the deadTimeout
			try
			{
				// we could connect to the server, let's recreate the socket pool
				lock (SyncRoot)
				{
					if (this.isDisposed) return false;

					// try to connect to the server
					using (var socket = this.CreateSocket()) ;

					if (this.internalPoolImpl.IsAlive)
						return true;

					// it's easier to create a new pool than reinitializing a dead one
					// rewrite-then-dispose to avoid a race condition with Acquire (which does no locking)
					var oldPool = this.internalPoolImpl;
					var newPool = new InternalPoolImpl(this, this.config);

					Interlocked.Exchange(ref this.internalPoolImpl, newPool);

					try { oldPool.Dispose(); }
					catch { }
				}

				return true;
			}
			//could not reconnect
			catch { return false; }
		}

		/// <summary>
		/// Acquires a new item from the pool
		/// </summary>
		/// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
		public IPooledSocketResult Acquire()
		{
			if (!this.isInitialized)
				lock (this.internalPoolImpl)
					if (!this.isInitialized)
					{
						this.internalPoolImpl.InitPool();
						this.isInitialized = true;
					}

			try
			{
				return this.internalPoolImpl.Acquire();
			}
			catch (Exception e)
			{
				var message = "Acquire failed. Maybe we're already disposed?";
				log.Error(message, e);
				var result = new PooledSocketResult();
				result.Fail(message, e);
				return result;
			}
		}

		~MemcachedNode()
		{
			try { ((IDisposable)this).Dispose(); }
			catch { }
		}

		/// <summary>
		/// Releases all resources allocated by this instance
		/// </summary>
		public void Dispose()
		{
			if (this.isDisposed) return;

			GC.SuppressFinalize(this);

			// this is not a graceful shutdown
			// if someone uses a pooled item then it's 99% that an exception will be thrown
			// somewhere. But since the dispose is mostly used when everyone else is finished
			// this should not kill any kittens
			lock (SyncRoot)
			{
				if (this.isDisposed) return;

				this.isDisposed = true;
				this.internalPoolImpl.Dispose();
			}
		}

		void IDisposable.Dispose()
		{
			this.Dispose();
		}

		#region [ InternalPoolImpl             ]

		private class InternalPoolImpl : IDisposable
		{
			private static readonly Enyim.Caching.ILog log = Enyim.Caching.LogManager.GetLogger(typeof(InternalPoolImpl).FullName.Replace("+", "."));

			/// <summary>
			/// A list of already connected but free to use sockets
			/// </summary>
			private InterlockedStack<PooledSocket> freeItems;

			private bool isDisposed;
			private bool isAlive;
			private DateTime markedAsDeadUtc;

			private int minItems;
			private int maxItems;

			private MemcachedNode ownerNode;
			private IPEndPoint endPoint;
			private TimeSpan queueTimeout;
			private Semaphore semaphore;

			private object initLock = new Object();

			internal InternalPoolImpl(MemcachedNode ownerNode, ISocketPoolConfiguration config)
			{
				if (config.MinPoolSize < 0)
					throw new InvalidOperationException("minItems must be larger >= 0", null);
				if (config.MaxPoolSize < config.MinPoolSize)
					throw new InvalidOperationException("maxItems must be larger than minItems", null);
				if (config.QueueTimeout < TimeSpan.Zero)
					throw new InvalidOperationException("queueTimeout must be >= TimeSpan.Zero", null);

				this.ownerNode = ownerNode;
				this.isAlive = true;
				this.endPoint = ownerNode.EndPoint;
				this.queueTimeout = config.QueueTimeout;

				this.minItems = config.MinPoolSize;
				this.maxItems = config.MaxPoolSize;

				this.semaphore = new Semaphore(maxItems, maxItems);
				this.freeItems = new InterlockedStack<PooledSocket>();
			}

			internal void InitPool()
			{
				try
				{
					if (this.minItems > 0)
					{
						for (int i = 0; i < this.minItems; i++)
						{
							this.freeItems.Push(this.CreateSocket());

							// cannot connect to the server
							if (!this.isAlive)
								break;
						}
					}

					if (log.IsDebugEnabled)
						log.DebugFormat("Pool has been inited for {0} with {1} sockets", this.endPoint, this.minItems);

				}
				catch (Exception e)
				{
					log.Error("Could not init pool.", e);

					this.MarkAsDead();
				}
			}

			private PooledSocket CreateSocket()
			{
				var ps = this.ownerNode.CreateSocket();
				ps.CleanupCallback = this.ReleaseSocket;

				return ps;
			}

			public bool IsAlive
			{
				get { return this.isAlive; }
			}

			public DateTime MarkedAsDeadUtc
			{
				get { return this.markedAsDeadUtc; }
			}

			/// <summary>
			/// Acquires a new item from the pool
			/// </summary>
			/// <returns>An <see cref="T:PooledSocket"/> instance which is connected to the memcached server, or <value>null</value> if the pool is dead.</returns>
			public IPooledSocketResult Acquire()
			{
				var result = new PooledSocketResult();
				var message = string.Empty;

				bool hasDebug = log.IsDebugEnabled;

				if (hasDebug) log.Debug("Acquiring stream from pool. " + this.endPoint);

				if (!this.isAlive || this.isDisposed)
				{
					message = "Pool is dead or disposed, returning null. " + this.endPoint;
					result.Fail(message);
					
					if (hasDebug) log.Debug(message);

					return result;
				}

				PooledSocket retval = null;

				if (!this.semaphore.WaitOne(this.queueTimeout))
				{
					message = "Pool is full, timeouting. " + this.endPoint;
					if (hasDebug) log.Debug(message);
					result.Fail(message, new TimeoutException());

					// everyone is so busy
					return result;
				}

				// maybe we died while waiting
				if (!this.isAlive)
				{
					message = "Pool is dead, returning null. " + this.endPoint;
					if (hasDebug) log.Debug(message);
					result.Fail(message);

					return result;
				}

				// do we have free items?
				if (this.freeItems.TryPop(out retval))
				{
					#region [ get it from the pool         ]

					try
					{
						retval.Reset();

						message = "Socket was reset. " + retval.InstanceId;
						if (hasDebug) log.Debug(message);

						result.Pass(message);
						result.Value = retval;
						return result;
					}
					catch (Exception e)
					{
						message = "Failed to reset an acquired socket.";
						log.Error(message, e);

						this.MarkAsDead();
						result.Fail(message, e);
						return result;
					}

					#endregion
				}

				// free item pool is empty
				message = "Could not get a socket from the pool, Creating a new item. " + this.endPoint;
				if (hasDebug) log.Debug(message);
				

				try
				{
					// okay, create the new item
					retval = this.CreateSocket();
					result.Value = retval;
					result.Pass();
				}
				catch (Exception e)
				{
					message = "Failed to create socket. " + this.endPoint;
					log.Error(message, e);

					// eventhough this item failed the failure policy may keep the pool alive
					// so we need to make sure to release the semaphore, so new connections can be
					// acquired or created (otherwise dead conenctions would "fill up" the pool
					// while the FP pretends that the pool is healthy)
					semaphore.Release();

					this.MarkAsDead();
					result.Fail(message);
					return result;
				}

				if (hasDebug) log.Debug("Done.");

				return result;
			}

			private void MarkAsDead()
			{
				if (log.IsDebugEnabled) log.DebugFormat("Mark as dead was requested for {0}", this.endPoint);

				var shouldFail = ownerNode.FailurePolicy.ShouldFail();

				if (log.IsDebugEnabled) log.Debug("FailurePolicy.ShouldFail(): " + shouldFail);

				if (shouldFail)
				{
					if (log.IsWarnEnabled) log.WarnFormat("Marking node {0} as dead", this.endPoint);

					this.isAlive = false;
					this.markedAsDeadUtc = DateTime.UtcNow;

					var f = this.ownerNode.Failed;

					if (f != null)
						f(this.ownerNode);
				}
			}

			/// <summary>
			/// Releases an item back into the pool
			/// </summary>
			/// <param name="socket"></param>
			private void ReleaseSocket(PooledSocket socket)
			{
				if (log.IsDebugEnabled)
				{
					log.Debug("Releasing socket " + socket.InstanceId);
					log.Debug("Are we alive? " + this.isAlive);
				}

				if (this.isAlive)
				{
					// is it still working (i.e. the server is still connected)
					if (socket.IsAlive)
					{
						// mark the item as free
						this.freeItems.Push(socket);

						// signal the event so if someone is waiting for it can reuse this item
						this.semaphore.Release();
					}
					else
					{
						// kill this item
						socket.Destroy();

						// mark ourselves as not working for a while
						this.MarkAsDead();

						// make sure to signal the Acquire so it can create a new conenction
						// if the failure policy keeps the pool alive
						this.semaphore.Release();
					}
				}
				else
				{
					// one of our previous sockets has died, so probably all of them 
					// are dead. so, kill the socket (this will eventually clear the pool as well)
					socket.Destroy();
				}
			}


			~InternalPoolImpl()
			{
				try { ((IDisposable)this).Dispose(); }
				catch { }
			}

			/// <summary>
			/// Releases all resources allocated by this instance
			/// </summary>
			public void Dispose()
			{
				// this is not a graceful shutdown
				// if someone uses a pooled item then 99% that an exception will be thrown
				// somewhere. But since the dispose is mostly used when everyone else is finished
				// this should not kill any kittens
				if (!this.isDisposed)
				{
					this.isAlive = false;
					this.isDisposed = true;

					PooledSocket ps;

					while (this.freeItems.TryPop(out ps))
					{
						try { ps.Destroy(); }
						catch { }
					}

					this.ownerNode = null;
					this.semaphore.Close();
					this.semaphore = null;
					this.freeItems = null;
				}
			}

			void IDisposable.Dispose()
			{
				this.Dispose();
			}
		}

		#endregion
		#region [ Comparer                     ]
		internal sealed class Comparer : IEqualityComparer<IMemcachedNode>
		{
			public static readonly Comparer Instance = new Comparer();

			bool IEqualityComparer<IMemcachedNode>.Equals(IMemcachedNode x, IMemcachedNode y)
			{
				return x.EndPoint.Equals(y.EndPoint);
			}

			int IEqualityComparer<IMemcachedNode>.GetHashCode(IMemcachedNode obj)
			{
				return obj.EndPoint.GetHashCode();
			}
		}
		#endregion

		protected internal virtual PooledSocket CreateSocket()
		{
			return new PooledSocket(this.endPoint, this.config.ConnectionTimeout, this.config.ReceiveTimeout);
		}

		//protected internal virtual PooledSocket CreateSocket(IPEndPoint endpoint, TimeSpan connectionTimeout, TimeSpan receiveTimeout)
		//{
		//    PooledSocket retval = new PooledSocket(endPoint, connectionTimeout, receiveTimeout);

		//    return retval;
		//}

		protected virtual IPooledSocketResult ExecuteOperation(IOperation op)
		{
			var result = this.Acquire();
			if (result.Success && result.HasValue)
			{
				try
				{
					var socket = result.Value;
					var b = op.GetBuffer();

					socket.Write(b);

					var readResult = op.ReadResponse(socket);
					if (readResult.Success)
					{
						result.Pass();
					}
					else
					{
						readResult.Combine(result);
					}
					return result;
				}
				catch (IOException e)
				{
					log.Error(e);

					result.Fail("Exception reading response", e);
					return result;
				}
				finally
				{
					((IDisposable)result.Value).Dispose();
				}
			}
			else
			{
				result.Fail("Failed to obtain socket from pool");
				return result;
			}

		}

		protected virtual bool ExecuteOperationAsync(IOperation op, Action<bool> next)
		{
			var socket = this.Acquire().Value;
			if (socket == null) return false;

			var b = op.GetBuffer();

			try
			{
				socket.Write(b);

				var rrs = op.ReadResponseAsync(socket, readSuccess =>
				{
					((IDisposable)socket).Dispose();

					next(readSuccess);
				});

				return rrs;
			}
			catch (IOException e)
			{
				log.Error(e);
				((IDisposable)socket).Dispose();

				return false;
			}
		}

		#region [ IMemcachedNode               ]

		IPEndPoint IMemcachedNode.EndPoint
		{
			get { return this.EndPoint; }
		}

		bool IMemcachedNode.IsAlive
		{
			get { return this.IsAlive; }
		}

		bool IMemcachedNode.Ping()
		{
			return this.Ping();
		}

		IOperationResult IMemcachedNode.Execute(IOperation op)
		{
			return this.ExecuteOperation(op);
		}

		bool IMemcachedNode.ExecuteAsync(IOperation op, Action<bool> next)
		{
			return this.ExecuteOperationAsync(op, next);
		}

		event Action<IMemcachedNode> IMemcachedNode.Failed
		{
			add { this.Failed += value; }
			remove { this.Failed -= value; }
		}

		#endregion
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
