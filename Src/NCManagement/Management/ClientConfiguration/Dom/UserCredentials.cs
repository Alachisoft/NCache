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

namespace Alachisoft.NCache.Management.ClientConfiguration.Dom
{
    [Serializable]
    internal class UserCredentials : ICloneable, ICompactSerializable
    {
        private string _userId;
        private string _password;

        [ConfigurationAttribute("user-id")]
        internal string UserId
        {
            get { return _userId; }
            set { _userId = value; }
        }

        [ConfigurationAttribute("password")]
        internal string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            UserCredentials credentials = new UserCredentials();
            credentials._userId = _userId;
            credentials._password = _password;
            
            return credentials;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _userId = reader.ReadObject() as string;
            _password = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_userId);
            writer.WriteObject(_password);
        }

        #endregion
    }
}
