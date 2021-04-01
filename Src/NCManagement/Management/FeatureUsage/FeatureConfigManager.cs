using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.NewDom;
using Alachisoft.NCache.Licensing;
using Renci.SshNet.Messages.Connection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Xml;

namespace Alachisoft.NCache.Management
{
    public class FeatureConfigManager
    {
        private const string DIRNAME = "log-files";
        private const string FILENAME = "ncache-feature-usage.xml";
        internal string ENDSTRING = "\r\n";
        private readonly string requestDomain = "https://app.alachisoft.com/services/usage.php";

        private string c_configDir = DIRNAME;
        private string c_configFileName = FILENAME;

        private readonly object _lock = new object();
        private FeatureUsage _configuration;
        private FeatureUsage _finalConfiguration;


        #region Configuration
        public void LoadConfiguration()
        {
            try
            {
                _finalConfiguration = new FeatureUsage();
                _finalConfiguration.Profile = new Profile();
                CombinePath();

                //If the file does not exist, create the file.
                if (!File.Exists(c_configFileName))
                {
                    lock (_lock)
                    {
                        SaveDummyConfiguration();
                    }
                }
                else
                {
                    LoadXml();
                }

            }
            catch (ManagementException ex)
            {
                c_configFileName = "";
                throw;
            }
            catch (Exception ex)
            {
                c_configFileName = "";
                throw;
            }
        }

        private void SaveDummyConfiguration()
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<feature-usage>\r\n");
            xml.Append("\r\n</feature-usage>");
            WriteXmlToFile(xml.ToString());
        }


        public void SaveConfiguration()
        {
            WriteXmlToFile(ToXml());
        }

        private void WriteXmlToFile(String dataToWrite)
        {
            if (_finalConfiguration == null)
                return;
            if (c_configFileName == null || c_configFileName == "")
                CombinePath();

            lock (_lock)
            {
                try
                {
                    using (FileStream fs = new FileStream(c_configFileName, FileMode.Create))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.Write(dataToWrite);
                            sw.Flush();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }

            }
        }

        public bool PostConfiguration(out string featureReportedMessage)
        {
            featureReportedMessage = String.Empty;
            int retryCount = ServiceConfiguration.UsageFailureRetriesCount;
            while (retryCount > 0)
            {
                try
                {
                    CombinePath();

                    if (!File.Exists(c_configFileName))
                        throw new Exception("File does not exist " + c_configFileName);

                    XmlDocument document = new XmlDocument();
                    document.Load(c_configFileName);
                    string data = document.InnerXml;
                    HttpWebResponse response = SendPostRequest(requestDomain, data);
                    string result = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    if (result.Contains("Exception") || result.Contains("error"))
                    {
                        featureReportedMessage = $"An error occurred while post request to server: Failed to post NCacheFeatureUsage. {result}";
                        return false;
                    }

                    else if (response.StatusCode == HttpStatusCode.OK)
                    {
                        featureReportedMessage = "NcacheFeatureUsageReport was successfully posted";
                        ResetConfiguration();
                    }
                    break;
                }
                catch (Exception e)
                {
                    retryCount--;
                    if (e.Message.Contains("File does not exist " + c_configFileName) || retryCount <= 0)
                        throw new Exception("Error occur during reporting " + e.ToString());
                    return false;
                }
            }
            return true;
        }

        public void ResetConfiguration()
        {
            LoadConfiguration();

            if (_configuration != null)
            {
                _configuration.FeatureDetails = null;

                if (_configuration.Profile != null)
                {
                    _configuration.Profile.CachingProfile = null;

                    if (_configuration.Profile.ClientProfile != null)
                    {
                        _configuration.Profile.ClientProfile.MaximumCores = 0;
                        _configuration.Profile.ClientProfile.MinimumCores = 0;
                        _configuration.Profile.ClientProfile.MaximumMemory = 0;
                        _configuration.Profile.ClientProfile.MinimumMemory = 0;
                        _configuration.Profile.ClientProfile.MaximumConnectedClients = 0;
                        _configuration.Profile.ClientProfile.OperatingSystem = string.Empty;
                        _configuration.Profile.ClientProfile.Platform = string.Empty;
                    }

                    if (_configuration.Profile.HardwareProfile != null)
                    {
                        _configuration.Profile.HardwareProfile.OtherServers = string.Empty;
                    }
                }
            }

            _finalConfiguration = _configuration;
            SaveConfiguration();
        }

