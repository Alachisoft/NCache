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
using System.Collections;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Security : ICloneable,ICompactSerializable
    {
        bool enabled;
        string domainController;
        string ldapPort;
        User[] users;

        public Security()
        { }

        [ConfigurationAttribute("enable-security")]//Changes for New Dom from enabled
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        [ConfigurationAttribute("domain-controller")]
        public string DomainController
        {
            get { return domainController; }
            set { domainController = value; }
        }
        
        [ConfigurationAttribute("port")]
        public string LdapPort
        {
            get { return ldapPort; }
            set { ldapPort = value; }
        }

        [ConfigurationSection("user")]
        public User[] Users
        {
            get { return users; }
            set { users = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Security security = new Security();
            security.Enabled = Enabled;
            security.DomainController = DomainController;
            security.Users = users;
            security.LdapPort = LdapPort;
            return security;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            enabled = reader.ReadBoolean();
            domainController = reader.ReadObject() as string;
            users = reader.ReadObject() as User[];
            ldapPort = reader.ReadObject() as string;

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(enabled);
            writer.WriteObject(domainController);
            writer.WriteObject(users);
            writer.WriteObject(ldapPort);
        }

        #endregion
    }
}
