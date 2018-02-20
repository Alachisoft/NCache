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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    [Serializable]
    public class ExtensibleDependency : DependencyHint, ICompactSerializable
    {

        private Runtime.Dependencies.ExtensibleDependency _extDependency;


        /// <summary>
        /// Constructor.
        /// </summary>
        public ExtensibleDependency()
        {
            base._hintType = ExpirationHintType.ExtensibleDependency;
        }

        /// <summary>
        /// Constructor.
        /// </summary>

        public ExtensibleDependency(Runtime.Dependencies.ExtensibleDependency extDependency)
        {
            base._hintType = ExpirationHintType.ExtensibleDependency;
            _extDependency = extDependency;
        }

        internal override bool Reset(CacheRuntimeContext context)
        {

            base.Reset(context);
            return _extDependency.Initialize();
        }

        /// <summary> Returns true if the hint is indexable in expiration manager, otherwise returns false.</summary>
        public override bool IsIndexable { get { return true; } }        

        /// <summary>
        /// Always returns false. user should override this property.
        /// </summary>
        public override bool HasChanged
        {
            get { return _extDependency.HasChanged; }
        }

        protected override void DisposeInternal()
        {
            if (_extDependency != null) _extDependency.Dispose();
        }
        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _extDependency = reader.ReadObject() as Runtime.Dependencies.ExtensibleDependency;
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.WriteObject(_extDependency);
        }

        #endregion
    }
}