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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common;
using Runtime = Alachisoft.NCache.Runtime;

#if NETCORE
using System.Runtime.InteropServices;
using Alachisoft.NCache.Licensing.NetCore.RegistryUtil;
#endif


namespace Alachisoft.NCache.Management.Management
{
    [Serializable]
    public class ServerLicenseInfo : ICompactSerializable
    {
        public string _companyName;
        public string _email = "";
        private string _firstName;
        private string _lastName;
        public string _registeredName;

        public ServerLicenseInfo()
        {
            Load();
        }

       public string Email
        {
            get
            {
                return _email;
            }
            set { _email = value; }
        }

       public string CompanyName
        {
            get
            {
                return _companyName;
            }
            set
            {
                _companyName = value;
            }
        }

       public string FirstName
        {
            get
            {
                return _firstName;
            }
            set { _firstName = value; }
        }
        public string LastName
        {
            get
            {
                return _lastName;
            }
            set { _lastName = value; }
        }

        public string RegisteredName
        {
            get
            {
                return _registeredName;
            }
            set { _registeredName = value; }
        }

        public void Load()
        {            


            if (RuntimeContext.CurrentContext == RtContextValue.NCACHE)
            {
                try
                {
                    string USER_KEY = RegHelper.ROOT_KEY + @"\UserInfo";
                    _companyName = (string)RegHelper.GetRegValue(USER_KEY, "company", 0);
                }
                catch
                {

                }
            }

            
            if (RuntimeContext.CurrentContext == RtContextValue.NCACHE)
            {
                try
                {
                    string USER_KEY = RegHelper.ROOT_KEY + @"\UserInfo";
                    _firstName = (string)RegHelper.GetRegValue(USER_KEY, "firstname", 0);
                    _lastName = (string)RegHelper.GetRegValue(USER_KEY, "lastname", 0);
                    _registeredName = _firstName + " " + _lastName;
                }
                catch
                {

                }
            }


            if (RuntimeContext.CurrentContext == RtContextValue.NCACHE)
            {
                try
                {

                    string USER_KEY = RegHelper.ROOT_KEY + @"\UserInfo";
                    _email = (string)RegHelper.GetRegValue(USER_KEY, "email", 0);
                }
                catch
                {

                }
            }


#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    RegUtil.LoadRegistry();
                    _companyName = RegUtil.LicenseProperties.UserInfo.Company;
                    _email = RegUtil.LicenseProperties.UserInfo.Email;
                    _firstName = RegUtil.LicenseProperties.UserInfo.FirstName;
                    _lastName = RegUtil.LicenseProperties.UserInfo.LastName;
                    _registeredName = _firstName + " " + _lastName;
                }
                catch (Exception) {}
            }
#endif
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _registeredName = reader.ReadObject() as string;
            _companyName = reader.ReadObject() as string;

            _email = reader.ReadObject() as string;
            _firstName = reader.ReadObject() as string;
            _lastName = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {

            writer.WriteObject(RegisteredName);
            writer.WriteObject(CompanyName);

            writer.WriteObject(Email);
            writer.WriteObject(_firstName);
            writer.WriteObject(_lastName);
        }
        #endregion
    }
}
