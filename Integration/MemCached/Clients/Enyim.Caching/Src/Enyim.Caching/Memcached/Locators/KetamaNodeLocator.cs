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
using System.Security.Cryptography;
using System.Threading;
using System.Web;

namespace Enyim.Caching.Memcached
{
	/// <summary>
	/// Implements Ketama cosistent hashing, compatible with the "spymemcached" Java client
	/// </summary>
	public sealed class KetamaNodeLocator : IMemcachedNodeLocator
	{
		// TODO make this configurable without restructuring the whole config system
		private const string DefaultHashName = "System.Security.Cryptography.MD5";
		private const int ServerAddressMutations = 160;
		private LookupData lookupData;
		private string hashName;
		private Func<HashAlgorithm> factory;

		/// <summary>
		/// Initialized a new instance of the <see cref="KetamaNodeLocator"/> using the default hash algorithm.
		/// </summary>
		public KetamaNodeLocator() : this(DefaultHashName) { }

		/// <summary>
		/// Initialized a new instance of the <see cref="KetamaNodeLocator"/> using a custom hash algorithm.
		/// </summary>
		/// <param name="hashName">The name of the hash algorithm to use.
		/// <list type="table">
		/// <listheader><term>Name</term><description>Description</description></listheader>
		/// <item><term>md5</term><description>Equivalent of System.Security.Cryptography.MD5</description></item>
		/// <item><term>sha1</term><description>Equivalent of System.Security.Cryptography.SHA1</description></item>
		/// <item><term>tiger</term><description>Tiger Hash</description></item>
		/// <item><term>crc</term><description>CRC32</description></item>
		/// <item><term>fnv1_32</term><description>FNV Hash 32bit</description></item>
		/// <item><term>fnv1_64</term><description>FNV Hash 64bit</description></item>
		/// <item><term>fnv1a_32</term><description>Modified FNV Hash 32bit</description></item>
		/// <item><term>fnv1a_64</term><description>Modified FNV Hash 64bit</description></item>
		/// <item><term>murmur</term><description>Murmur Hash</description></item>
		/// <item><term>oneatatime</term><description>Jenkin's "One at A time Hash"</description></item>
		/// </list>
		/// </param>
		/// <remarks>If the hashName does not match any of the item on the list it will be passed to HashAlgorithm.Create.</remarks>
		public KetamaNodeLocator(string hashName)
		{
			this.hashName = hashName ?? DefaultHashName;

			if (!hashFactory.TryGetValue(this.hashName, out this.factory))
				this.factory = () => HashAlgorithm.Create(this.hashName);
		}

		void IMemcachedNodeLocator.Initialize(IList<IMemcachedNode> nodes)
		{
			// quit if we've been initialized because we can handle dead nodes,
			// so there is no need to recalculate everything
			if (this.lookupData != null) return;

			// sizeof(uint)
			const int KeyLength = 4;
			var hashAlgo = this.factory();

			int PartCount = hashAlgo.HashSize / 8 / KeyLength; // HashSize is in bits, uint is 4 bytes long
			if (PartCount < 1) throw new ArgumentOutOfRangeException("The hash algorithm must provide at least 32 bits long hashes");

			var keys = new List<uint>(nodes.Count * KetamaNodeLocator.ServerAddressMutations);
			var keyToServer = new Dictionary<uint, IMemcachedNode>(keys.Count, new UIntEqualityComparer());

			for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
			{
				var currentNode = nodes[nodeIndex];

				// every server is registered numberOfKeys times
				// using UInt32s generated from the different parts of the hash
				// i.e. hash is 64 bit:
				// 01 02 03 04 05 06 07
				// server will be stored with keys 0x07060504 & 0x03020100
				string address = currentNode.EndPoint.ToString();

				for (int mutation = 0; mutation < ServerAddressMutations / PartCount; mutation++)
				{
					byte[] data = hashAlgo.ComputeHash(Encoding.ASCII.GetBytes(address + "-" + mutation));

					for (int p = 0; p < PartCount; p++)
					{
						var tmp = p * 4;
						var key = ((uint)data[tmp + 3] << 24)
									| ((uint)data[tmp + 2] << 16)
									| ((uint)data[tmp + 1] << 8)
									| ((uint)data[tmp]);

						keys.Add(key);
						keyToServer[key] = currentNode;
					}
				}
			}

			keys.Sort();

			var lookupData = new LookupData
			{
				keys = keys.ToArray(),
				KeyToServer = keyToServer,
				Servers = nodes.ToArray()
			};

			Interlocked.Exchange(ref this.lookupData, lookupData);
		}

