// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.Analysis;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Contains application level information
    /// </summary>
    internal sealed class Application : IDisposable
    {
        /// <summary>
        /// Modes in which application could run
        /// </summary>
        public enum ApplicationMode
        {
            /// <summary>
            /// Command are passed thru
            /// </summary>
            Passthru,

            /// <summary>
            /// Command are analyzed
            /// </summary>
            Analysis,

            /// <summary>
            /// Command results are cached
            /// </summary>
            Cache
        }

        /// <summary>
        /// Current instance of CachePolicyCollection
        /// </summary>
        public static readonly Application Instance;

        /// <summary>
        /// Static constructor
        /// </summary>
        static Application()
        {
            Instance = new Application();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private Application()
        {
            this.LoadAppSettings();
            this.Mode = ApplicationMode.Passthru;
        }

        /// <summary>
        /// Read app settings from app.cofig/web.config
        /// </summary>
        private void LoadAppSettings()
        {
            try
            {
                ApplicationId = ConfigurationManager.AppSettings["app-id"];
            }
            catch (ConfigurationErrorsException exc)
            {
                Logger.Instance.TraceError(exc.Message);
                ApplicationId = string.Empty;
            }

            try
            {
                ///Check error level set by user in app.config/web.config's appSettings
                string level = ConfigurationManager.AppSettings["logging-level"];
                if (!level.IsNullOrEmpty())
                {
                    switch (level.ToLower())
                    {
                        case "off":
                            LoggingLevel = Logger.LoggingLevel.Off;
                            break;

                        case "error":
                            LoggingLevel = Logger.LoggingLevel.Error;
                            break;

                        case "debug":
                            LoggingLevel = Logger.LoggingLevel.Debug;
                            break;
                    }
                }
                else
                {
                    LoggingLevel = Logger.LoggingLevel.Off;
                }
            }
            catch (ConfigurationErrorsException exc)
            {
                Logger.Instance.TraceError(exc.Message);
                LoggingLevel = Logger.LoggingLevel.Off;
            }
        }

        /// <summary>
        /// Initialize application and cache settings
        /// </summary>
        public void Initialize()
        {
            this.LoadConfiguration();
            EFCachingConfiguration.Instance.ConfigurationUpdated += new EventHandler<ConfiguraitonUpdatedEventArgs>(Instance_ConfigurationUpdated);
        }

        /// <summary>
        /// Called when configuration is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_ConfigurationUpdated(object sender, ConfiguraitonUpdatedEventArgs e)
        {
            this.LoadConfiguration(e.Configuration);
        }

        /// <summary>
        /// Load application configuration
        /// </summary>
        private void LoadConfiguration()
        {
            ///Cannot load configuration if no app-id is found in web.config or app.config
            if (this.ApplicationId.IsNullOrEmpty())
            {
                Logger.Instance.TraceError("app-id not found in application config");
                return;
            }

            ///Load configuration
            EFCachingConfigurationElement config = EFCachingConfiguration.Instance.LoadConfiguration();
            ApplicationConfigurationElement appConfig = null;

            if (config != null)
            {
                appConfig = config.GetAppConfig(this.ApplicationId);
                this.LoadConfiguration(appConfig);

                if (appConfig != null)
                {
                    CachePolicy.LoadConfig(appConfig.CachePolicy);
                    AnalysisPolicy.LoadConfig(appConfig.AnalysisPolicy);
                    //QueryLevelCachePolicy.LoadConfig(appConfig.QueryLevelCachePolicy);
                    this.InitializeCache(appConfig);

                    ///Start analysis
                    if (this.Mode == ApplicationMode.Analysis)
                    {
                        AnalysisManager.Instance.Start(AnalysisPolicy.GetEffectivePolicy());
                    }
                }
                else
                {
                    Logger.Instance.TraceError("No 'app-config' found in configuration file for the application-id = " + this.ApplicationId);
                }
            }
        }

        /// <summary>
        /// Load application configuration
        /// </summary>
        /// <param name="config">Application configuration settings</param>
        public void LoadConfiguration(ApplicationConfigurationElement config)
        {
            this.Mode = ApplicationMode.Passthru;
            if (config != null)
            {
                switch (config.Mode)
                {
                    case ApplicationConfigurationElement.AppMode.Analysis:
                        this.Mode = ApplicationMode.Analysis;
                        break;

                    case ApplicationConfigurationElement.AppMode.Caching:
                        this.Mode = ApplicationMode.Cache;
                        break;
                }
            }
        }

        /// <summary>
        /// Initialize cache
        /// </summary>
        /// <param name="config">Application configuration settings</param>
        private void InitializeCache(ApplicationConfigurationElement config)
        {
            ///Cache is only initialized once
            if (this.Cache == null)
            {
                CachePolicyElement policy = config.CachePolicy;
                if (policy != null)
                {
                    try
                    {
                        if (policy.CacheName.IsNullOrEmpty())
                        {
                            Logger.Instance.TraceError("Cache Id cannot be null or empty string");
                        }
                        else
                        {
                            Cache = new NCache.Integrations.EntityFramework.Caching.NCache(policy.CacheName);
                        }
                    }
                    catch (Exception exc)
                    {
                        Logger.Instance.TraceError(exc.ToString());
                    }
                }
                else
                {
                    Logger.Instance.TraceError("No 'cache-policy' found in confgiruation file");
                }
            }
        }

        /// <summary>
        /// Get application id as specified in app.config/web.config
        /// </summary>
        public string ApplicationId { get; private set; }

        /// <summary>
        /// Get logging level as specified in app.config/web.config
        /// </summary>
        public Logger.LoggingLevel LoggingLevel { get; private set; }

        /// <summary>
        /// Get current application mode
        /// </summary>
        public ApplicationMode Mode { get; private set; }

        /// <summary>
        /// Get cache instance
        /// </summary>
        public ICache Cache { get; private set; }

        /// <summary>
        /// Get cache policy
        /// </summary>
        public CachePolicyCollection CachePolicy { get { return CachePolicyCollection.Instance; } }

        /// <summary>
        /// Get analysis policy
        /// </summary>
        public AnalysisPolicy AnalysisPolicy { get { return AnalysisPolicy.Instance; } }

        //public QueryLevelCachePolicy QueryLevelCachePolicy { get { return QueryLevelCachePolicy.Instance; } }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.Cache != null)
            {
                this.Cache.Dispose();
            }
            AnalysisManager.Instance.Dispose();
        }

        #endregion


        ~Application()
        {
            AnalysisManager.Instance.Dispose();
        }
    }
}
