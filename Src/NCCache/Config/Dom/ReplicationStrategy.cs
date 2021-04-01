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
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
	[Serializable]
	public class ReplicationStrategy : ICloneable,ICompactSerializable
	{
		bool _synchronous = true;

		public ReplicationStrategy() { }

		[ConfigurationAttribute("synchronous")]
		public bool ReplicateSynchronous
		{
			get { return _synchronous; }
			set { _synchronous = value; }
		}

		#region ICloneable Members

		public object Clone()
		{
			ReplicationStrategy replicationStrategy = new ReplicationStrategy();
			replicationStrategy._synchronous = _synchronous;
			return replicationStrategy;
		}

		#endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _synchronous = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_synchronous);
        }

        #endregion
    }

}
