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
//Copyright (c) 2007-2008 Henrik Schr�der, Oliver Kofoed Pedersen

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Text;
using Alachisoft.NCache.Integrations.Memcached.Provider;
using Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions;
using System.Collections;
namespace BeIT.MemCached{
	/// <summary>
	/// Memcached client main class.
	/// Use the static methods Setup and GetInstance to setup and get an instance of the client for use.
	/// </summary>
	public class MemcachedClient :IDisposable{
		#region Static fields and methods.
		private static Dictionary<string, MemcachedClient> instances = new Dictionary<string, MemcachedClient>();
		private static LogAdapter logger = LogAdapter.GetLogger(typeof(MemcachedClient));

		/// <summary>
		/// Static method for creating an instance. This method will throw an exception if the name already exists.
		/// </summary>
		/// <param name="name">The name of the instance.</param>
		/// <param name="servers">A list of memcached servers in standard notation: host:port. 
		/// If port is omitted, the default value of 11211 is used. 
		/// Both IP addresses and host names are accepted, for example:
		/// "localhost", "127.0.0.1", "cache01.example.com:12345", "127.0.0.1:12345", etc.</param>
		public static void Setup(string name, string[] servers) {
			if (instances.ContainsKey(name)) {
				throw new ConfigurationErrorsException("Trying to configure MemcachedClient instance \"" + name + "\" twice.");
			}
			instances[name] = new MemcachedClient(name, servers);
		}

		/// <summary>
		/// Static method which checks if a given named MemcachedClient instance exists.
		/// </summary>
		/// <param name="name">The name of the instance.</param>
		/// <returns></returns>
		public static bool Exists(string name) {
			return instances.ContainsKey(name);
		}

		/// <summary>
		/// Static method for getting the default instance named "default".
		/// </summary>
		private static MemcachedClient defaultInstance = null;
		public static MemcachedClient GetInstance() {
			return defaultInstance ?? (defaultInstance = GetInstance("default"));
		}

		/// <summary>
		/// Static method for getting an instance. 
		/// This method will first check for named instances that has been set up programmatically.
		/// If no such instance exists, it will check the "beitmemcached" section of the standard 
		/// config file and see if it can find configuration info for it there.
		/// If that also fails, an exception is thrown.
		/// </summary>
		/// <param name="name">The name of the instance.</param>
		/// <returns>The named instance.</returns>
		public static MemcachedClient GetInstance(string name) {
			MemcachedClient c;
			if (instances.TryGetValue(name, out c)) {
				return c;
			} else {
				NameValueCollection config = ConfigurationManager.GetSection("beitmemcached") as NameValueCollection;
				if (config != null && !String.IsNullOrEmpty(config.Get(name))) {
					Setup(name, config.Get(name).Split(new char[] { ',' }));
					return GetInstance(name);
				}
				throw new ConfigurationErrorsException("Unable to find MemcachedClient instance \"" + name + "\".");
			}
		}
		#endregion

		#region Fields, constructors, and private methods.
		public readonly string Name;
		private readonly ServerPool serverPool;
        private IMemcachedProvider memcachedProvider;
		/// <summary>
		/// If you specify a key prefix, it will be appended to all keys before they are sent to the memcached server.
		/// They key prefix is not used when calculating which server a key belongs to.
		/// </summary>
		public string KeyPrefix { get { return keyPrefix; } set { keyPrefix = value; } }
		private string keyPrefix = "";

		/// <summary>
		/// The send receive timeout is used to determine how long the client should wait for data to be sent 
		/// and received from the server, specified in milliseconds. The default value is 2000.
		/// </summary>
		public int SendReceiveTimeout { get { return serverPool.SendReceiveTimeout; } set { serverPool.SendReceiveTimeout = value; } }

		/// <summary>
		/// The connect timeout is used to determine how long the client should wait for a connection to be established,
		/// specified in milliseconds. The default value is 2000.
		/// </summary>
		public int ConnectTimeout { get { return serverPool.ConnectTimeout; } set { serverPool.ConnectTimeout = value; } }

