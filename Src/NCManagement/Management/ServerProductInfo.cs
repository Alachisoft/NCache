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
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Licensing.RegistryUtil;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using Runtime = Alachisoft.NCache.Runtime;
#if NETCORE
using System.Runtime.InteropServices;
#endif

namespace Alachisoft.NCache.Management.Management
{
    /// <summary>
    /// What Get-NCacheVersion reports about a server: its operating system, install type and
    /// service pack. It replaces ServerLicenseInfo, which also carried the registration (name,
    /// company, email, edition id, install code) that the OpenSource edition no longer has.
    /// </summary>
    [Serializable]
    public class ServerProductInfo : ICompactSerializable
    {
        private string _spVersion;
        private string _installationType = "";
        private string _operatingSystem = "";
        private bool _hideOperatingSystem = ServiceConfiguration.HideOperatingSystem;

        public ServerProductInfo()
        {
            Load();
        }

        public string SPVersion
        {
            get { return _spVersion; }
            set { _spVersion = value; }
        }

        public bool HideOperatingSystem
        {
            get { return _hideOperatingSystem; }
            set { _hideOperatingSystem = value; }
        }

        public string InstallationType { get { return _installationType; } }

        public string GetOS { get { return _operatingSystem; } }

        public void Load()
        {
            try
            {
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
                    if (string.IsNullOrEmpty(_spVersion))
                        _spVersion = (string)RegHelper.GetRegValue(RegHelper.ROOT_KEY, "SPVersion", 0);
                }
                catch { }
            }
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _spVersion = reader.ReadObject() as string;
            _installationType = reader.ReadObject() as string;
            _operatingSystem = reader.ReadObject() as string;
            HideOperatingSystem = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(SPVersion);
            writer.WriteObject(InstallationType);
            writer.WriteObject(GetOS);
            writer.Write(HideOperatingSystem);
        }
        #endregion
    }
}
