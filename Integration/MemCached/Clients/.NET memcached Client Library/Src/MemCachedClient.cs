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
/**
/// Memcached C# client
/// Copyright (c) 2005
///
/// This module is Copyright (c) 2005 Tim Gebhardt
/// All rights reserved.
/// Based on code written by Greg Whalin and Richard Russo
/// for a Java Memcached client which can be found here:
/// http://www.whalin.com/memcached/
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the GNU Lesser General Public
/// License as published by the Free Software Foundation; either
/// version 2.1 of the License, or (at your option) any later
/// version.
///
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See the GNU Lesser General Public License for more
/// details.
///
/// You should have received a copy of the GNU Lesser General Public
/// License along with this library; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307  USA
///
/// @author Tim Gebhardt <tim@gebhardtcomputing.com>
/// @version 1.0
**/
namespace Memcached.ClientLibrary
{
	using System;
	using System.Collections;
	using System.Globalization;
	using System.IO;
	using System.Resources;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using System.Text.RegularExpressions;
    using System.Collections.Generic;
	using log4net;
	using ICSharpCode.SharpZipLib.GZip;
    using Memcached.ClientLibrary;
    using System.Configuration;
    using Alachisoft.NCache.Integrations.Memcached.Provider;
    using Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions;
	/// <summary>
	/// This is a C# client for the memcached server available from
	/// <a href="http:/www.danga.com/memcached/">http://www.danga.com/memcached/</a>.
	///
	/// Supports setting, adding, replacing, deleting compressed/uncompressed and
	/// serialized (can be stored as string if object is native class) objects to memcached.
	///
	/// Now pulls SockIO objects from SockIOPool, which is a connection pool.  The server failover
	/// has also been moved into the SockIOPool class.
	/// This pool needs to be initialized prior to the client working.  See javadocs from SockIOPool.
	/// (This will have to be fixed for our C# version.  Most of this code is straight ported over from Java.)
	/// </summary>
	/// <example>
	/// //***To create cache client object and set params:***
	/// MemcachedClient mc = new MemcachedClient();
	/// 
	/// // compression is enabled by default	
	/// mc.setCompressEnable(true);
	///
	///	// set compression threshhold to 4 KB (default: 15 KB)	
	///	mc.setCompressThreshold(4096);
	///
	///	// turn on storing primitive types as a string representation
	///	// Should not do this in most cases.	
	///	mc.setPrimitiveAsString(true);
	/// 
	/// 
	/// //***To store an object:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "cacheKey1";	
	/// object value = SomeClass.getObject();	
	/// mc.set(key, value);
	/// 
	/// 
	/// //***To store an object using a custom server hashCode:***
	/// //The set method shown here will always set the object in the cache.
	/// //The add and replace methods do the same, but with a slight difference.
	/// //  add -- will store the object only if the server does not have an entry for this key
	/// //  replace -- will store the object only if the server already has an entry for this key
	///	MemcachedClient mc = new MemcachedClient();
	///	string key   = "cacheKey1";	
	///	object value = SomeClass.getObject();	
	///	int hash = 45;
	///	mc.set(key, value, hash);
	/// 
	/// 
	/// //***To delete a cache entry:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "cacheKey1";	
	/// mc.delete(key);
	/// 
	/// 
	/// //***To delete a cache entry using a custom hash code:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "cacheKey1";	
	/// int hash = 45;
	/// mc.delete(key, hashCode);
	/// 
	/// 
	/// //***To store a counter and then increment or decrement that counter:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "counterKey";	
	/// mc.storeCounter(key, 100);
	/// Console.WriteLine("counter after adding      1: " mc.incr(key));	
	/// Console.WriteLine("counter after adding      5: " mc.incr(key, 5));	
	/// Console.WriteLine("counter after subtracting 4: " mc.decr(key, 4));	
	/// Console.WriteLine("counter after subtracting 1: " mc.decr(key));	
	/// 
	/// 
	/// //***To store a counter and then increment or decrement that counter with custom hash:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "counterKey";	
	/// int hash = 45;	
	/// mc.storeCounter(key, 100, hash);
	/// Console.WriteLine("counter after adding      1: " mc.incr(key, 1, hash));	
	/// Console.WriteLine("counter after adding      5: " mc.incr(key, 5, hash));	
	/// Console.WriteLine("counter after subtracting 4: " mc.decr(key, 4, hash));	
	/// Console.WriteLine("counter after subtracting 1: " mc.decr(key, 1, hash));	
	/// 
	/// 
	/// //***To retrieve an object from the cache:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "key";	
	/// object value = mc.get(key);	
	///
	///
	/// //***To retrieve an object from the cache with custom hash:***
	/// MemcachedClient mc = new MemcachedClient();
	/// string key   = "key";	
	/// int hash = 45;	
	/// object value = mc.get(key, hash);
	/// 
	/// 
	/// //***To retrieve an multiple objects from the cache***
	/// MemcachedClient mc = new MemcachedClient();
	/// string[] keys   = { "key", "key1", "key2" };
	/// object value = mc.getMulti(keys);
	/// 
	///
	/// //***To retrieve an multiple objects from the cache with custom hashing***
	/// MemcachedClient mc = new MemcachedClient();
	/// string[] keys    = { "key", "key1", "key2" };
	/// int[] hashes = { 45, 32, 44 };
	/// object value = mc.getMulti(keys, hashes);
	/// 
	///
	/// //***To flush all items in server(s)***
	/// MemcachedClient mc = new MemcachedClient();
	/// mc.FlushAll();
	/// 
	///
	/// //***To get stats from server(s)***
	/// MemcachedClient mc = new MemcachedClient();
	/// Hashtable stats = mc.stats();
	/// </example>
    public class MemcachedClient : IDisposable
	{
        private IMemcachedProvider memcachedProvider;
		// logger
		private static ILog log = LogManager.GetLogger(typeof(MemcachedClient));

