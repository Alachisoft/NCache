using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Config.NewDom;
using Microsoft.Win32;
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Licensing.NetCore.RegistryUtil;
using Alachisoft.NCache.Common.Logger;


namespace Alachisoft.NCache.Management
{
    public class ProfileUsageCollector
    {
        private static object _creationLock = new object();
        private HardwareProfile _hardwareProfile = new HardwareProfile();
        private UserProfile _userProfile = new UserProfile();

        static ProfileUsageCollector _profileUsageCollector = null;

        public static ProfileUsageCollector Instance
        {
            get
            {
                if (_profileUsageCollector == null)
                {
                    lock (_creationLock)
                    {
                        if (_profileUsageCollector == null)
                            _profileUsageCollector = new ProfileUsageCollector();
                    }
                }
                return _profileUsageCollector;
            }
        }

        public static CacheServer CacheServer { get; set; }

        public void PopulateHardwareProfile(string machineId)
        {
            _hardwareProfile.MachineName = System.Environment.MachineName;
            _hardwareProfile.EnvironmentName = string.Empty;
            if (String.IsNullOrEmpty(_hardwareProfile.MachineID))
            {
                if (!String.IsNullOrEmpty(machineId))
                {
                    _hardwareProfile.MachineID = machineId;
                }
                else
                {
                    _hardwareProfile.MachineID = Guid.NewGuid().ToString();
                }
            }
            _hardwareProfile.OperatingSystem = CacheServer.GetOSPlatform().ToString();
            _hardwareProfile.OtherServers = CacheServer.GetPossibleMachinesInCluster();
        }

        public HardwareProfile ReportHardwareProfile()
        {
            return _hardwareProfile;
        }

        public UserProfile ReportUserProfile()
        {
            return _userProfile;
        }

        public IDictionary<string, CacheServerConfig> PopulateCachingProfile()
        {
            IDictionary<string, CacheServerConfig> _cacheMetaInfo = new Dictionary<string, CacheServerConfig>();

            foreach (DictionaryEntry cache in CacheServer.Caches)
            {
                String cacheName = cache.Key.ToString().ToLower();
                if (_cacheMetaInfo != null)
                {
                    if (_cacheMetaInfo.ContainsKey(cache.Key.ToString()))
                        _cacheMetaInfo[cache.Key.ToString()] = DomHelper.convertToNewDom(CacheServer.GetCacheInfo(cacheName).CacheProps);
                    else
                        _cacheMetaInfo.Add(cache.Key.ToString(), DomHelper.convertToNewDom(CacheServer.GetCacheInfo(cacheName).CacheProps));
                }
            }

            return _cacheMetaInfo;
        }

        public void PopulateUserProfile()
        {
            try
            {
                RegUtil.LoadRegistry();
                _userProfile.Company = RegUtil.LicenseProperties.UserInfo.Company;
                _userProfile.Email = RegUtil.LicenseProperties.UserInfo.Email;
                _userProfile.FirstName = RegUtil.LicenseProperties.UserInfo.FirstName;
                _userProfile.LastName = RegUtil.LicenseProperties.UserInfo.LastName;
            }
            catch (Exception e)
            {
                NCacheServiceLogger.LogError($"Error occurred during PopulateUserProfile(). {e}");
            }
        }


    }
}
