using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.FeatureUsageData;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.NewDom;
using Alachisoft.NCache.Licensing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace Alachisoft.NCache.Management
{

    // replace all hashtable into generic implmentation IDictionary<string,Feature>; 
    public class FeatureDataManager
    {
        private int _autoStartDelay = 0;
        private Thread _dataCollector;
        private Thread _dataPosting;
        private CacheServer _cacheServer;
        private IDictionary<string, CacheServerConfig> _cachingProfile;
        private IDictionary<string, Common.FeatureUsageData.Feature> _mergedFeatureReport;
        private IDictionary<string, Dictionary<string, Common.FeatureUsageData.Feature>> _accumulatedDataFromCaches;
        private FeatureConfigManager _featureConfigManager;
        private DateTime _installTime;

        public FeatureDataManager(CacheServer cacheServer)
        {

            _autoStartDelay = ServiceConfiguration.InitialLoggingDelayAtStartup;
            _cacheServer = cacheServer;
            _accumulatedDataFromCaches = new Dictionary<string, Dictionary<string, Common.FeatureUsageData.Feature>>();
            _featureConfigManager = new FeatureConfigManager();
            _installTime = Directory.GetCreationTime(AppUtil.InstallDir);
            ProfileUsageCollector.CacheServer = cacheServer;

        }

        #region Data Collection
        public void StartGatheringData()
        {
            if (_dataCollector == null && ServiceConfiguration.EnableFeatureUsageLogging)
            {
                _dataCollector = new Thread(new ThreadStart(PerformDataCollection));
                _dataCollector.IsBackground = true;
                _dataCollector.Name = "FeatureDataCollection";
                _dataCollector.Start();
            }
        }
        public void StoptGatheringData()
        {
            if (_dataCollector != null && _dataCollector.IsAlive)
            {
#if !NETCORE
                _dataCollector.Abort();
#else
                _dataCollector.Interrupt();
#endif
            }

            _featureConfigManager = null;
            _cacheServer = null;
            _accumulatedDataFromCaches = null;
            _cachingProfile = null;

        }

        //Thread is initiated to get all the running caches featureUsageReport.The Thread must sleep for an autostart delay for all the configured caches to get started( caches set with autostart property).
        private void PerformDataCollection()
        {
            try
            {
                bool threadAbort = false;
                if (_autoStartDelay > 0)
                    Thread.Sleep(_autoStartDelay * 1000);

                while (!threadAbort)
                {
                    try
                    {
                        _mergedFeatureReport = null;

                        //  load config state from configuration file. 
                        _featureConfigManager.LoadConfiguration();

                        // Get user profile state from running service and syncronize it with persistance state. 
                        _featureConfigManager.UpdateFeatureUsageInfo(_featureConfigManager.GetLastPostingTime());
                        // Get hardward profile state from running service and update the persistant state . 
                        ProfileUsageCollector.Instance.PopulateHardwareProfile(_featureConfigManager.GetMachineId());
                        _featureConfigManager.UpdateHardwareProfile(ProfileUsageCollector.Instance.ReportHardwareProfile());

                        ProfileUsageCollector.Instance.PopulateUserProfile();
                        _featureConfigManager.UpdateUserInfo(ProfileUsageCollector.Instance.ReportUserProfile());


                        // Get caching profile state from running service and syncronize it with persistance state.
                        _cachingProfile = ProfileUsageCollector.Instance.PopulateCachingProfile();
                        _featureConfigManager.MergeCachingProfile(_cachingProfile);

                        ClientUsage clientUsage = new ClientUsage();
                        foreach (DictionaryEntry cache in _cacheServer.Caches)
                        {
                            String cacheName = cache.Key.ToString().ToLower();
                            GetAndAccumulateCacheFeatureReport(cacheName);
                            GetAndAccumulateClientProfileReport(cacheName, clientUsage);
                        }

                        MergeServiceFeatureReport();

                        _featureConfigManager.SyncClientProfiletoPresistanceState(clientUsage.GetClientProfile());

                        _featureConfigManager.MergeFeaturesWithConfiguration(_mergedFeatureReport);

                        SaveFeatureReport();

                    }
                    catch (ThreadAbortException)
                    {
                        threadAbort = true;
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        threadAbort = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (NCacheServiceLogger.Logger != null)
                            NCacheServiceLogger.Logger.Error("Data Collector", ex.ToString());
                    }

                    Thread.Sleep(TimeSpan.FromMinutes(ServiceConfiguration.FeatureDataCollectionInterval));

                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (NCacheServiceLogger.Logger != null)
                    NCacheServiceLogger.Logger.Error("Data Collector", ex.ToString());
            }
        }

        #endregion



        #region Data Posting
        public void StartPostingData()
        {
            if (_dataPosting == null && ServiceConfiguration.UploadFeatureUsageStats)
            {
                _dataPosting = new Thread(new ThreadStart(PerformDataPosting));
                _dataPosting.IsBackground = true;
                _dataPosting.Name = "FeatureDataPosting";
                _dataPosting.Start();
            }
        }

        public void StopPostingData()
        {
            if (_dataPosting != null && _dataPosting.IsAlive)
            {
#if !NETCORE
                _dataPosting.Abort();
#else
                _dataPosting.Interrupt();
#endif
            }
        }

        //Thread is initiated to post the FeatureUsageConfiguration file.Thread sleeps for 3 days before posting file for the first time and then it sleeps for 30 days before posting it.
        private void PerformDataPosting()
        {
            try
            {
                if (_autoStartDelay > 0)
                    Thread.Sleep(_autoStartDelay * 1000);
                bool threadAbort = false;

                string lastPostingTime = _featureConfigManager.GetLastPostingTime();

                if (string.IsNullOrEmpty(lastPostingTime))
                {
                    while (!IsTodayInitialPostingDay(_installTime))
                    {
                        Thread.Sleep(TimeSpan.FromMinutes(ServiceConfiguration.FeatureDataPostingWaitTime));
                    }
                }


                while (!threadAbort)
                {

                    try
                    {
                        lastPostingTime = _featureConfigManager.GetLastPostingTime();

                        if (!string.IsNullOrEmpty(lastPostingTime))
                        {
                            if (!IsTodayPostingDay(Convert.ToDateTime(lastPostingTime)))
                            {
                                Thread.Sleep(TimeSpan.FromMinutes(ServiceConfiguration.FeatureDataPostingWaitTime));
                                continue;
                            }
                        }

                        bool isSuccessfull = _featureConfigManager.PostConfiguration(out string returnMessage);

                        if (!String.IsNullOrEmpty(returnMessage) && NCacheServiceLogger.Logger != null)
                        {
                            NCacheServiceLogger.Logger.CriticalInfo(returnMessage);
                        }

                        if (isSuccessfull)
                        {
                            _featureConfigManager.LoadConfiguration();
                            _featureConfigManager.UpdateFeatureUsageInfo(DateTime.Now.ToString());
                            _featureConfigManager.SaveConfiguration();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        threadAbort = true;
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        threadAbort = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (NCacheServiceLogger.Logger != null)
                            NCacheServiceLogger.Logger.Error("Data Posting", ex.ToString());
                    }

                }
            }

            catch (ThreadAbortException)
            {
                return;
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (NCacheServiceLogger.Logger != null)
                    NCacheServiceLogger.Logger.Error("Data Posting", ex.ToString());
            }
        }
        #endregion

        private void GetAndAccumulateCacheFeatureReport(String cacheName)

        {
            try
            {
                var featureReport = _cacheServer.GetFeatureUsageReport(cacheName);
                if (featureReport != null)
                {
                    UpdateAccumulatedFeatureData(cacheName, featureReport);

                    foreach (KeyValuePair<string, Dictionary<string, Common.FeatureUsageData.Feature>> cacheFeatureReport in _accumulatedDataFromCaches)
                    {
                        MergeAccumulatedFeatureReport(cacheFeatureReport.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                if (NCacheServiceLogger.Logger != null)
                    NCacheServiceLogger.Logger.Error("GetAndAccumulateCacheFeatureReport", ex.ToString());

            }
        }

        private void MergeAccumulatedFeatureReport(IDictionary<string, Common.FeatureUsageData.Feature> featureTable)
        {
            try
            {
                if (_mergedFeatureReport == null)
                {
                    _mergedFeatureReport = new Dictionary<string, Common.FeatureUsageData.Feature>(featureTable);
                }
                else
                {
                    foreach (var feature in featureTable)
                    {
                        if (_mergedFeatureReport.ContainsKey(feature.Key))
                        {
                            Common.FeatureUsageData.Feature oldFeature = _mergedFeatureReport[feature.Key];
                            Common.FeatureUsageData.Feature newFeature = (Common.FeatureUsageData.Feature)feature.Value.Clone();
                            _mergedFeatureReport[feature.Key] = MergeFeature(oldFeature, newFeature);
                        }
                        else
                        {
                            _mergedFeatureReport.Add(feature.Key, feature.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (NCacheServiceLogger.Logger != null)
                    NCacheServiceLogger.Logger.Error("MergeAccumulatedFeatureReport", ex.ToString());

            }
        }

        private Common.FeatureUsageData.Feature MergeFeature(Common.FeatureUsageData.Feature oldFeature, Common.FeatureUsageData.Feature newFeature)
        {
            try
            {
                Common.FeatureUsageData.Feature mergedFeature = new Common.FeatureUsageData.Feature { Name = oldFeature.Name, LastUsageTime = oldFeature.LastUsageTime, CreationTime = oldFeature.CreationTime };

                if (newFeature.LastUsageTime > oldFeature.LastUsageTime)
                    mergedFeature.UpdateUsageTime(newFeature.LastUsageTime);

                var oldSubFeatures = oldFeature.Features();
                var newSubFeatures = newFeature.Features();

                if (oldSubFeatures != null && oldSubFeatures.Count > 0 && newSubFeatures != null && newSubFeatures.Count > 0)
                {
                    mergedFeature.SubFeatures = newSubFeatures;
                    IDictionary<string, Common.FeatureUsageData.Feature> oldCopiedSubFeatures = new Dictionary<string, Common.FeatureUsageData.Feature>(oldSubFeatures);
                    foreach (var feature in oldCopiedSubFeatures)
                    {
                        if (mergedFeature.SubFeatures.ContainsKey(feature.Key))
                        {
                            mergedFeature.SubFeatures[feature.Key] = MergeFeature(feature.Value, mergedFeature.SubFeatures[feature.Key]);
                        }
                        else
                            mergedFeature.SubFeatures.Add(feature.Key, feature.Value);

                    }
                }

                else if (oldSubFeatures != null && oldSubFeatures.Count > 0)
                {
                    mergedFeature.SubFeatures = oldSubFeatures;
                }
                else if (newSubFeatures != null && newSubFeatures.Count > 0)
                {
                    mergedFeature.SubFeatures = newSubFeatures;
                }

                return mergedFeature;
            }
            catch (Exception ex)
            {
                if (NCacheServiceLogger.Logger != null)
                    NCacheServiceLogger.Logger.Error("MergeAccumulatedFeatureReport", ex.ToString());
                return null;

            }
        }

        private void UpdateAccumulatedFeatureData(string cacheName, Dictionary<string, Common.FeatureUsageData.Feature> updatedFeatures)
        {
            Dictionary<string, Common.FeatureUsageData.Feature> oldFeatureData;
            if (_accumulatedDataFromCaches.TryGetValue(cacheName, out oldFeatureData))
            {
                foreach (var featureEntry in updatedFeatures)
                {
                    if (oldFeatureData.ContainsKey(featureEntry.Key))
                    {
                        var oldfeature = oldFeatureData[featureEntry.Key];
                        var updatedfeature = featureEntry.Value;
                        oldFeatureData[featureEntry.Key] = MergeFeature(oldfeature, updatedfeature);
                    }
                    else
                    {
                        oldFeatureData.Add(featureEntry.Key, featureEntry.Value);
                    }
                }
            }
            else
            {
                _accumulatedDataFromCaches.Add(cacheName, updatedFeatures);
            }
        }

        private void GetAndAccumulateClientProfileReport(string cacheName, ClientUsage clientUsage)
        {
            {
                try
                {
                    var clientProfile = _cacheServer.GetClientProfileReport(cacheName);
                    if (clientProfile != null && !hasDefaultValues(clientProfile))
                        clientUsage.UpdateClientUsageProfile(clientProfile);
                }
                catch (Exception ex)
                {

                    if (NCacheServiceLogger.Logger != null)
                        NCacheServiceLogger.Logger.Error("GetAndAccumulateCacheFeatureReport", ex.ToString());
                }
            }
        }

        private void MergeServiceFeatureReport()
        {
            Dictionary<string, Common.FeatureUsageData.Feature> featureReport = FeatureUsageCollector.Instance.GetFeatureReport();
            MergeAccumulatedFeatureReport(featureReport);
        }

        private void SaveFeatureReport()
        {
            int retyrCount = ServiceConfiguration.UsageFailureRetriesCount;
            while (retyrCount > 0)
            {
                try
                {
                    _featureConfigManager.SaveConfiguration();
                    break;
                }
                catch (Exception)
                {
                    retyrCount--;
                    if (retyrCount == 0)
                        throw;
                }
            }
        }

        private bool hasDefaultValues(ClientProfileDom clientProfile)
        {
            return clientProfile.MaximumCores == 0 && clientProfile.MinimumCores == 0 && clientProfile.MinimumMemory == 0 && clientProfile.MaximumMemory == 0;
        }

        private bool IsTodayPostingDay(DateTime dateTime)
        {
            return dateTime.AddMinutes(ServiceConfiguration.FeatureDataPostingDelay) < DateTime.Now;
        }

        private bool IsTodayInitialPostingDay(DateTime dateTime)
        {
            return dateTime.AddMinutes(ServiceConfiguration.InitailFeatureDataPostingDelay) < DateTime.Now;
        }
    }

}