		// return codes
		private const string VALUE = "VALUE"; // start of value line from server
        private const string STATS = "STAT"; // start of stats line from server
        private const string DELETED = "DELETED"; // successful deletion
        private const string NOTFOUND = "NOT_FOUND"; // record not found for delete or incr/decr
        private const string STORED = "STORED"; // successful store of data
        private const string NOTSTORED = "NOT_STORED"; // data not stored
        private const string OK = "OK"; // success
        private const string END = "END"; // end of data from server
        private const string ERROR = "ERROR"; // invalid command name from client
        private const string CLIENT_ERROR = "CLIENT_ERROR"; // client error in input line - invalid protocol
        private const string SERVER_ERROR = "SERVER_ERROR";	// server error

		// default compression threshold
		private const int COMPRESS_THRESH = 30720;
    
		// values for cache flags 
		//
		// using 8 (1 << 3) so other clients don't try to unpickle/unstore/whatever
		// things that are serialized... I don't think they'd like it. :)
		private const int F_COMPRESSED = 2;
		private const int F_SERIALIZED = 8;
	
		// flags
		private bool _primitiveAsString;
		private bool _compressEnable;
		private long _compressThreshold;
		private string _defaultEncoding;

		// which pool to use
		private string _poolName;

		/// <summary>
		/// Creates a new instance of MemcachedClient.
		/// </summary>
		public MemcachedClient() 
		{
			Init();
		}

		/// <summary>
		/// Initializes client object to defaults.
		/// 
		/// This enables compression and sets compression threshhold to 15 KB.
		/// </summary>
		private void Init() 
		{
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
            {
                throw e;
            }
            try
            {
                memcachedProvider = CacheFactory.CreateCacheProvider(cacheName);
            }
            catch (Exception e)
            {
                //do nothing
            }
			_primitiveAsString = false;
			_compressEnable = true;
			_compressThreshold = COMPRESS_THRESH;
			_defaultEncoding = "UTF-8";
			_poolName = GetLocalizedString("default instance");
		}

		/// <summary>
		/// Sets the pool that this instance of the client will use.
		/// The pool must already be initialized or none of this will work.
		/// </summary>
        public string PoolName
        {
            get { return _poolName; }
            set { _poolName = value; }
        }

		/// <summary>
		/// Enables storing primitive types as their string values. 
		/// </summary>
		public bool PrimitiveAsString
		{
			get { return _primitiveAsString; }
			set { _primitiveAsString = value; }
		}

		/// <summary>
		/// Sets default string encoding when storing primitives as strings. 
		/// Default is UTF-8.
		/// </summary>
		public string DefaultEncoding
		{
			get { return _defaultEncoding; }
			set { _defaultEncoding = value; }
		}

		/// <summary>
		/// Enable storing compressed data, provided it meets the threshold requirements.
		/// 
		/// If enabled, data will be stored in compressed form if it is
		/// longer than the threshold length set with setCompressThreshold(int)
		/// 
		/// The default is that compression is enabled.
		/// 
		/// Even if compression is disabled, compressed data will be automatically
		/// decompressed.
		/// </summary>
		/// <value><c>true</c> to enable compuression, <c>false</c> to disable compression</value>
		public bool EnableCompression
		{
			get { return _compressEnable; }
			set { _compressEnable = value; }
		}

		/// <summary>
		/// Sets the required length for data to be considered for compression.
		/// 
		/// If the length of the data to be stored is not equal or larger than this value, it will
		/// not be compressed.
		/// 
		/// This defaults to 15 KB.
		/// </summary>
		/// <value>required length of data to consider compression</value>
		public long CompressionThreshold
		{
			get { return _compressThreshold; }
			set { _compressThreshold = value; }
		}

		/// <summary>
		/// Checks to see if key exists in cache. 
		/// </summary>
		/// <param name="key">the key to look for</param>
		/// <returns><c>true</c> if key found in cache, <c>false</c> if not (or if cache is down)</returns>
		public bool KeyExists(string key) 
		{
			return(Get(key, null, true) != null);
		}
	
