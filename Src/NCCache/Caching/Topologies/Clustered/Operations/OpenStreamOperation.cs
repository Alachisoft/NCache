//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations
{
    public class OpenStreamOperation :ICompactSerializable
    {
        private string _key;
        private StreamModes _mode;
        private string _lockHandle;
        private string _group;
        private string _subGroup;
        private ExpirationHint _expHint;
        private EvictionHint _evictionHint;
        private OperationContext _operationContext;

        public OpenStreamOperation() { }

        public OpenStreamOperation(string key,string lockHandle,StreamModes mode, string group, string subGroup, ExpirationHint expHint, EvictionHint evHint,OperationContext operationContext)
        {
            _key = key;
            _group = group;
            _subGroup = subGroup;
            _expHint = expHint;
            _evictionHint = evHint;
            _mode = mode;
            _lockHandle = lockHandle;
            _operationContext = operationContext;
        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string LockHandle
        {
            get { return _lockHandle; }
            set { _lockHandle = value; }
        }

        public StreamModes Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }
        public string SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }

        public ExpirationHint ExpirationHint
        {
            get { return _expHint; }
            set { _expHint = value;  }
        }

        public EvictionHint EvictionHint
        {
            get { return _evictionHint; }
            set { _evictionHint = value; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
            set { _operationContext = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _key = reader.ReadObject() as string;
            _mode = (StreamModes)reader.ReadInt16();
            _group = reader.ReadObject() as string;
            _subGroup = reader.ReadObject() as string;
            _expHint = reader.ReadObject() as ExpirationHint;
            _evictionHint = reader.ReadObject() as EvictionHint;
            _lockHandle = reader.ReadObject() as string;
            _operationContext = reader.ReadObject() as OperationContext;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_key);
            writer.Write((Int16)_mode);
            writer.WriteObject(_group);
            writer.WriteObject(_subGroup);
            writer.WriteObject(_expHint);
            writer.WriteObject(_evictionHint);
            writer.WriteObject(_lockHandle);
            writer.WriteObject(_operationContext);
        }

        #endregion
    }

}
