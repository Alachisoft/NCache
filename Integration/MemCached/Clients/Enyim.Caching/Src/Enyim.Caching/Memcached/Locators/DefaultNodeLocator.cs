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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// This is a ketama-like consistent hashing based node locator. Used when no other <see cref="T:IMemcachedNodeLocator"/> is specified for the pool.
	/// </summary>
	public sealed class DefaultNodeLocator : IMemcachedNodeLocator, IDisposable
	{
		private const int ServerAddressMutations = 100;

		// holds all server keys for mapping an item key to the server consistently
		private uint[] keys;
		// used to lookup a server based on its key
		private Dictionary<uint, IMemcachedNode> servers;
		private Dictionary<IMemcachedNode, bool> deadServers;
		private List<IMemcachedNode> allServers;
		private ReaderWriterLockSlim serverAccessLock;

		public DefaultNodeLocator()
		{
			this.servers = new Dictionary<uint, IMemcachedNode>(new UIntEqualityComparer());
			this.deadServers = new Dictionary<IMemcachedNode, bool>();
			this.allServers = new List<IMemcachedNode>();
			this.serverAccessLock = new ReaderWriterLockSlim();
		}

		private void BuildIndex(List<IMemcachedNode> nodes)
		{
			var keys = new uint[nodes.Count * DefaultNodeLocator.ServerAddressMutations];

			int nodeIdx = 0;

			foreach (IMemcachedNode node in nodes)
			{
				var tmpKeys = DefaultNodeLocator.GenerateKeys(node, DefaultNodeLocator.ServerAddressMutations);

				for (var i = 0; i < tmpKeys.Length; i++)
				{
					this.servers[tmpKeys[i]] = node;
				}

				tmpKeys.CopyTo(keys, nodeIdx);
				nodeIdx += DefaultNodeLocator.ServerAddressMutations;
			}

			Array.Sort<uint>(keys);
			Interlocked.Exchange(ref this.keys, keys);
		}

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			this.serverAccessLock.EnterWriteLock();

			try
			{
				this.allServers = nodes.ToList();
				this.BuildIndex(this.allServers);
			}
			finally
			{
				this.serverAccessLock.ExitWriteLock();
			}
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (key == null) throw new ArgumentNullException("key");

			this.serverAccessLock.EnterUpgradeableReadLock();

			try { return this.Locate(key); }
			finally { this.serverAccessLock.ExitUpgradeableReadLock(); }
		}

		IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
		{
			this.serverAccessLock.EnterReadLock();

			try { return this.allServers.Except(this.deadServers.Keys).ToArray(); }
			finally { this.serverAccessLock.ExitReadLock(); }
		}

		private IMemcachedNode Locate(string key)
		{
			var node = FindNode(key);
			if (node == null || node.IsAlive)
				return node;

			// move the current node to the dead list and rebuild the indexes
			this.serverAccessLock.EnterWriteLock();

			try
			{
				// check if it's still dead or it came back
				// while waiting for the write lock
				if (!node.IsAlive)
					this.deadServers[node] = true;

				this.BuildIndex(this.allServers.Except(this.deadServers.Keys).ToList());
			}
			finally
			{
				this.serverAccessLock.ExitWriteLock();
			}

			// try again with the dead server removed from the lists
			return this.Locate(key);
		}

		/// <summary>
		/// locates a node by its key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private IMemcachedNode FindNode(string key)
		{
			if (this.keys.Length == 0) return null;

			uint itemKeyHash = BitConverter.ToUInt32(new FNV1a().ComputeHash(Encoding.UTF8.GetBytes(key)), 0);
			// get the index of the server assigned to this hash
			int foundIndex = Array.BinarySearch<uint>(this.keys, itemKeyHash);

			// no exact match
			if (foundIndex < 0)
			{
				// this is the nearest server in the list
				foundIndex = ~foundIndex;

				if (foundIndex == 0)
				{
					// it's smaller than everything, so use the last server (with the highest key)
					foundIndex = this.keys.Length - 1;
				}
				else if (foundIndex >= this.keys.Length)
				{
					// the key was larger than all server keys, so return the first server
					foundIndex = 0;
				}
			}

			if (foundIndex < 0 || foundIndex > this.keys.Length)
				return null;

			return this.servers[this.keys[foundIndex]];
		}

		private static uint[] GenerateKeys(IMemcachedNode node, int numberOfKeys)
		{
			const int KeyLength = 4;
			const int PartCount = 1; // (ModifiedFNV.HashSize / 8) / KeyLength; // HashSize is in bits, uint is 4 byte long

			var k = new uint[PartCount * numberOfKeys];

			// every server is registered numberOfKeys times
			// using UInt32s generated from the different parts of the hash
			// i.e. hash is 64 bit:
			// 00 00 aa bb 00 00 cc dd
			// server will be stored with keys 0x0000aabb & 0x0000ccdd
			// (or a bit differently based on the little/big indianness of the host)
			string address = node.EndPoint.ToString();
			var fnv = new FNV1a();

			for (int i = 0; i < numberOfKeys; i++)
			{
				byte[] data = fnv.ComputeHash(Encoding.ASCII.GetBytes(String.Concat(address, "-", i)));

				for (int h = 0; h < PartCount; h++)
				{
					k[i * PartCount + h] = BitConverter.ToUInt32(data, h * KeyLength);
				}
			}

			return k;
		}

		#region [ IDisposable                  ]

		void IDisposable.Dispose()
		{
			using (this.serverAccessLock)
			{
				this.serverAccessLock.EnterWriteLock();

				try
				{
					// kill all pending operations (with an exception)
					// it's not nice, but disposeing an instance while being used is bad practice
					this.allServers = null;
					this.servers = null;
					this.keys = null;
					this.deadServers = null;
				}
				finally
				{
					this.serverAccessLock.ExitWriteLock();
				}
			}

			this.serverAccessLock = null;
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