		/// <summary>
		/// Deletes an object from cache given cache key.
		/// </summary>
		/// <param name="key">the key to be removed</param>
		/// <returns><c>true</c>, if the data was deleted successfully</returns>
		public bool Delete(string key) 
		{
			return Delete(key, null, DateTime.MaxValue);
		}

		/// <summary>
		/// Deletes an object from cache given cache key and expiration date. 
		/// </summary>
		/// <param name="key">the key to be removed</param>
		/// <param name="expiry">when to expire the record.</param>
		/// <returns><c>true</c>, if the data was deleted successfully</returns>
		public bool Delete(string key, DateTime expiry) 
		{
			return Delete(key, null, expiry);
		}

		/// <summary>
		/// Deletes an object from cache given cache key, a delete time, and an optional hashcode.
		/// 
		/// The item is immediately made non retrievable.<br/>
		/// Keep in mind: 
		/// <see cref="add">add(string, object)</see> and <see cref="replace">replace(string, object)</see>
		///	will fail when used with the same key will fail, until the server reaches the
		///	specified time. However, <see cref="set">set(string, object)</see> will succeed
		/// and the new value will not be deleted.
		/// </summary>
		/// <param name="key">the key to be removed</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <param name="expiry">when to expire the record.</param>
		/// <returns><c>true</c>, if the data was deleted successfully</returns>
		public bool Delete(string key, object hashCode, DateTime expiry) 
		{
			if(key == null) 
			{
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("null key delete"));
				}
				return false;
			}

            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return false;
            }
            ulong seconds = 0;
			if(expiry != DateTime.MaxValue)
				seconds=(ulong)GetExpirationTime(expiry) / 1000;

			try 
			{
				// if we get appropriate response back, then we return true
                OperationResult opResult = memcachedProvider.Delete(key,0);
                
				string line = "";
                switch (opResult.ReturnResult)
                {
                    case Result.SUCCESS:
                        line = "DELETED";
                        break;
                    case Result.ITEM_NOT_FOUND:
                        line = "NOT_FOUND";
                        break;
                }
                if(DELETED == line) 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("delete success").Replace("$$Key$$", key));
					}

					return true;
				}
				else if(NOTFOUND == line) 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("delete key not found").Replace("$$Key$$", key));
					}
				}
				else 
				{
					if(log.IsErrorEnabled)
					{
						log.Error(GetLocalizedString("delete key error").Replace("$$Key$$", key).Replace("$$Line$$", line));
					}
				}
			}
			catch(Exception e) 
			{
				if(log.IsErrorEnabled)
				{
					// exception thrown
					log.Error(GetLocalizedString("delete IOException"), e);
				}

			}

			return false;
		}

		/// <summary>
		/// Converts a .NET date time to a UNIX timestamp
		/// </summary>
		/// <param name="ticks"></param>
		/// <returns></returns>
		private static int GetExpirationTime(DateTime expiration)
		{
			if(expiration <= DateTime.Now)
				return 0;

			TimeSpan thirtyDays = new TimeSpan(29, 23, 59, 59);
			if(expiration.Subtract(DateTime.Now) > thirtyDays)
				return (int)thirtyDays.TotalSeconds;
			
			return (int)expiration.Subtract(DateTime.Now).TotalSeconds;
		}
    
		/// <summary>
		/// Stores data on the server; only the key and the value are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Set(string key, object value) 
		{
			return Set("set", key, value, DateTime.MaxValue, null, _primitiveAsString);
		}

		/// <summary>
		/// Stores data on the server; only the key and the value are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Set(string key, object value, int hashCode) 
		{
			return Set("set", key, value, DateTime.MaxValue, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Stores data on the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Set(string key, object value, DateTime expiry) 
		{
			return Set("set", key, value, expiry, null, _primitiveAsString);
		}

		/// <summary>
		/// Stores data on the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Set(string key, object value, DateTime expiry, int hashCode) 
		{
			return Set("set", key, value, expiry, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Adds data to the server; only the key and the value are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Add(string key, object value) 
		{
			return Set("add", key, value, DateTime.MaxValue, null, _primitiveAsString);
		}

		/// <summary>
		/// Adds data to the server; the key, value, and an optional hashcode are passed in.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Add(string key, object value, int hashCode) 
		{
			return Set("add", key, value, DateTime.MaxValue, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Adds data to the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Add(string key, object value, DateTime expiry) 
		{
			return Set("add", key, value, expiry, null, _primitiveAsString);
		}

		/// <summary>
		/// Adds data to the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Add(string key, object value, DateTime expiry, int hashCode) 
		{
			return Set("add", key, value, expiry, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Updates data on the server; only the key and the value are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Replace(string key, object value) 
		{
			return Set("replace", key, value, DateTime.MaxValue, null, _primitiveAsString);
		}

		/// <summary>
		/// Updates data on the server; only the key and the value and an optional hash are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Replace(string key, object value, int hashCode) 
		{
			return Set("replace", key, value, DateTime.MaxValue, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Updates data on the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Replace(string key, object value, DateTime expiry) 
		{
			return Set("replace", key, value, expiry, null, _primitiveAsString);
		}

		/// <summary>
		/// Updates data on the server; the key, value, and an expiration time are specified.
		/// </summary>
		/// <param name="key">key to store data under</param>
		/// <param name="value">value to store</param>
		/// <param name="expiry">when to expire the record</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true, if the data was successfully stored</returns>
		public bool Replace(string key, object value, DateTime expiry, int hashCode) 
		{
			return Set("replace", key, value, expiry, hashCode, _primitiveAsString);
		}

		/// <summary>
		/// Stores data to cache.
		/// 
		/// If data does not already exist for this key on the server, or if the key is being
		/// deleted, the specified value will not be stored.
		/// The server will automatically delete the value when the expiration time has been reached.
		/// 
		/// If compression is enabled, and the data is longer than the compression threshold
		/// the data will be stored in compressed form.
		/// 
		/// As of the current release, all objects stored will use .NET serialization.
		/// </summary>
		/// <param name="cmdname">action to take (set, add, replace)</param>
		/// <param name="key">key to store cache under</param>
		/// <param name="value">object to cache</param>
		/// <param name="expiry">expiration</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <param name="asString">store this object as a string?</param>
		/// <returns>true/false indicating success</returns>
		private bool Set(string cmdname, string key, object obj, DateTime expiry, object hashCode, bool asString) 
		{
			if(expiry < DateTime.Now)
				return true;

			if(cmdname == null || cmdname.Trim().Length == 0 || key == null || key.Length == 0) 
			{
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("set key null"));
				}
				return false;
			}
            if (memcachedProvider == null)
            {
                    if (log.IsErrorEnabled)
                    {
                        log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$","localhost"));
                    }
                return false;
            }
            if (expiry == DateTime.MaxValue)
                expiry = new DateTime(0);

            // store flags
            int flags = 0;
            // byte array to hold data
			byte[] val;
			int length = 0;

			// useful for sharing data between .NET and non-.NET
            // and also for storing ints for the increment method
			if(NativeHandler.IsHandled(obj)) 
			{
				if(asString) 
				{
                    if(obj != null)
                    {
						if(log.IsInfoEnabled)
						{
							log.Info(GetLocalizedString("set store data as string").Replace("$$Key$$", key).Replace("$$Class$$", obj.GetType().Name));
						}
                        try
                        {
                            val = UTF8Encoding.UTF8.GetBytes(obj.ToString());
                        }
                        catch(ArgumentException ex)
                        {
							if(log.IsErrorEnabled)
							{
								log.Error(GetLocalizedString("set invalid encoding").Replace("$$Encoding$$", _defaultEncoding), ex);
							}
                            return false;
                        }
                    }
                    else
                    {
                        val = new byte[0];
						length = 0;
                    }
				}
				else 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("set store with native handler"));
					}

					try 
					{
						val = NativeHandler.Encode(obj);
						length = val.Length;
					}
					catch(ArgumentException e) 
					{
						if(log.IsErrorEnabled)
						{
							log.Error(GetLocalizedString("set failed to native handle object"), e);
						}

						return false;
					}
				}
			}
			else 
			{
                if(obj != null)
                {
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("set serializing".Replace("$$Key$$", key).Replace("$$Class$$", obj.GetType().Name)));
					}

                    // always serialize for non-primitive types
					try
                    {
                        MemoryStream memStream = new MemoryStream();
                        new BinaryFormatter().Serialize(memStream, obj);
                        val = memStream.GetBuffer();
						length = (int) memStream.Length;
                        flags |= F_SERIALIZED;
                    }
                    catch(IOException e)
                    {
                        // if we fail to serialize, then
                        // we bail
						if(log.IsErrorEnabled)
						{
							log.Error(GetLocalizedString("set failed to serialize").Replace("$$Object$$", obj.ToString()), e);
						}

                        return false;
                    }
                }
                else
                {
                    val = new byte[0];
					length = 0;
                }
			}
		
			// now try to compress if we want to
			// and if the length is over the threshold 
			if(_compressEnable && length > _compressThreshold) 
			{
				if(log.IsInfoEnabled)
				{
					log.Info(GetLocalizedString("set trying to compress data"));
					log.Info(GetLocalizedString("set size prior").Replace("$$Size$$", length.ToString(new NumberFormatInfo())));
				}

				try 
				{
					MemoryStream memoryStream = new MemoryStream();
					GZipOutputStream gos = new GZipOutputStream(memoryStream);
					gos.Write(val, 0, length);
					gos.Finish();
				
					// store it and set compression flag
					val = memoryStream.GetBuffer();
					length = (int)memoryStream.Length;
					flags |= F_COMPRESSED;

					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("set compression success").Replace("$$Size$$", length.ToString(new NumberFormatInfo())));
					}
				}
				catch(IOException e) 
				{
					if(log.IsErrorEnabled)
					{
						log.Error(GetLocalizedString("set compression failure"), e);
					}
				}
			}

			// now write the data to the cache server
			try 
			{
                OperationResult opResult = new OperationResult();
                switch (cmdname)
                {
                    case "set":
                        opResult = memcachedProvider.Set(key, (uint)flags, GetExpirationTime(expiry), val);
                        break;
                    case "add":
                        opResult = memcachedProvider.Add(key, (uint)flags, GetExpirationTime(expiry), val);
                        break;
                    case "replace":
                        opResult = memcachedProvider.Replace(key, (uint)flags, GetExpirationTime(expiry), val);
                        break;
                }
                string line = "";
                if (cmdname.Equals("replace") && opResult.ReturnResult == Result.ITEM_NOT_FOUND)
                    line = "NOT_STORED";
                else
                    switch (opResult.ReturnResult)
                    {
                        case Result.SUCCESS:
                            line = "STORED";
                            break;
                        case Result.ITEM_EXISTS:
                            line = "NOT_STORED";
                            break;
                        case Result.ITEM_NOT_FOUND:
                            line = "NOT_FOUND";
                            break;
                        case Result.ITEM_MODIFIED:
                            line = "EXISTS";
                            break;
                    }
				string cmd = cmdname + " " + key + " " + flags + " "
					+ GetExpirationTime(expiry) + " " + length + "\r\n";
				if(log.IsInfoEnabled)
				{
					log.Info(GetLocalizedString("set memcached command result").Replace("$$Cmd$$", cmd).Replace("$$Line$$", line));
				}

				if(STORED == line) 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("set success").Replace("$$Key$$", key));
					}
					return true;
				}
				else if(NOTSTORED == line) 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("set not stored").Replace("$$Key$$", key));
					}
				}
				else 
				{
					if(log.IsErrorEnabled)
					{
						log.Error(GetLocalizedString("set error").Replace("$$Key$$", key).Replace("$$Size$$", length.ToString(new NumberFormatInfo())).Replace("$$Line$$", line));
					}
				}
			}
			catch(Exception e) 
			{
				// exception thrown
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("set IOException"), e);
				}

			}

			return false;
		}

		/// <summary>
		/// Store a counter to memcached given a key
		/// </summary>
		/// <param name="key">cache key</param>
		/// <param name="counter">number to store</param>
		/// <returns>true/false indicating success</returns>
		public bool StoreCounter(string key, long counter) 
		{
			return Set("set", key, counter, DateTime.MaxValue, null, true);
		}
    
		/// <summary>
		/// Store a counter to memcached given a key
		/// </summary>
		/// <param name="key">cache key</param>
		/// <param name="counter">number to store</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>true/false indicating success</returns>
		public bool StoreCounter(string key, long counter, int hashCode) 
		{
			return Set("set", key, counter, DateTime.MaxValue, hashCode, true);
		}

		/// <summary>
		/// Returns value in counter at given key as long. 
		/// </summary>
		/// <param name="key">cache ket</param>
		/// <returns>counter value or -1 if not found</returns>
		public long GetCounter(string key) 
		{
			return GetCounter(key, null);
		}

		/// <summary>
		/// Returns value in counter at given key as long. 
		/// </summary>
		/// <param name="key">cache ket</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>counter value or -1 if not found</returns>
		public long GetCounter(string key, object hashCode) 
		{
			if(key == null) 
			{
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("getcounter null key"));
				}
				return -1;
			}

			long counter = -1;
			try 
			{
				counter = long.Parse((string)Get(key, hashCode, true), new NumberFormatInfo());
			}
			catch(ArgumentException) 
			{
				// not found or error getting out
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("getcounter counter not found").Replace("$$Key$$", key));
				}
			}
		
			return counter;
		}

		/// <summary>
		/// Increment the value at the specified key by 1, and then return it.
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Increment(string key) 
		{
			return IncrementOrDecrement("incr", key, 1, null);
		}

		/// <summary>
		/// Increment the value at the specified key by passed in val. 
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <param name="inc">how much to increment by</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Increment(string key, long inc) 
		{
			return IncrementOrDecrement("incr", key, inc, null);
		}

		/// <summary>
		/// Increment the value at the specified key by the specified increment, and then return it.
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <param name="inc">how much to increment by</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Increment(string key, long inc, int hashCode) 
		{
			return IncrementOrDecrement("incr", key, inc, hashCode);
		}
	
		/// <summary>
		/// Decrement the value at the specified key by 1, and then return it.
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Decrement(string key) 
		{
			return IncrementOrDecrement("decr", key, 1, null);
		}

		/// <summary>
		/// Decrement the value at the specified key by passed in value, and then return it.
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <param name="inc">how much to increment by</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Decrement(string key, long inc) 
		{
			return IncrementOrDecrement("decr", key, inc, null);
		}

		/// <summary>
		/// Decrement the value at the specified key by the specified increment, and then return it.
		/// </summary>
		/// <param name="key">key where the data is stored</param>
		/// <param name="inc">how much to increment by</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>-1, if the key is not found, the value after incrementing otherwise</returns>
		public long Decrement(string key, long inc, int hashCode) 
		{
			return IncrementOrDecrement("decr", key, inc, hashCode);
		}

		/// <summary>
		/// Increments/decrements the value at the specified key by inc.
		/// 
		/// Note that the server uses a 32-bit unsigned integer, and checks for
		/// underflow. In the event of underflow, the result will be zero.  Because
		/// Java lacks unsigned types, the value is returned as a 64-bit integer.
		/// The server will only decrement a value if it already exists;
		/// if a value is not found, -1 will be returned.
		/// 
		/// TODO: C# has unsigned types.  We can fix this.
		/// </summary>
		/// <param name="cmdname">increment/decrement</param>
		/// <param name="key">cache key</param>
		/// <param name="inc">amount to incr or decr</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>new value or -1 if not exist</returns>
		private long IncrementOrDecrement(string cmdname, string key, long inc, object hashCode) 
		{
            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return -1;
            }
            if (inc < 0)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("CLIENT_ERROR cannot increment or decrement non-numeric value"));
                }
                return -1;
            }
			try 
			{
                MutateOpResult opResult = new MutateOpResult();
                switch (cmdname)
                {
                    case "incr":
                        opResult = memcachedProvider.Increment(key, (ulong)inc, null, 0, 0);
                        break;
                    case "decr":
                        opResult = memcachedProvider.Decrement(key, (ulong)inc, null, 0, 0);
                        break;
                }
                // get result back
                string line = "";

                if (opResult.ReturnResult == Result.SUCCESS)
                    line = opResult.MutateResult.ToString();
                else
                    if (opResult.ReturnResult == Result.ITEM_NOT_FOUND)
                        line = "NOT_FOUND";
                    else
                        if(opResult.ReturnResult==Result.ITEM_TYPE_MISMATCHED)
                            line = "CLIENT_ERROR cannot increment or decrement non-numeric value";


				string cmd = cmdname + " " + key + " " + inc + "\r\n";
				if(log.IsDebugEnabled)
				{
					log.Debug(GetLocalizedString("incr-decr command").Replace("$$Cmd$$", cmd));
				}
				
				if(new Regex("\\d+").Match(line).Success) 
				{

					return long.Parse(line, new NumberFormatInfo());

				} 
				else if(NOTFOUND == line) 
				{
					if(log.IsInfoEnabled)
					{
						log.Info(GetLocalizedString("incr-decr key not found").Replace("$$Key$$", key));
					}
				} 
				else 
				{
					if(log.IsErrorEnabled)
					{
						log.Error(GetLocalizedString("incr-decr key error").Replace("$$Key$$", key));
					}
				}
			}
			catch(Exception e) 
			{
				// exception thrown
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("incr-decr IOException"), e);
				}
                if (e is System.OverflowException)
                    throw e;

			}
			return -1;
		}

		/// <summary>
		/// Retrieve a key from the server, using a specific hash.
		/// 
		/// If the data was compressed or serialized when compressed, it will automatically
		/// be decompressed or serialized, as appropriate. (Inclusive or)
		/// 
		/// Non-serialized data will be returned as a string, so explicit conversion to
		/// numeric types will be necessary, if desired
		/// </summary>
		/// <param name="key">key where data is stored</param>
		/// <returns>the object that was previously stored, or null if it was not previously stored</returns>
		public object Get(string key) 
		{
			return Get(key, null, false);
		}

		/// <summary>
		/// Retrieve a key from the server, using a specific hash.
		/// 
		/// If the data was compressed or serialized when compressed, it will automatically
		/// be decompressed or serialized, as appropriate. (Inclusive or)
		/// 
		/// Non-serialized data will be returned as a string, so explicit conversion to
		/// numeric types will be necessary, if desired
		/// </summary>
		/// <param name="key">key where data is stored</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <returns>the object that was previously stored, or null if it was not previously stored</returns>
		public object Get(string key, int hashCode) 
		{
			return Get(key, hashCode, false);
		}

		/// <summary>
		/// Retrieve a key from the server, using a specific hash.
		/// 
		/// If the data was compressed or serialized when compressed, it will automatically
		/// be decompressed or serialized, as appropriate. (Inclusive or)
		/// 
		/// Non-serialized data will be returned as a string, so explicit conversion to
		/// numeric types will be necessary, if desired
		/// </summary>
		/// <param name="key">key where data is stored</param>
		/// <param name="hashCode">if not null, then the int hashcode to use</param>
		/// <param name="asString">if true, then return string val</param>
		/// <returns>the object that was previously stored, or null if it was not previously stored</returns>
		public object Get(string key, object hashCode, bool asString) 
		{
            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return null;
            }
			try 
			{
				string cmd = "get " + key + "\r\n";
				if(log.IsDebugEnabled)
				{
					log.Debug(GetLocalizedString("get memcached command").Replace("$$Cmd$$", cmd));
				}

				// build empty map
				// and fill it from server
				Hashtable hm = new Hashtable();
				LoadItems(new[]{key},hm, asString);

				if(log.IsDebugEnabled)
				{
					// debug code
					log.Debug(GetLocalizedString("get memcached result").Replace("$$Results$$", hm.Count.ToString(new NumberFormatInfo())));
				}
				return hm[key];

			}
			catch(Exception e) 
			{
				// exception thrown
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("get IOException").Replace("$$Key$$", key), e);
				}

				
			}

			return null;
		}

		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <returns>object array ordered in same order as key array containing results</returns>
		public object[] GetMultipleArray(string[] keys) 
		{
            return GetMultipleArray(keys, null, false);
		}

		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <param name="hashCodes">if not null, then the int array of hashCodes</param>
		/// <returns>object array ordered in same order as key array containing results</returns>
        public object[] GetMultipleArray(string[] keys, int[] hashCodes) 
		{
            return GetMultipleArray(keys, hashCodes, false);
		}

		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <param name="hashCodes">if not null, then the int array of hashCodes</param>
		/// <param name="asString">asString if true, retrieve string vals</param>
		/// <returns>object array ordered in same order as key array containing results</returns>
        public object[] GetMultipleArray(string[] keys, int[] hashCodes, bool asString) 
		{
			if(keys == null)
				return new object[0];

            Hashtable data = GetMultiple(keys, hashCodes, asString);

			object[] res = new object[keys.Length];
			for(int i = 0; i < keys.Length; i++) 
			{
				res[i] = data[ keys[i] ];
			}

			return res;
		}

		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <returns>
		/// a hashmap with entries for each key is found by the server,
		/// keys that are not found are not entered into the hashmap, but attempting to
		/// retrieve them from the hashmap gives you null.
		/// </returns>
		public Hashtable GetMultiple(string[] keys) 
		{
            return GetMultiple(keys, null, false);
		}
    
		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <param name="hashCodes">hashCodes if not null, then the int array of hashCodes</param>
		/// <returns>
		/// a hashmap with entries for each key is found by the server,
		/// keys that are not found are not entered into the hashmap, but attempting to
		/// retrieve them from the hashmap gives you null.
		/// </returns>
        public Hashtable GetMultiple(string[] keys, int[] hashCodes) 
		{
            return GetMultiple(keys, hashCodes, false);
		}

		/// <summary>
		/// Retrieve multiple objects from the memcache.
		/// 
		/// This is recommended over repeated calls to <see cref="get">get(string)</see>, since it
		/// is more efficient.
		/// </summary>
		/// <param name="keys">string array of keys to retrieve</param>
		/// <param name="hashCodes">hashCodes if not null, then the int array of hashCodes</param>
		/// <param name="asString">if true then retrieve using string val</param>
		/// <returns>
		/// a hashmap with entries for each key is found by the server,
		/// keys that are not found are not entered into the hashmap, but attempting to
		/// retrieve them from the hashmap gives you null.
		/// </returns>
        public Hashtable GetMultiple(string[] keys, int[] hashCodes, bool asString) 
		{
			if(keys == null)
				return new Hashtable();
			// now query memcache
			Hashtable ret = new Hashtable();
			ArrayList toRemove = new ArrayList();
            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return null;
            }
			try 
			{
				string cmd = "get" + "\r\n";
				if(log.IsDebugEnabled)
				{
					log.Debug(GetLocalizedString("getmultiple memcached command").Replace("$$Cmd$$", cmd));
				}
				LoadItems(keys,ret, asString);
			}
			catch(Exception e) 
			{
				// exception thrown
				if(log.IsErrorEnabled)
				{
					log.Error(GetLocalizedString("getmultiple IOException"), e);
				}

			}
			if(log.IsDebugEnabled)
			{
				log.Debug(GetLocalizedString("getmultiple results").Replace("$$Results$$", ret.Count.ToString(new NumberFormatInfo())));
			}
			return ret;
		}
    
		/// <summary>
		/// This method loads the data from cache into a Hashtable.
		/// 
		/// Pass a SockIO object which is ready to receive data and a Hashtable
		/// to store the results.
		/// </summary>
		/// <param name="sock">socket waiting to pass back data</param>
		/// <param name="hm">hashmap to store data into</param>
		/// <param name="asString">if true, and if we are using NativehHandler, return string val</param>
		private void LoadItems(string[] keys,Hashtable hm, bool asString) 
		{
            string line = "";
            int length = 0;
				if(log.IsDebugEnabled)
				{
					log.Debug(GetLocalizedString("loaditems line").Replace("$$Line$$", line));
				}
            List<GetOpResult> opResult = memcachedProvider.Get(keys);
            foreach(GetOpResult resultEntry in opResult)
            {
					string key    = resultEntry.Key;
					int flag      = (int)resultEntry.Flag;

					if(log.IsDebugEnabled)
					{
						log.Debug(GetLocalizedString("loaditems header").Replace("$$Key$$", key).Replace("$$Flags$$", flag.ToString(new NumberFormatInfo())).Replace("$$Length$$", length.ToString(new NumberFormatInfo())));
					}
				
					// read obj into buffer
					byte[] buf =(byte[])resultEntry.Value;
					// ready object
					object o;
				
					// check for compression
					if((flag & F_COMPRESSED) != 0) 
					{
						try 
						{
							// read the input stream, and write to a byte array output stream since
							// we have to read into a byte array, but we don't know how large it
							// will need to be, and we don't want to resize it a bunch
							GZipInputStream gzi = new GZipInputStream(new MemoryStream(buf));
							MemoryStream bos = new MemoryStream(buf.Length);
							
							int count;
							byte[] tmp = new byte[2048];
							while((count = gzi.Read(tmp, 0, tmp.Length)) > 0)
							{
								bos.Write(tmp, 0, count);
							}
							
							// store uncompressed back to buffer
							buf = bos.ToArray();
							gzi.Close();
						}
						catch(IOException e) 
						{
							if(log.IsErrorEnabled)
							{
								log.Error(GetLocalizedString("loaditems uncompression IOException").Replace("$$Key$$", key), e);
							}
							throw new IOException(GetLocalizedString("loaditems uncompression IOException").Replace("$$Key$$", key), e);
						}
					}

					// we can only take out serialized objects
					if((flag & F_SERIALIZED) == 0) 
					{
						if(_primitiveAsString || asString) 
						{
							// pulling out string value
							if(log.IsInfoEnabled)
							{
								log.Info(GetLocalizedString("loaditems retrieve as string"));
							}
							o = Encoding.GetEncoding(_defaultEncoding).GetString(buf);
						}
						else 
						{
							// decoding object
							try 
							{
								o = NativeHandler.Decode(buf);    
							}
							catch(Exception e) 
							{
								if(log.IsErrorEnabled)
								{
									log.Error(GetLocalizedString("loaditems deserialize error").Replace("$$Key$$", key), e);
								}
								throw new IOException(GetLocalizedString("loaditems deserialize error").Replace("$$Key$$", key), e);
							}
						}
					}
					else 
					{
						// deserialize if the data is serialized
						try 
						{
							MemoryStream memStream = new MemoryStream(buf);
							o = new BinaryFormatter().Deserialize(memStream);
							if(log.IsInfoEnabled)
							{
								log.Info(GetLocalizedString("loaditems deserializing").Replace("$$Class$$", o.GetType().Name));
							}
						}
						catch(SerializationException e) 
						{
							if(log.IsErrorEnabled)
							{
								log.Error(GetLocalizedString("loaditems SerializationException").Replace("$$Key$$", key), e);
							}
							throw new IOException(GetLocalizedString("loaditems SerializationException").Replace("$$Key$$", key), e);
						}
					}

					// store the object into the cache
					hm[ key ] =  o ;
				
				
			}
		}

		/// <summary>
		/// Invalidates the entire cache.
		/// 
		/// Will return true only if succeeds in clearing all servers.
		/// </summary>
		/// <returns>success true/false</returns>
		public bool FlushAll() 
		{
			return FlushAll(null);
		}

		/// <summary>
		/// Invalidates the entire cache.
		/// 
		/// Will return true only if succeeds in clearing all servers.
		/// If pass in null, then will try to flush all servers.
		/// </summary>
		/// <param name="servers">optional array of host(s) to flush (host:port)</param>
		/// <returns>success true/false</returns>
		public bool FlushAll(ArrayList servers) 
		{
			bool success = true;
            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return false;
            }
				try 
				{
                    OperationResult opResult = memcachedProvider.Flush_All(0);

                    // if we get appropriate response back, then we return true
                    string line = "";
                    if (opResult.ReturnResult == Result.SUCCESS)
                        line = "OK";
                    success = (OK == line)
                        ? success && true
                        : false;
				}
				catch(Exception e) 
				{
					if(log.IsErrorEnabled)
					{
						log.Error(GetLocalizedString("flushall IOException"), e);
					}
                    success=false;
				}
            
           
			return success;
		}

		/// <summary>
		/// Retrieves stats for all servers.
		/// 
		/// Returns a map keyed on the servername.
		/// The value is another map which contains stats
		/// with stat name as key and value as value.
		/// </summary>
		/// <returns></returns>
		public Hashtable Stats() 
		{
			return Stats(null);
		}

		/// <summary>
		/// Retrieves stats for passed in servers (or all servers).
		/// 
		/// Returns a map keyed on the servername.
		/// The value is another map which contains stats
		/// with stat name as key and value as value.
		/// </summary>
		/// <param name="servers">string array of servers to retrieve stats from, or all if this is null</param>
		/// <returns>Stats map</returns>
		public Hashtable Stats(ArrayList servers) 
		{
			// array of stats Hashtables
			Hashtable statsMaps = new Hashtable();
            if (memcachedProvider == null)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error(GetLocalizedString("failed to get socket").Replace("$$Host$$", "localhost"));
                }
                return statsMaps;
            }
            try
            {
                OperationResult opResult = memcachedProvider.GetStatistics("");
                statsMaps = (Hashtable)opResult.Value;
            }
            catch (Exception e)
            { }
			return statsMaps;
		}

		private static ResourceManager _resourceManager = new ResourceManager("Memcached.ClientLibrary.StringMessages", typeof(MemcachedClient).Assembly);
		private static string GetLocalizedString(string key)
		{
			return _resourceManager.GetString(key);
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
