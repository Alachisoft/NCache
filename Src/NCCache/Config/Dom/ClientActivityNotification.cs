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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ClientActivityNotification: ICloneable, ICompactSerializable
    {
        private bool _enabled;
        private int _retention;

        public ClientActivityNotification()
        {
            _enabled = false;
            _retention = 5;
        }

        [ConfigurationAttribute("enabled")]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        [ConfigurationAttribute("retention-period", "sec")]
        public int Retention
        {
            get { return _retention; }
            set { _retention = value; }
        }

        public object Clone()
        {
            ClientActivityNotification config = new ClientActivityNotification();
            config._enabled = _enabled;
            config._retention = _retention;
            return config;
        }

        public void Deserialize(CompactReader reader)
        {
            _enabled = reader.ReadBoolean();
            _retention = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_enabled);
            writer.Write(_retention);
        }
    }
}