		/// <summary>
		/// The min pool size determines the number of sockets the socket pool will keep.
		/// Note that no sockets will be created on startup, only on use, so the socket pool will only
		/// contain this amount of sockets if the amount of simultaneous requests goes above it.
		/// The default value is 5.
		/// </summary>
		public uint MinPoolSize { 
			get { return serverPool.MinPoolSize; } 
			set {
				if (value > MaxPoolSize) { throw new ConfigurationErrorsException("MinPoolSize (" + value + ") may not be larger than the MaxPoolSize (" + MaxPoolSize + ")."); }
				serverPool.MinPoolSize = value;
			} 
		}

		/// <summary>
		/// The max pool size determines how large the socket connection pool is allowed to grow.
		/// There can be more sockets in use than this amount, but when the extra sockets are returned, they will be destroyed.
		/// The default value is 10.
		/// </summary>
		public uint MaxPoolSize {
			get { return serverPool.MaxPoolSize; } 
			set {
				if (value < MinPoolSize) { throw new ConfigurationErrorsException("MaxPoolSize (" + value + ") may not be smaller than the MinPoolSize (" + MinPoolSize + ")."); }
				serverPool.MaxPoolSize = value;
			}
		}
		
		/// <summary>
		/// If the pool contains more than the minimum amount of sockets, and a socket is returned that is older than this recycle age
		/// that socket will be destroyed instead of put back in the pool. This allows the pool to shrink back to the min pool size after a peak in usage.
		/// The default value is 30 minutes.
		/// </summary>
		public TimeSpan SocketRecycleAge { get { return serverPool.SocketRecycleAge; } set { serverPool.SocketRecycleAge = value; } }

		private uint compressionThreshold = 1024*128; //128kb
		/// <summary>
		/// If an object being stored is larger in bytes than the compression threshold, it will internally be compressed before begin stored,
		/// and it will transparently be decompressed when retrieved. Only strings, byte arrays and objects can be compressed.
		/// The default value is 1048576 bytes = 1MB.
		/// </summary>
		public uint CompressionThreshold { get { return compressionThreshold; } set { compressionThreshold = value; } }

		//Private constructor
		private MemcachedClient(string name, string[] hosts) {
			if (String.IsNullOrEmpty(name)) {
				throw new ConfigurationErrorsException("Name of MemcachedClient instance cannot be empty.");
			}
            Name = name;
            serverPool = new ServerPool(hosts);
            string cacheName = "";
            try
            {
                cacheName = ConfigurationManager.AppSettings["NCache.CacheName"];
                if (String.IsNullOrEmpty(cacheName))
                {
                    Exception ex = new Exception("Unable to read NCache Name from configuration");
                    throw ex;
                }
            }
            catch (Exception e)
            { throw e; }

            try
            {
                memcachedProvider = CacheFactory.CreateCacheProvider(cacheName);
            }
            catch (Exception e)
            { }
            
		}

		/// <summary>
		/// Private key hashing method that uses the modified FNV hash.
		/// </summary>
		/// <param name="key">The key to hash.</param>
		/// <returns>The hashed key.</returns>
		private uint hash(string key) {
			checkKey(key);
			return BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(Encoding.UTF8.GetBytes(key)), 0);
		}

