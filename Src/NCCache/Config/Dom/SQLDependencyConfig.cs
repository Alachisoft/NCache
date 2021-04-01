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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class SQLDependencyConfig : ICloneable,ICompactSerializable
    {
        private bool _useDefault = true;

        [ConfigurationAttribute("use-default")]
        public bool UseDefault
        {
            get { return _useDefault; }
            set { _useDefault = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            SQLDependencyConfig sqlDependencyConfig = new SQLDependencyConfig();
                sqlDependencyConfig.UseDefault = this.UseDefault;
            return sqlDependencyConfig;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _useDefault = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_useDefault);
        }

        #endregion
    }
}
