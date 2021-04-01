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
using System.Threading;
using System.Web.UI;
using Alachisoft.ContentOptimization.Caching;
using Alachisoft.ContentOptimization.Diagnostics.Logging;
using Alachisoft.ContentOptimization;
using Alachisoft.NCache.ContentOptimization.Caching;
using Alachisoft.NCache.ContentOptimization.Configurations;
using Alachisoft.NCache.ContentOptimization.Diagnostics;

namespace Alachisoft.NCache.Adapters
{
    public class PageAdapter : System.Web.UI.Adapters.PageAdapter
    {
        static ReaderWriterLock settingsLock = new ReaderWriterLock();
        static ICache cache;
        static ContentSettings _settings;
        
        static PageAdapter()
        {
            settingsLock = new ReaderWriterLock();
            var configProvider = ConfigurationProvider.Settings;
            LoadSettings();
        }

        public override PageStatePersister GetStatePersister()
        {
            settingsLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                var persister = GetStatePersisterUnsafe();
                return persister;
            }
            finally
            {
                settingsLock.ReleaseReaderLock();
            }                      
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
			string em = Page.Request.QueryString["rpo"] ?? "on";
        }

        private PageStatePersister GetStatePersisterUnsafe()
        {
            PageStatePersister persister;
            try
            {   
                if (_settings.EnableViewstateCaching)
                {
                    persister = GetPersister();
                }
                else
                    persister = base.GetStatePersister();
            }
            catch (Exception ex)
            {
                FileBasedTraceProvider.Current.WriteTrace(TraceSeverity.Exception, "Exception occurred in GetStatePersisterUnsafe :" + ex.Message);
                return null;
            }

            return persister;
        }

        private PageStatePersister GetPersister()
        {
            ContentSettings defaultSettings = null;
            PageStatePersister persister = null;
            ViewStatePersister ncachePersister;
            Alachisoft.ContentOptimization.Caching.Expiration expiration;

            string relativeUrl = Page.AppRelativeVirtualPath.Replace("~/", "");

            defaultSettings  = GetDefaultSettings();

            if (defaultSettings != null)
            {
                expiration = defaultSettings.CacheSettings.Expiration.Convert(Alachisoft.ContentOptimization.Caching.ExpirationType.Sliding);

                ncachePersister = new ViewStatePersister(Page, cache, FileBasedTraceProvider.Current);

                persister = ncachePersister;
            }
            return persister;
        }

        private ContentSettings GetDefaultSettings()
        {
            return _settings;
        }

        static void configProvider_ConfigurationModified(object sender, EventArgs e)
        {
            LoadSettings();
        }

        static void LoadSettings()
        {
            settingsLock.AcquireWriterLock(Timeout.Infinite);

            try
            {
                if (cache != null)
                    cache.Dispose();
                
                _settings = ConfigurationProvider.Settings;

                if (_settings.Enabled)
                {
                    if (_settings.CacheSettings == null) 
                        throw new Exception("Cache settings not set");

                    var cacheAdapter = new CacheAdapter(_settings.CacheSettings.CacheName);
                   
                    cacheAdapter.RetryInterval = _settings.CacheSettings.ConnectionRetryInterval.HasValue ? _settings.CacheSettings.ConnectionRetryInterval.Value : 300;
                    cacheAdapter.Load();

                    if (_settings.CacheSettings.Expiration == null)
                        throw new Exception("Expiration settings not set");

                    cacheAdapter.DefaultExpiration = _settings.CacheSettings.Expiration.Convert();
                    
                    cache = cacheAdapter.GetSynchronized(settingsLock);
                }
            }
            finally
            {
                settingsLock.ReleaseWriterLock();
            }                        
        }     
    }
}