        public void UpdateFeatureUsageInfo(string lastPostingTime)
        {
            if (_finalConfiguration != null)
            {
                if (_configuration != null)
                {
                    if (_configuration.Profile != null)
                        _finalConfiguration.Profile = _configuration.Profile;
                    if (_configuration.FeatureDetails != null)
                        _finalConfiguration.FeatureDetails = _configuration.FeatureDetails;
                }

                _finalConfiguration.Edition = "OSS";
                _finalConfiguration.Version = ProductVersion.GetVersion();
                _finalConfiguration.UpdateTime = DateTime.Now.ToString();
                _finalConfiguration.LastPostingTime = lastPostingTime;
#if NETCORE
                _finalConfiguration.Platform = ".NET Core";
#endif

#if !NETCORE
                _finalConfiguration.Platform = ".NET Framework";
#endif
            }
        }

        public void UpdateHardwareProfile(HardwareProfile hardwareProfile)
        {
            if (hardwareProfile != null)
            {

                if (_configuration != null && _configuration.Profile != null && _configuration.Profile.HardwareProfile != null)

                    if (!String.IsNullOrEmpty(_configuration.Profile.HardwareProfile.OtherServers))
                    {
                        string[] configServers = _configuration.Profile.HardwareProfile.OtherServers.Split(',');
                        List<string> serverMachineIdList = hardwareProfile.OtherServers.Split(',').ToList<string>();

                        foreach (string server in configServers)
                        {
                            if (!serverMachineIdList.Contains(server))
                                serverMachineIdList.Add(server);
                        }

                        hardwareProfile.OtherServers = String.Join(",", serverMachineIdList);
                    }

                _finalConfiguration.Profile.HardwareProfile = hardwareProfile;

            }

            else
            {
                if (_configuration != null && _configuration.Profile != null && _configuration.Profile.HardwareProfile != null)
                {
                    _finalConfiguration.Profile.HardwareProfile = hardwareProfile;
                }
            }
        }

        public void UpdateUserInfo(UserProfile userProfile)
        {
            if (userProfile != null)
            {
                _finalConfiguration.Profile.UserProfile = userProfile;
            }
            else
            {
                if (_configuration != null && _configuration.Profile != null && _configuration.Profile.UserProfile != null)
                {
                    _finalConfiguration.Profile.UserProfile = _configuration.Profile.UserProfile;
                }
            }

        }

        public string GetMachineId()
        {
            string machineID = string.Empty;

            if (_configuration == null)
                LoadConfiguration();

            if (_configuration != null && _configuration.Profile != null)
            {
                HardwareProfile hardwareProfile = _configuration.Profile.HardwareProfile;
                if (hardwareProfile != null)
                    machineID = hardwareProfile.MachineID;
            }

            return machineID;
        }

        public string GetLastPostingTime()
        {
            string lastPostingTime = string.Empty;

            if (_configuration == null)
                LoadConfiguration();

            if (_configuration != null)
            {
                lastPostingTime = _configuration.LastPostingTime;
            }

            return lastPostingTime;
        }
        #endregion

        #region MergingWithConfiguration
        public void MergeFeaturesWithConfiguration(IDictionary<string, Common.FeatureUsageData.Feature> featureReport)
        {
            FeatureDetails featureDetails = new FeatureDetails();
            if (_configuration != null && _configuration.FeatureDetails != null)
            {
                IDictionary<string, Common.FeatureUsageData.Dom.Feature> features = new Dictionary<string, Common.FeatureUsageData.Dom.Feature>();

                if (_configuration.FeatureDetails.Features != null)
                {
                    foreach (Common.FeatureUsageData.Dom.Feature configFeature in _configuration.FeatureDetails.Features)
                    {
                        Common.FeatureUsageData.Dom.Feature feature = configFeature;

                        if (featureReport != null && featureReport.ContainsKey(configFeature.Name))
                        {
                            feature = MergeFeatures(configFeature, featureReport[configFeature.Name]);
                        }

                        features.Add(feature.Name, feature);
                    }

                }

                if (featureReport != null && featureReport.Count > 0)
                {
                    foreach (var reportedFeature in featureReport)
                    {
                        if (!features.ContainsKey(reportedFeature.Key))
                        {
                            features.Add(reportedFeature.Key, new Feature
                            {
                                Name = reportedFeature.Value.Name,
                                LastUsedOn = reportedFeature.Value.LastUsageTime.Date.ToString("d"),
                                CreationTime = reportedFeature.Value.CreationTime.ToString("d"),
                                Subfeatures = Common.FeatureUsageData.Feature.ConvertFeatures(reportedFeature.Value.Features())
                            });
                        }
                    }
                }

                featureDetails.Features = features.Values.ToArray();
            }
            else
            {
                Common.FeatureUsageData.Dom.Feature[] features = Alachisoft.NCache.Common.FeatureUsageData.Feature.ConvertFeatures(featureReport);
                featureDetails.Features = features;
            }

            _finalConfiguration.FeatureDetails = featureDetails;
        }

