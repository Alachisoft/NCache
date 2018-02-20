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
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.Web.Caching.RemoteCacheDependency
{
    [Serializable]
    public class RemoteCacheKeyDependency : ExtensibleDependency
    {
        bool hasChanged = false;
        string _remoteCacheKey;
        string _remoteCacheID;

        public string RemoteCacheKey
        {
            set { this._remoteCacheKey = value; }
            get { return this._remoteCacheKey; }
        }

        public string RemoteCacheID
        {
            set { this._remoteCacheID = value; }
            get { return this._remoteCacheID; }
        }


        public RemoteCacheKeyDependency(string remoteCacheKey, string remoteCacheID)
        {
            RemoteCacheID = remoteCacheID;
            RemoteCacheKey = remoteCacheKey;
        }

        public override bool Initialize()
        {
            return RemoteCacheKeyDependencyManager.RegisterRemoteCacheDependency(this);
        }

        public override bool HasChanged
        {
            get { return hasChanged = RemoteCacheKeyDependencyManager.HasExpired(this); }
        }

        protected override void DependencyDispose()
        {
            RemoteCacheKeyDependencyManager.UnregisterRemoteCacheDependency(this);
        }
    }
}