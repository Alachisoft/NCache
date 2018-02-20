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
    public class AbsoluteExpiration : ICloneable, ICompactSerializable
    {
       // private long _default;
        private bool _longerEnabled;

        
        private long _longer;
        private bool _defaultEnabled;

        
        private long _default;

        public AbsoluteExpiration()
        {
           // _default = 5;
            _longerEnabled = false;
            _longer = 0;
            _defaultEnabled = false;
            _default = 0;
        }

        [ConfigurationAttribute("longer-enabled")]
        public bool LongerEnabled
        {
            get { return _longerEnabled; }
            set { _longerEnabled = value; }
        }
        [ConfigurationAttribute("longer-value")]
        public long Longer
        {
            get { return _longer; }
            set { _longer = value; }
        }
        [ConfigurationAttribute("default-enabled")]
        public bool DefaultEnabled
        {
            get { return _defaultEnabled; }
            set { _defaultEnabled = value; }
        }
        [ConfigurationAttribute("default-value")]
        public long Default
        {
            get { return _default; }
            set { _default = value; }
        }


        #region ICloneable memebers

        public object Clone()
        {
            AbsoluteExpiration exp = new AbsoluteExpiration();
           // exp.Default = Default;
            exp.LongerEnabled = LongerEnabled;
            exp.Longer = Longer;
            exp.DefaultEnabled = DefaultEnabled;
            exp.Default = Default;
            return exp;
        }

        #endregion

        #region ICompactSerializable members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _longerEnabled = reader.ReadBoolean();
            _longer = reader.ReadInt64();
            _defaultEnabled = reader.ReadBoolean();
            _default = reader.ReadInt64();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_longerEnabled);
            writer.Write(_longer);
            writer.Write(_defaultEnabled);
            writer.Write(_default);
        }

        #endregion
    }
}