        public void MergeCachingProfile(IDictionary<string, CacheServerConfig> cachesInfo)
        {
            Dictionary<string, Topology> topologyInfo = new Dictionary<string, Topology>();
            CachingProfile cachingTopologies = new CachingProfile();

            if (cachesInfo != null && cachesInfo.Count > 0)
            {
                foreach (var cacheInfo in cachesInfo)
                {
                    string topology = string.Empty;
                    int clusterSize = 1;
                    Topology cacheTopology;

                    if (cacheInfo.Value.CacheSettings != null)
                        topology = cacheInfo.Value.CacheSettings.CacheTopology.Topology;

                    if (cacheInfo.Value.CacheDeployment != null)
                        clusterSize = cacheInfo.Value.CacheDeployment.Servers.ServerNodeList.Length;

                    if (topologyInfo.ContainsKey(topology))
                    {
                        cacheTopology = topologyInfo[topology];
                        if (clusterSize > cacheTopology.MaxClusterSize)
                            cacheTopology.MaxClusterSize = clusterSize;
                        cacheTopology.NoOfCaches++;
                        topologyInfo[topology] = cacheTopology;
                    }
                    else
                    {
                        cacheTopology = new Topology { Name = topology, MaxClusterSize = clusterSize, NoOfCaches = 1 };
                        topologyInfo.Add(topology, cacheTopology);
                    }
                }

            }

            if (_configuration != null && _configuration.Profile != null && _configuration.Profile.CachingProfile != null && _configuration.Profile.CachingProfile.Topologies != null)
            {
                Topology[] configTopologies = _configuration.Profile.CachingProfile.Topologies;
                foreach (Topology configTopology in configTopologies)
                {
                    if (!topologyInfo.ContainsKey(configTopology.Name))
                        topologyInfo.Add(configTopology.Name, configTopology);
                }
            }

            cachingTopologies.Topologies = topologyInfo.Values.ToArray();

            if (_finalConfiguration != null && _finalConfiguration.Profile == null)
            {
                _finalConfiguration.Profile = new Profile();
            }

            if (_finalConfiguration != null && _finalConfiguration.Profile != null)
                _finalConfiguration.Profile.CachingProfile = cachingTopologies;

        }
        #endregion

        #region HelperMethods
        private void LoadXml()
        {
            if (String.IsNullOrEmpty(c_configFileName))
                CombinePath();

            ConfigurationBuilder configBuilder = new ConfigurationBuilder(c_configFileName);
            configBuilder.RegisterRootConfigurationObject(typeof(FeatureUsage));
            configBuilder.ReadConfiguration();

            FeatureUsage featureConfiguration = null;
            Object[] configuration = configBuilder.Configuration;

            if (configuration != null && configuration.Length > 0)
            {
                for (int i = 0; i < configuration.Length; i++)
                {
                    featureConfiguration = configuration[i] as FeatureUsage;
                    break;
                }
            }

            _configuration = featureConfiguration;

            if (_configuration == null)
                _configuration = new FeatureUsage();
        }