		/// <summary>
		/// Private hashing method for user-supplied hash values.
		/// </summary>
		/// <param name="hashvalue">The user-supplied hash value to hash.</param>
		/// <returns>The hashed value</returns>
		private uint hash(uint hashvalue) {
			return BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(BitConverter.GetBytes(hashvalue)), 0);
		}

		/// <summary>
		/// Private multi-hashing method.
		/// </summary>
		/// <param name="keys">An array of keys to hash.</param>
		/// <returns>An arrays of hashes.</returns>
		private uint[] hash(string[] keys) {
			uint[] result = new uint[keys.Length];
			for (int i = 0; i < keys.Length; i++) {
				result[i] = hash(keys[i]);
			}
			return result;
		}

		/// <summary>
		/// Private multi-hashing method for user-supplied hash values.
		/// </summary>
		/// <param name="hashvalues">An array of keys to hash.</param>
		/// <returns>An arrays of hashes.</returns>
		private uint[] hash(uint[] hashvalues) {
			uint[] result = new uint[hashvalues.Length];
			for (int i = 0; i < hashvalues.Length; i++) {
				result[i] = hash(hashvalues[i]);
			}
			return result;
		}

		/// <summary>
		/// Private key-checking method.
		/// Throws an exception if the key does not conform to memcached protocol requirements:
		/// It may not contain whitespace, it may not be null or empty, and it may not be longer than 250 characters.
		/// </summary>
		/// <param name="key">The key to check.</param>
		private void checkKey(string key) {
			if (key == null) {
				throw new ArgumentNullException("Key may not be null.");
			}
			if (key.Length == 0) {
				throw new ArgumentException("Key may not be empty.");
			}
			if (key.Length > 250) {
				throw new ArgumentException("Key may not be longer than 250 characters.");
			}
			foreach (char c in key) {
				if (c <= 32) {
					throw new ArgumentException("Key may not contain whitespace or control characters.");
				}
			}
		}

		private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static int getUnixTime(DateTime datetime) {
			return (int)(datetime.ToUniversalTime() - epoch).TotalSeconds;
		}
		#endregion

		#region Set, Add, and Replace.
		/// <summary>
		/// This method corresponds to the "set" command in the memcached protocol. 
		/// It will unconditionally set the given key to the given value.
		/// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
		/// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
		/// This method returns true if the value was successfully set.
		/// </summary>
		public bool Set(string key, object value) { return store("set", key, true, value, hash(key), 0); }
		public bool Set(string key, object value, uint hash) { return store("set", key, false, value, this.hash(hash), 0); }
		public bool Set(string key, object value, TimeSpan expiry) { return store("set", key, true, value, hash(key), (int)expiry.TotalSeconds); }
		public bool Set(string key, object value, uint hash, TimeSpan expiry) { return store("set", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
		public bool Set(string key, object value, DateTime expiry) { return store("set", key, true, value, hash(key), getUnixTime(expiry)); }
		public bool Set(string key, object value, uint hash, DateTime expiry) { return store("set", key, false, value, this.hash(hash), getUnixTime(expiry)); }

		/// <summary>
		/// This method corresponds to the "add" command in the memcached protocol. 
		/// It will set the given key to the given value only if the key does not already exist.
		/// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
		/// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
		/// This method returns true if the value was successfully added.
		/// </summary>
		public bool Add(string key, object value) { return store("add", key, true, value, hash(key), 0); }
		public bool Add(string key, object value, uint hash) { return store("add", key, false, value, this.hash(hash), 0); }
		public bool Add(string key, object value, TimeSpan expiry) { return store("add", key, true, value, hash(key), (int)expiry.TotalSeconds); }
		public bool Add(string key, object value, uint hash, TimeSpan expiry) { return store("add", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
		public bool Add(string key, object value, DateTime expiry) { return store("add", key, true, value, hash(key), getUnixTime(expiry)); }
		public bool Add(string key, object value, uint hash, DateTime expiry) { return store("add", key, false, value, this.hash(hash), getUnixTime(expiry)); }

		/// <summary>
		/// This method corresponds to the "replace" command in the memcached protocol. 
		/// It will set the given key to the given value only if the key already exists.
		/// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
		/// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
		/// This method returns true if the value was successfully replaced.
		/// </summary>
		public bool Replace(string key, object value) { return store("replace", key, true, value, hash(key), 0); }
		public bool Replace(string key, object value, uint hash) { return store("replace", key, false, value, this.hash(hash), 0); }
		public bool Replace(string key, object value, TimeSpan expiry) { return store("replace", key, true, value, hash(key), (int)expiry.TotalSeconds); }
		public bool Replace(string key, object value, uint hash, TimeSpan expiry) { return store("replace", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
		public bool Replace(string key, object value, DateTime expiry) { return store("replace", key, true, value, hash(key), getUnixTime(expiry)); }
		public bool Replace(string key, object value, uint hash, DateTime expiry) { return store("replace", key, false, value, this.hash(hash), getUnixTime(expiry)); }

		/// <summary>
		/// This method corresponds to the "append" command in the memcached protocol.
		/// It will append the given value to the given key, if the key already exists.
		/// Modifying a key with this command will not change its expiry time.
		/// Using the overload it is possible to specify a custom hash to override server selection.
		/// </summary>
		public bool Append(string key, object value) { return store("append", key, true, value, hash(key)); }
		public bool Append(string key, object value, uint hash) { return store("append", key, false, value, this.hash(hash)); }

		/// <summary>
		/// This method corresponds to the "prepend" command in the memcached protocol.
		/// It will prepend the given value to the given key, if the key already exists.
		/// Modifying a key with this command will not change its expiry time.
		/// Using the overload it is possible to specify a custom hash to override server selection.
		/// </summary>
		public bool Prepend(string key, object value) { return store("prepend", key, true, value, hash(key)); }
		public bool Prepend(string key, object value, uint hash) { return store("prepend", key, false, value, this.hash(hash)); }

		public enum CasResult {
			Stored = 0,
			NotStored = 1,
			Exists = 2,
			NotFound = 3
		}

		public CasResult CheckAndSet(string key, object value, ulong unique) { return store(key, true, value, hash(key), 0, unique); }
		public CasResult CheckAndSet(string key, object value, uint hash, ulong unique) { return store(key, false, value, this.hash(hash), 0, unique); }
		public CasResult CheckAndSet(string key, object value, TimeSpan expiry, ulong unique) { return store(key, true, value, hash(key), (int)expiry.TotalSeconds, unique); }
		public CasResult CheckAndSet(string key, object value, uint hash, TimeSpan expiry, ulong unique) { return store(key, false, value, this.hash(hash), (int)expiry.TotalSeconds, unique); }
		public CasResult CheckAndSet(string key, object value, DateTime expiry, ulong unique) { return store(key, true, value, hash(key), getUnixTime(expiry), unique); }
		public CasResult CheckAndSet(string key, object value, uint hash, DateTime expiry, ulong unique) { return store(key, false, value, this.hash(hash), getUnixTime(expiry), unique); }

		//Private overload for the Set, Add and Replace commands.
		private bool store(string command, string key, bool keyIsChecked, object value, uint hash, int expiry) {
			return store(command, key, keyIsChecked, value, hash, expiry, 0).StartsWith("STORED");
		}

		//Private overload for the Append and Prepend commands.
		private bool store(string command, string key, bool keyIsChecked, object value, uint hash) {
			return store(command, key, keyIsChecked, value, hash, 0, 0).StartsWith("STORED");
		}

		//Private overload for the Cas command.
		private CasResult store(string key, bool keyIsChecked, object value, uint hash, int expiry, ulong unique) {
			string result = store("cas", key, keyIsChecked, value, hash, expiry, unique);
			if (result.StartsWith("STORED")) {
				return CasResult.Stored;
			} else if (result.StartsWith("EXISTS")) {
				return CasResult.Exists;
			} else if (result.StartsWith("NOT_FOUND")) {
				return CasResult.NotFound;
			}
			return CasResult.NotStored;
		}

		//Private common store method.
		private string store(string command, string key, bool keyIsChecked, object value, uint hash, int expiry, ulong unique) {
			if (!keyIsChecked) {
				checkKey(key);
			}
            SerializedType type;
				byte[] bytes;
                try
                {
                    bytes = Serializer.Serialize(value, out type, CompressionThreshold);
                }
                catch (Exception e)
                {
                    //If serialization fails, return false;

                    logger.Error("Error serializing object for key '" + key + "'.", e);
                    return "";
                }
				string commandResult = "";
                if (memcachedProvider == null)
                {
                    ConnectionError();
                    return "";
                }
                    OperationResult opResult = new OperationResult();

                    try
                    {
                        switch (command)
                        {
                            case "set":
                                opResult = memcachedProvider.Set(key, (ushort)type, expiry, bytes);
                                break;
                            case "add":
                                opResult = memcachedProvider.Add(key, (ushort)type, expiry, bytes);
                                break;
                            case "replace":
                                opResult = memcachedProvider.Replace(key, (ushort)type, expiry, bytes);
                                break;
                            case "append":
                                opResult = memcachedProvider.Append(key, bytes, unique);
                                break;
                            case "prepend":
                                opResult = memcachedProvider.Prepend(key, bytes, unique);
                                break;
                            case "cas":
                                opResult = memcachedProvider.CheckAndSet(key, (ushort)type, expiry, unique, bytes);
                                break;
                        }
                        if (command.Equals("replace") && opResult.ReturnResult == Result.ITEM_NOT_FOUND)
                            commandResult = "NOT_STORED";
                        else
                            switch (opResult.ReturnResult)
                            {
                                case Result.SUCCESS:
                                    commandResult = "STORED";
                                    break;
                                case Result.ITEM_EXISTS:
                                    commandResult = "NOT_STORED";
                                    break;
                                case Result.ITEM_NOT_FOUND:
                                    commandResult = "NOT_FOUND";
                                    break;
                                case Result.ITEM_MODIFIED:
                                    commandResult = "EXISTS";
                                    break;
                            }
                    }
                    catch (InvalidArgumentsException e)
                    {
                        commandResult = "CLIENT_ERROR bad command line format";
                    }
                    catch (CacheRuntimeException e)
                    {
                        commandResult = "SERVER_ERROR";
                    }
                    return commandResult;
		}

		#endregion

		#region Get
		/// <summary>
		/// This method corresponds to the "get" command in the memcached protocol.
		/// It will return the value for the given key. It will return null if the key did not exist,
		/// or if it was unable to retrieve the value.
		/// If given an array of keys, it will return a same-sized array of objects with the corresponding
		/// values.
		/// Use the overload to specify a custom hash to override server selection.
		/// </summary>
		public object Get(string key) { ulong i; return get("get", key, true, hash(key), out i); }
		public object Get(string key, uint hash) { ulong i; return get("get", key, false, this.hash(hash), out i); }

		/// <summary>
		/// This method corresponds to the "gets" command in the memcached protocol.
		/// It works exactly like the Get method, but it will also return the cas unique value for the item.
		/// </summary>
		public object Gets(string key, out ulong unique) { return get("gets", key, true, hash(key), out unique); }
		public object Gets(string key, uint hash, out ulong unique) { return get("gets", key, false, this.hash(hash), out unique); }

		private object get(string command, string key, bool keyIsChecked, uint hash, out ulong unique) {
			if (!keyIsChecked) {
				checkKey(key);
			}
            unique = 0;
            if (memcachedProvider == null)
            {
                ConnectionError();
                return null;
            }
			
            object value;
            try
            {
                List<GetOpResult> opResult = memcachedProvider.Get(new[] { key });
                
                if (opResult.Count == 0)
                    return null;
                else
                {
                    SerializedType type = (SerializedType)Enum.Parse(typeof(SerializedType), opResult[0].Flag.ToString());
                    try
                    {
                        value = Serializer.DeSerialize((byte[])opResult[0].Value, type);
                        unique = Convert.ToUInt64(opResult[0].Version);
                    }
                    catch (Exception e)
                    {
                        //If deserialization fails, return null
                        value = null;
                        logger.Error("Error deserializing object for key '" + key + "' of type " + type + ".", e);
                    }
                }
            }
            catch (InvalidArgumentsException e)
            {
                logger.Error("CLIENT_ERROR bad command line format" , e);
                value= null;
            }
            catch (CacheRuntimeException e)
            {
               value= null;
               logger.Error("SERVER_ERROR", e);
            }
                return value;
		}

		/// <summary>
		/// This method executes a multi-get. It will group the keys by server and execute a single get 
		/// for each server, and combine the results. The returned object[] will have the same size as
		/// the given key array, and contain either null or a value at each position according to
		/// the key on that position.
		/// </summary>
		public object[] Get(string[] keys) { ulong[] uniques; return get("get", keys, true, hash(keys), out uniques); }
		public object[] Get(string[] keys, uint[] hashes) { ulong[] uniques; return get("get", keys, false, hash(hashes), out uniques); }
		
		/// <summary>
		/// This method does a multi-gets. It functions exactly like the multi-get method, but it will
		/// also return an array of cas unique values as an out parameter.
		/// </summary>
		public object[] Gets(string[] keys, out ulong[] uniques) { return get("gets", keys, true, hash(keys), out uniques); }
		public object[] Gets(string[] keys, uint[] hashes, out ulong[] uniques) { return get("gets", keys, false, hash(hashes), out uniques); }

		private object[] get(string command, string[] keys, bool keysAreChecked, uint[] hashes, out ulong[] uniques) {
			//Check arguments.
			if (keys == null || hashes == null) {
				throw new ArgumentException("Keys and hashes arrays must not be null.");
			}
			if (keys.Length != hashes.Length) {
				throw new ArgumentException("Keys and hashes arrays must be of the same length.");
			}
			uniques = new ulong[keys.Length];

			//Avoid going through the server grouping if there's only one key.
			if (keys.Length == 1) {
				return new object[] { get(command, keys[0], keysAreChecked, hashes[0], out uniques[0]) };
			}

			//Check keys.
			if (!keysAreChecked) {
				for (int i = 0; i < keys.Length; i++) {
					checkKey(keys[i]);
				}
			}



			//Get the values
            uniques = new ulong[keys.Length];
            object[] returnValues = new object[keys.Length];
            try
            {
                List<GetOpResult> opResult = memcachedProvider.Get(keys);
                List<string> keysList = new List<string>();
                foreach (GetOpResult entry in opResult)
                    keysList.Add(entry.Key);

                for (int i = 0; i < keys.Length; i++)
                {
                    int index = keysList.IndexOf(keys[i]);
                    if (index > -1)
                    {
                        
                        SerializedType type = (SerializedType)Enum.Parse(typeof(SerializedType), opResult[index].Flag.ToString());
                        try
                        {
                            returnValues[i] = Serializer.DeSerialize((byte[])opResult[index].Value, type);
                            uniques[i] = Convert.ToUInt64(opResult[index].Version);
                        }
                        catch (Exception e)
                        {
                            //If deserialization fails, return null
                            returnValues[i] = null;
                            logger.Error("Error deserializing object for key '" + keys[i] + "' of type " + type + ".", e);
                        }
                    }
                    else
                        returnValues[i] = null;
                }
            }
            catch (InvalidArgumentsException e)
            {
                logger.Error("CLIENT_ERROR bad command line format", e);
            }
            catch (CacheRuntimeException e)
            {
                logger.Error("SERVER_ERROR", e);
            }
			return returnValues;
		}

		#endregion

		#region Delete
		/// <summary>
		/// This method corresponds to the "delete" command in the memcache protocol.
		/// It will immediately delete the given key and corresponding value.
		/// Use the overloads to specify an amount of time the item should be in the delete queue on the server,
		/// or to specify a custom hash to override server selection.
		/// </summary>
		public bool Delete(string key) { return delete(key, true, hash(key), 0); }
		public bool Delete(string key, uint hash) { return delete(key, false, this.hash(hash), 0); }
		public bool Delete(string key, TimeSpan delay) { return delete(key, true, hash(key), (int)delay.TotalSeconds); }
		public bool Delete(string key, uint hash, TimeSpan delay) { return delete(key, false, this.hash(hash), (int)delay.TotalSeconds); }
		public bool Delete(string key, DateTime delay) { return delete(key, true, hash(key), getUnixTime(delay)); }
		public bool Delete(string key, uint hash, DateTime delay) { return delete(key, false, this.hash(hash), getUnixTime(delay)); }

		private bool delete(string key, bool keyIsChecked, uint hash, int time) {
			if (!keyIsChecked) {
				checkKey(key);
			}
            if(memcachedProvider==null)
            {
                ConnectionError();
                return false;
            }
            try
            {
                OperationResult opResult = memcachedProvider.Delete(key,0);
                if (opResult.ReturnResult == Result.SUCCESS)
                    return true;
                else
                    return false;

            }
            catch (Exception e)
            {
                logger.Error("SERVER_ERROR", e);
                return false;
            }
		}
		#endregion

		#region Increment Decrement
		/// <summary>
		/// This method sets the key to the given value, and stores it in a format such that the methods
		/// Increment and Decrement can be used successfully on it, i.e. decimal representation of a 64-bit unsigned integer. 
		/// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
		/// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
		/// This method returns true if the counter was successfully set.
		/// </summary>
		public bool SetCounter(string key, ulong value) { return Set(key, value.ToString(CultureInfo.InvariantCulture)); }
		public bool SetCounter(string key, ulong value, uint hash) { return Set(key, value.ToString(CultureInfo.InvariantCulture), this.hash(hash)); }
		public bool SetCounter(string key, ulong value, TimeSpan expiry) { return Set(key, value.ToString(CultureInfo.InvariantCulture), expiry); }
		public bool SetCounter(string key, ulong value, uint hash, TimeSpan expiry) { return Set(key, value.ToString(CultureInfo.InvariantCulture), this.hash(hash), expiry); }
		public bool SetCounter(string key, ulong value, DateTime expiry) { return Set(key, value.ToString(CultureInfo.InvariantCulture), expiry); }
		public bool SetCounter(string key, ulong value, uint hash, DateTime expiry) { return Set(key, value.ToString(CultureInfo.InvariantCulture), this.hash(hash), expiry); }

		/// <summary>
		/// This method returns the value for the given key as a ulong?, a nullable 64-bit unsigned integer.
		/// It returns null if the item did not exist, was not stored properly as per the SetCounter method, or 
		/// if it was not able to successfully retrieve the item.
		/// </summary>
		public ulong? GetCounter(string key) {return getCounter(key, true, hash(key));}
		public ulong? GetCounter(string key, uint hash) { return getCounter(key, false, this.hash(hash)); }

		private ulong? getCounter(string key, bool keyIsChecked, uint hash) {
			ulong parsedLong, unique;
			return ulong.TryParse(get("get", key, keyIsChecked, hash, out unique) as string, out parsedLong) ? (ulong?)parsedLong : null;
		}

		public ulong?[] GetCounter(string[] keys) {return getCounter(keys, true, hash(keys));}
		public ulong?[] GetCounter(string[] keys, uint[] hashes) { return getCounter(keys, false, hash(hashes)); }

		private ulong?[] getCounter(string[] keys, bool keysAreChecked, uint[] hashes) {
			ulong?[] results = new ulong?[keys.Length];
			ulong[] uniques;
			object[] values = get("get", keys, keysAreChecked, hashes, out uniques);
			for (int i = 0; i < values.Length; i++) {
				ulong parsedLong;
				results[i] = ulong.TryParse(values[i] as string, out parsedLong) ? (ulong?)parsedLong : null;
			}
			return results;
		}

		/// <summary>
		/// This method corresponds to the "incr" command in the memcached protocol.
		/// It will increase the item with the given value and return the new value.
		/// It will return null if the item did not exist, was not stored properly as per the SetCounter method, or 
		/// if it was not able to successfully retrieve the item. 
		/// </summary>
		public ulong? Increment(string key, ulong value) { return incrementDecrement("incr", key, true, value, hash(key)); }
		public ulong? Increment(string key, ulong value, uint hash) { return incrementDecrement("incr", key, false, value, this.hash(hash)); }

		/// <summary>
		/// This method corresponds to the "decr" command in the memcached protocol.
		/// It will decrease the item with the given value and return the new value. If the new value would be 
		/// less than 0, it will be set to 0, and the method will return 0.
		/// It will return null if the item did not exist, was not stored properly as per the SetCounter method, or 
		/// if it was not able to successfully retrieve the item. 
		/// </summary>
		public ulong? Decrement(string key, ulong value) { return incrementDecrement("decr", key, true, value, hash(key)); }
		public ulong? Decrement(string key, ulong value, uint hash) { return incrementDecrement("decr", key, false, value, this.hash(hash)); }

		private ulong? incrementDecrement(string cmd, string key, bool keyIsChecked, ulong value, uint hash) {
			if (!keyIsChecked) {
				checkKey(key);
			}
            if (memcachedProvider == null)
            {
                ConnectionError();
                return null;
            }
            MutateOpResult opResult=new MutateOpResult();
            try
            {
                switch (cmd)
                {
                    case "incr":
                        opResult = memcachedProvider.Increment(key, value, null, 0, 0);
                        break;
                    case "decr":
                        opResult = memcachedProvider.Decrement(key, value, null, 0, 0);
                        break;
                }
                if (opResult.ReturnResult == Result.SUCCESS)
                    return opResult.MutateResult;
                else
                    return null;
            }
            catch (Exception e)
            {
                return null;
            }
		}
		#endregion

		#region Flush All
		/// <summary>
		/// This method corresponds to the "flush_all" command in the memcached protocol.
		/// When this method is called, it will send the flush command to all servers, thereby deleting
		/// all items on all servers.
		/// Use the overloads to set a delay for the flushing. If the parameter staggered is set to true,
		/// the client will increase the delay for each server, i.e. the first will flush after delay*0, 
		/// the second after delay*1, the third after delay*2, etc. If set to false, all servers will flush 
		/// after the same delay.
		/// It returns true if the command was successful on all servers.
		/// </summary>
		public bool FlushAll() { return FlushAll(TimeSpan.Zero, false); }
		public bool FlushAll(TimeSpan delay) { return FlushAll(delay, false); }
		public bool FlushAll(TimeSpan delay, bool staggered) {
			bool noerrors = true;
            if (memcachedProvider == null)
            {
                ConnectionError();
                return false;
            }
            try
            {
                memcachedProvider.Flush_All(Convert.ToUInt32(delay.TotalSeconds));
            }
            catch (Exception e)
            {
                noerrors = false;
            }
			return noerrors;
		}
		#endregion

		#region Stats
		/// <summary>
		/// This method corresponds to the "stats" command in the memcached protocol.
		/// It will send the stats command to all servers, and it will return a Dictionary for each server
		/// containing the results of the command.
		/// </summary>
		public Dictionary<string, Dictionary<string, string>> Stats() {
			Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            if (memcachedProvider == null)
            {
                ConnectionError();
                return results;
            }
            try
            {
                OperationResult stats = memcachedProvider.GetStatistics("");
                var ht = (Hashtable)stats.Value;
                Dictionary<string, string> statsDic = new Dictionary<string, string>();
                foreach (DictionaryEntry entry in ht)
                    statsDic.Add(entry.Key.ToString(), entry.Value.ToString());
                results.Add("127.0.0.1", statsDic);
            }
            catch (Exception)
            { }
			return results;
		}

		/// <summary>
		/// This method corresponds to the "stats" command in the memcached protocol.
		/// It will send the stats command to the server that corresponds to the given key, hash or host,
		/// and return a Dictionary containing the results of the command.
		/// </summary>
		public Dictionary<string, string> Stats(string key) { return Stats(hash(key)); }
		public Dictionary<string, string> Stats(uint hash) { return stats(serverPool.GetSocketPool(this.hash(hash))); }
		public Dictionary<string, string> StatsByHost(string host) { return stats(serverPool.GetSocketPool(host)); }
		private Dictionary<string, string> stats(SocketPool pool) {
			if (pool == null) {
				return null;
			}

            Dictionary<string, string> results = new Dictionary<string, string>();
            if (memcachedProvider == null)
            {
                ConnectionError();
                return results;
            }
            try
            {
                OperationResult stats = memcachedProvider.GetStatistics("");
                var ht = (Hashtable)stats.Value;
                foreach (DictionaryEntry entry in ht)
                    results.Add(entry.Key.ToString(), entry.Value.ToString());
                
            }
            catch (Exception)
            { }
            return results;
		}

		#endregion

		#region Status
		/// <summary>
		/// This method retrives the status from the serverpool. It checks the connection to all servers
		/// and returns usage statistics for each server.
		/// </summary>
		public Dictionary<string, Dictionary<string, string>> Status() {
			Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
			
			return results;
		}
		#endregion

        private void ConnectionError()
        {
            Exception e = new Exception("No connection could be made because the target machine actively refused it");
            logger.Error("Error connecting to Cache Server ", e);
        }


        ~MemcachedClient()
        {
            try { ((IDisposable)this).Dispose(); }
            catch { }
        }

        void IDisposable.Dispose()
        {
            CacheFactory.DisposeCacheProvider();
        }
	}
}
