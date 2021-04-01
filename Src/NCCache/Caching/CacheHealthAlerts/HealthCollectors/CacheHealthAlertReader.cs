using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{ 

    public class CacheHealthAlertReader
    {
        private const string configName = "alerts.xml";
        private string installPath;
        private string filePath;


        public CacheHealthAlertReader()
        {
            installPath = Path.Combine(AppUtil.InstallDir,"config");
            if (!Directory.Exists(installPath))
                installPath = Environment.CurrentDirectory;
            filePath = Path.Combine(installPath, configName);
        }

        internal HealthAlerts LoadConfiguration()
        {
            try
            {
                ConfigurationBuilder builder = new ConfigurationBuilder(filePath);
                builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.Dom.HealthAlerts));
                builder.ReadConfiguration();

                HealthAlerts[] healthAlerts = new HealthAlerts[builder.Configuration.Length];
                builder.Configuration.CopyTo(healthAlerts, 0);
                return healthAlerts[0];
            }
            catch (Exception ex)
            {
                return new HealthAlerts();
            }
        }

        

    }
}
