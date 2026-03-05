//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Licensing;
using Alachisoft.NCache.Licensing.RegistryUtil;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using Runtime = Alachisoft.NCache.Runtime;
#if NETCORE
using System.Runtime.InteropServices;
using Alachisoft.NCache.Licensing.RegistryUtil;
using Alachisoft.NCache.Common.Util;
#endif


namespace Alachisoft.NCache.Management.Management
{
    [Serializable]
    public class ServerLicenseInfo : ICompactSerializable
    {
        public string _version;
        public string _editionID;
        public string _registeredName;
        public string _companyName;
        public string _spVersion;
        public bool _isServerOnly;
        public string _email = "";
        public string _installationVesion;
        private string _firstName;
        private string _lastName;
        private decimal _memory;
        private int _logicalCores;
        private int _physicalCores;
        private string _installationType = "";
        private string _operatingSystem = "";
        private string _installCode = "";
        private bool _hideOperatingSystem = ServiceConfiguration.HideOperatingSystem;

        public ServerLicenseInfo(bool ignoreMac = false)
        {
            Load(ignoreMac);
        }

        public string SPVersion
        {
            get
            {
                return _spVersion;
            }
            set
            {
                _spVersion = value;
            }
        }

        public string EditionID
        {
            get
            {
                return _editionID;
            }
            set { _editionID = value; }
        }

        public string InstallationVersion
        {
            get
            {
                return _installationVesion;
            }
            set { _installationVesion = value; }
        }

        public bool HideOperatingSystem
        {
            get
            {
                return _hideOperatingSystem;
            }
            set
            {
                _hideOperatingSystem = value;
            }
        }
    
     
        public string Version
        {
            get
            {
                return _version;
            }
            set { _version = value; }
        }
       

        public string Email
        {
            get
            {
                return _email;
            }
            set { _email = value; }
        }

        public string RegisteredName
        {
            get
            {
                return _registeredName;
            }
            set { _registeredName = value; }
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

        
        public decimal Memory
        {
            get
            {
                return _memory;
            }
        }
        public int LogicalCores
        {
            get
            {
                return _logicalCores;
            }
        }
        public int PhysicalCores
        {
            get
            {
                return _physicalCores;
            }
        }
    

        public string InstallationType { get { return _installationType; } }

        public string GetOS { get { return _operatingSystem; } }
        public void Load(bool ignoreMac = false)
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
            try
            {

                _installCode = RegUtil.ReadInstallCode();
                _installationType = RegUtil.GetCompatibleInstallTypeFormatted();


            }
            catch (Exception)
            {


            }

#if NETCORE

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _operatingSystem = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _operatingSystem = "Windows";
            }
#else
            _operatingSystem = "Windows";
#endif

           


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

          


            if (RuntimeContext.CurrentContext == RtContextValue.NCACHE)
            {
                try
                {
                    if (string.IsNullOrEmpty(_spVersion))
                        _spVersion = (string)RegHelper.GetRegValue(RegHelper.ROOT_KEY, "SPVersion", 0);


                }
                catch { }
            }


          


         
          


        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _version = reader.ReadObject() as string;
            _editionID = reader.ReadObject() as string;
            _registeredName = reader.ReadObject() as string;
            _companyName = reader.ReadObject() as string;
            _spVersion = reader.ReadObject() as string;
            _email = reader.ReadObject() as string;
            _installationVesion = reader.ReadObject() as string;
            _firstName = reader.ReadObject() as string;
            _lastName = reader.ReadObject() as string;
            _physicalCores = reader.ReadInt32();
            _logicalCores = reader.ReadInt32(); ;
            _memory = reader.ReadDecimal();
            _installationType = reader.ReadObject() as string;
            _operatingSystem = reader.ReadObject() as string;
            HideOperatingSystem = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Version);
            writer.WriteObject(EditionID);
            writer.WriteObject(RegisteredName);
            writer.WriteObject(CompanyName);
            writer.WriteObject(SPVersion);

            writer.WriteObject(Email);
            writer.WriteObject(InstallationVersion);
            writer.WriteObject(_firstName);
            writer.WriteObject(_lastName);
            writer.Write(PhysicalCores);
            writer.Write(LogicalCores);
            writer.Write(Memory);
            writer.WriteObject(InstallationType);
            writer.WriteObject(GetOS);
            writer.Write(HideOperatingSystem);
        
        }
        #endregion
    }
}
