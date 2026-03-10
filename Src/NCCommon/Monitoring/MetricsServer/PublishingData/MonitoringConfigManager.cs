using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;


namespace Alachisoft.NCache.Common.Monitoring
{
   public class MonitoringConfigManager
    {
        static string c_configDir = DIRNAME;
        static string c_configFileName = FILENAME;
        static string DIRNAME = "config";
        static string FILENAME = "monitoring.ncconf";
        internal static string ENDSTRING = "\r\n";
        static object _lock = new object();
        static  Counters _configuration;
        public static bool isConfigFileFound { get; set; }

        public MonitoringConfigManager()
        {
            LoadConfiguration();
        }
        private  void CombinePath()
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

        public  Category GetCategory(string categoryName)
        {
            if (_configuration != null)
            {
                foreach (var category in _configuration.Category)
                {
                    if (category.Name == categoryName)
                        return category;
                }
            }
            return null;
        }
        public  void LoadConfiguration()
        {
            try
            {
                CombinePath();

                //If the file does not exist, create the file.
                if (!File.Exists(c_configFileName))
                {
                    isConfigFileFound = false;
                   
                    if (_configuration == null)
                    {
                        
                        _configuration = new Counters();
                    }
                }
                else
                {
                    isConfigFileFound = true;
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

        private  string ToXml()
        {
            StringBuilder sb = new StringBuilder();

            object[] configuration = new object[1];
            configuration[0] = _configuration;
            ConfigurationBuilder cfgBuilder = new ConfigurationBuilder(configuration);
            cfgBuilder.RegisterRootConfigurationObject(typeof(Counters));
            sb.Append(cfgBuilder.GetXmlString());


            return sb.ToString();
        }
        public  void SaveConfiguration()
        {
            if (c_configFileName == null || c_configFileName == "")
                CombinePath();

            FileStream fs = null;
            StreamWriter sw = null;
            lock (_lock)
            {
                try
                {
                    fs = new FileStream(c_configFileName, FileMode.Create);
                    sw = new StreamWriter(fs);
                    sw.Write(ToXml());
                    sw.Flush();
                }
                catch (Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }
                finally
                {
                    if (sw != null) sw.Close();
                    if (fs != null) fs.Close();
                }
            }
        }

        private  void LoadXml()
        {
            if (String.IsNullOrEmpty(c_configFileName))
                CombinePath();

            ConfigurationBuilder configBuilder = new ConfigurationBuilder(c_configFileName);
            configBuilder.RegisterRootConfigurationObject(typeof(Counters));
            configBuilder.ReadConfiguration();

            Counters counterConfiguration = null;
            Object[] configuration = configBuilder.Configuration;

            if (configuration != null && configuration.Length > 0)
            {
                for (int i = 0; i < configuration.Length; i++)
                {
                    counterConfiguration = configuration[i] as Counters;
                    break;
                }
            }

            _configuration = counterConfiguration;

            if (_configuration == null)
                _configuration = new Counters();

        }
    }
}