        private Feature MergeFeatures(Feature configFeature, Common.FeatureUsageData.Feature reportedFeature)
        {
            Feature finalFeature = new Feature
            {
                Name = configFeature.Name,
                LastUsedOn = DateTime.Compare(Convert.ToDateTime(configFeature.LastUsedOn), reportedFeature.LastUsageTime) > 0 ? configFeature.LastUsedOn : reportedFeature.LastUsageTime.Date.ToString("d")
            };

            if (string.IsNullOrEmpty(configFeature.CreationTime))
            {
                finalFeature.CreationTime = reportedFeature.CreationTime.ToString("d");
            }
            else
            {
                finalFeature.CreationTime = configFeature.CreationTime;
            }

            Feature[] subfeatures = Common.FeatureUsageData.Feature.ConvertFeatures(reportedFeature.Features());
            if (reportedFeature.Features() != null && reportedFeature.Features().Count > 0 && configFeature.Subfeatures != null && configFeature.Subfeatures.Length > 0)
            {
                IDictionary<string, Feature> commonFeatures = new Dictionary<string, Feature>();
                for (int i = 0; i < configFeature.Subfeatures.Length; i++)
                {
                    if (reportedFeature.Features().ContainsKey(configFeature.Subfeatures[i].Name))
                    {
                        commonFeatures.Add(configFeature.Subfeatures[i].Name, MergeFeatures(configFeature.Subfeatures[i], reportedFeature.GetFeature(configFeature.Subfeatures[i].Name)));
                    }
                    else
                    {
                        commonFeatures.Add(configFeature.Subfeatures[i].Name, configFeature.Subfeatures[i]);
                    }
                }
                foreach (var feature in reportedFeature.Features())
                {
                    if (!commonFeatures.ContainsKey(feature.Key))
                    {
                        commonFeatures.Add(feature.Key, new Feature
                        {
                            Name = feature.Value.Name,
                            LastUsedOn = feature.Value.LastUsageTime.ToString("d"),
                            CreationTime = feature.Value.CreationTime.ToString("d"),
                            Subfeatures = Common.FeatureUsageData.Feature.ConvertFeatures(feature.Value.Features())
                        });
                    }
                }

                finalFeature.Subfeatures = commonFeatures.Values.ToArray();
            }

            else if (reportedFeature.Features() != null && reportedFeature.Features().Count > 0)
            {
                subfeatures = Common.FeatureUsageData.Feature.ConvertFeatures(reportedFeature.Features());
                finalFeature.Subfeatures = subfeatures;
            }

            else
            {
                finalFeature.Subfeatures = configFeature.Subfeatures;
            }

            return finalFeature;
        }

        private string ToXml()
        {
            StringBuilder sb = new StringBuilder();

            object[] configuration = new object[1];
            configuration[0] = _finalConfiguration;
            ConfigurationBuilder cfgBuilder = new ConfigurationBuilder(configuration);
            cfgBuilder.RegisterRootConfigurationObject(typeof(FeatureUsage));
            sb.Append(cfgBuilder.GetXmlString());
            _configuration = null;

            return sb.ToString();
        }

        private void CombinePath()
        {
            c_configDir = AppUtil.InstallDir;

            if (c_configDir == null || c_configDir.Length == 0)
            {
                throw new ManagementException("Missing installation folder information");
            }

            c_configDir = Path.Combine(c_configDir, DIRNAME);
            if (!Directory.Exists(c_configDir))
                Directory.CreateDirectory(c_configDir);

            c_configFileName = Path.Combine(c_configDir, FILENAME);
        }

        public HttpWebResponse SendPostRequest(string url, string postData)
        {
            var data = Encoding.ASCII.GetBytes(postData);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            //request.Headers.Add("Authorization", $"Bearer { "privateRequest"}");
            request.ContentType = "text/xml";
            request.ContentLength = data.Length;
            request.Accept = "application/string";

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            return (HttpWebResponse)request.GetResponse();

        }
        #endregion

        public void SyncClientProfiletoPresistanceState(ClientProfileDom clientProfile)
        {

            if (_configuration != null && _configuration.Profile != null && _configuration.Profile.ClientProfile != null)
            {
                Common.FeatureUsageData.ClientUsage clientUsage = new Common.FeatureUsageData.ClientUsage();

                clientUsage.UpdateClientUsageProfile(clientProfile);

                if (!hasDefualtValues(_configuration.Profile.ClientProfile))
                    clientUsage.UpdateConfigUsageProfile(_configuration.Profile.ClientProfile);

                var domProfile = clientUsage.GetClientProfile();

                _finalConfiguration.Profile.ClientProfile = domProfile;
            }
            else
            {
                _finalConfiguration.Profile.ClientProfile = clientProfile;
            }
        }

        private bool hasDefualtValues(ClientProfileDom clientProfile)
        {
            return clientProfile.MaximumCores == 0 && clientProfile.MinimumCores == 0 && clientProfile.MinimumMemory == 0 && clientProfile.MaximumMemory == 0;
        }
    }
}