		private uint GetKeyHash(string key)
		{
			var hashAlgo = this.factory();
			var uintHash = hashAlgo as IUIntHashAlgorithm;

			var keyData = Encoding.UTF8.GetBytes(key);

			// shortcut for internal hash algorithms
			if (uintHash == null)
			{
				var data = hashAlgo.ComputeHash(keyData);

				return ((uint)data[3] << 24) | ((uint)data[2] << 16) | ((uint)data[1] << 8) | ((uint)data[0]);
			}
			else
			{
				return uintHash.ComputeHash(keyData);
			}
		}

		IMemcachedNode IMemcachedNodeLocator.Locate(string key)
		{
			if (key == null) throw new ArgumentNullException("key");

			var ld = this.lookupData;

			switch (ld.Servers.Length)
			{
				case 0: return null;
				case 1: var tmp = ld.Servers[0]; return tmp.IsAlive ? tmp : null;
			}

			var retval = LocateNode(ld, this.GetKeyHash(key));

			// if the result is not alive then try to mutate the item key and 
			// find another node this way we do not have to reinitialize every 
			// time a node dies/comes back
			// (DefaultServerPool will resurrect the nodes in the background without affecting the hashring)
			if (!retval.IsAlive)
			{
				for (var i = 0; i < ld.Servers.Length; i++)
				{
					// -- this is from spymemcached so we select the same node for the same items
					ulong tmpKey = (ulong)GetKeyHash(i + key);
					tmpKey += (uint)(tmpKey ^ (tmpKey >> 32));
					tmpKey &= 0xffffffffL; /* truncate to 32-bits */
					// -- end

					retval = LocateNode(ld, (uint)tmpKey);

					if (retval.IsAlive) return retval;
				}
			}

			return retval.IsAlive ? retval : null;
		}

		IEnumerable<IMemcachedNode> IMemcachedNodeLocator.GetWorkingNodes()
		{
			var ld = this.lookupData;

			if (ld.Servers == null || ld.Servers.Length == 0)
				return Enumerable.Empty<IMemcachedNode>();

			var retval = new IMemcachedNode[ld.Servers.Length];
			Array.Copy(ld.Servers, retval, retval.Length);

			return retval;
		}

		private static IMemcachedNode LocateNode(LookupData ld, uint itemKeyHash)
		{
			// get the index of the server assigned to this hash
			int foundIndex = Array.BinarySearch<uint>(ld.keys, itemKeyHash);

			// no exact match
			if (foundIndex < 0)
			{
				// this is the nearest server in the list
				foundIndex = ~foundIndex;

				if (foundIndex == 0)
				{
					// it's smaller than everything, so use the last server (with the highest key)
					foundIndex = ld.keys.Length - 1;
				}
				else if (foundIndex >= ld.keys.Length)
				{
					// the key was larger than all server keys, so return the first server
					foundIndex = 0;
				}
			}

			if (foundIndex < 0 || foundIndex > ld.keys.Length)
				return null;

			return ld.KeyToServer[ld.keys[foundIndex]];
		}

		#region [ LookupData                   ]
		/// <summary>
		/// this will encapsulate all the indexes we need for lookup
		/// so the instance can be reinitialized without locking
		/// in case an IMemcachedConfig implementation returns the same instance form the CreateLocator()
		/// </summary>
		private class LookupData
		{
			public IMemcachedNode[] Servers;
			// holds all server keys for mapping an item key to the server consistently
			public uint[] keys;
			// used to lookup a server based on its key
			public Dictionary<uint, IMemcachedNode> KeyToServer;
		}
		#endregion
		#region [ hashFactory                  ]

		private static readonly Dictionary<string, Func<HashAlgorithm>> hashFactory = new Dictionary<string, Func<HashAlgorithm>>(StringComparer.OrdinalIgnoreCase)
		{
		    { String.Empty, () => MD5.Create() },
		    { "default", () => MD5.Create() },
			{ "md5", () => MD5.Create() },
			{ "sha1", () => SHA1.Create() },
			{ "tiger", () => new TigerHash() },
		    { "crc", () => new HashkitCrc32() },
		    { "fnv1_32", () => new Enyim.FNV1() },
		    { "fnv1_64", () => new Enyim.FNV1a() },
		    { "fnv1a_32", () => new Enyim.FNV64() },
		    { "fnv1a_64", () => new Enyim.FNV64a() },
		    { "murmur", () => new HashkitMurmur() },
		    { "oneatatime", () => new HashkitOneAtATime() }
		};

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
