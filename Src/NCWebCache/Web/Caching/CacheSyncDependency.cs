// Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// A client application can have more than one cache instances initialized.
    /// CacheSyncDependency keeps the items present in one cache synchronized with 
    /// the items present in another cache. 
    /// </summary>
    /// <remarks>
    /// You can add items with CacheSyncDependency to your application's cache with the 
    /// <see cref="Cache.Add"/> and <see cref="Cache.Insert"/> methods.
    /// <para>When you add an item to an application's <see cref="Cache"/> object with 
    /// <see cref="CacheSyncDependency"/>, it monitors the remote cache (the cache you want your local cache
    /// to be synchronized with) for changes. As soon as an item is updated in or removed from the remote cache   
    /// this change is automatically updated in the local cache if the CacheSyncDependency was provided with the
    /// cache items.
    /// This helps you keep your local cache synchronized with the remote cache all the time. 
    /// </para>
    /// </remarks>
    /// 
    [Serializable]
    public class CacheSyncDependency : Runtime.Serialization.ICompactSerializable
    {
        private string _cacheId;
        private string _key;
        private string _server;
        private int _port;


        /// <summary>
        /// Initializes a new instance of the CacheSyncDependency with the 
        /// specified parameters. Internally it tries to initialize the remote cache.
        /// If the cache can not be initialized, it throws the exception describing the cause of
        /// failure. The remote cache must be running as outproc even if it is on the same machine.
        /// The information to connect to the remote cache instance (like server-name and server-port)
        /// are picked from the 'client.ncconf'.
        /// </summary>
        /// <param name="remoteCacheId">The unique id of the remote cache</param>
        /// <param name="key">The key of the item in the remote cache with which the 
        /// local cache item will be kept synchronized.</param>
        public CacheSyncDependency(string remoteCacheId, string key)
        {
            _cacheId = remoteCacheId;
            _key = key;
        }


        /// <summary>
        /// Initializes a new instance of the CacheSyncDependency with the 
        /// specified parameters. Internally it tries to initialize the remote cache.
        /// If the cache can not be initialized, it throws the exception describing the cause of
        /// failure. The remote cache must be running as outproc even if it is on the same machine.
        /// </summary>
        /// <param name="remoteCacheId">The unique id of the remote cache</param>
        /// <param name="key">The key of the item in the remote cache with which the 
        /// local cache item will be kept synchronized.</param>
        /// <param name="server">The name of the server where the remote cache is running</param>
        /// <param name="port">The port used by the client to connect to the server</param>
        public CacheSyncDependency(string remoteCacheId, string key, string server, int port) : this(remoteCacheId, key)
        {
            _server = server;
            _port = port;
        }


        /// <summary>
        /// The unique Id of the remote cache
        /// </summary>
        public string CacheId
        {
            get { return _cacheId; }
        }

        /// <summary>
        /// The key of the item in the remote cache with which the local
        /// cache item needs to be synchronized.
        /// </summary>
        public string Key
        {
            get { return _key; }
        }

        /// <summary>
        /// The name of the server where the remote cache is running.
        /// </summary>
        public string Server
        {
            get { return _server; }
        }

        /// <summary>
        /// The server port that is used by the clients to connect to 
        /// the server.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }


        #region ICompact Serializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cacheId = (string) reader.ReadObject();
            _key = (string) reader.ReadObject();
            _server = (string) reader.ReadObject();
            _port = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cacheId);
            writer.WriteObject(_key);
            writer.WriteObject(_server);
            writer.Write(_port);
        }

        #endregion
    }
}