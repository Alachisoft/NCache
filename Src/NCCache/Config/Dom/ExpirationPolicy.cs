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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ExpirationPolicy : ICloneable, ICompactSerializable
    {
        bool _isExpirationEnabled = false;
        AbsoluteExpiration _absoluteExpiration;
        SlidingExpiration _slidingExpiration;

        public ExpirationPolicy()
        {
            _isExpirationEnabled = false;
            _absoluteExpiration = new AbsoluteExpiration();
            _slidingExpiration = new SlidingExpiration();
        }

        [ConfigurationSection("absolute-expiration")]
        public AbsoluteExpiration AbsoluteExpiration
        {
            get { return _absoluteExpiration; }
            set { _absoluteExpiration = value; }
        }

        [ConfigurationSection("sliding-expiration")]
        public SlidingExpiration SlidingExpiration
        {
            get { return _slidingExpiration; }
            set { _slidingExpiration = value; }
        }



        [ConfigurationAttribute("enabled")]
        public bool IsExpirationEnabled
        {
            get { return _isExpirationEnabled; }
            set { _isExpirationEnabled = value; }
        }
        
        #region ICompact Serializable members
        
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _isExpirationEnabled = reader.ReadBoolean();
            _absoluteExpiration  = reader.ReadObject() as AbsoluteExpiration;
            _slidingExpiration   = reader.ReadObject() as SlidingExpiration;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_isExpirationEnabled);
            writer.WriteObject(_absoluteExpiration);
            writer.WriteObject(_slidingExpiration);
        }

        #endregion

        #region ICloneable Members
        public object Clone()
        {
            ExpirationPolicy expPolicy = new ExpirationPolicy();
            expPolicy.IsExpirationEnabled = this._isExpirationEnabled;
            expPolicy.AbsoluteExpiration = this._absoluteExpiration;
            expPolicy._slidingExpiration = this._slidingExpiration;
            return expPolicy;
        }
        #endregion
    
    }
}
