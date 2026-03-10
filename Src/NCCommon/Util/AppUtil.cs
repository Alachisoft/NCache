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
using System;
using System.Diagnostics;
using Alachisoft.NCache.Common.Interop;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Alachisoft.NCache.Common.RuntimeEnvironment;
using Alachisoft.NCache.Licensing.RegistryUtil;
using Alachisoft.NCache.Common.Licensing;

using System.Web;
using System.Linq;



#if NETCORE
using System.Runtime.InteropServices;
#endif
using Alachisoft.NCache.Common.Util;


namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Utility class to help with common tasks.
    /// </summary>
    public class AppUtil
    {
        public const int MAX_BUCKETS = 1000;
        static bool isRunningAsWow64 = false;
        static string installDir = null;

        public readonly static string DeployedCachingModuleAssemblyDir = "bin" + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar;
        public readonly static string DeployedAssemblyDir = "deploy" + Path.DirectorySeparatorChar;
        public readonly static string serviceLogsPath = "log-files" + Path.DirectorySeparatorChar + "service.log";

        static int s_logLevel = 7;
        static string javaLibDir = null;
        static int _bucketSize;
        static string logDir = null;
        private static int counter = 0;
        static Random random = new Random();
        private static NCacheLogger _nCacheEventLogger = null;

        static AppUtil()
        {
            try
            {
                _bucketSize = (int)Math.Ceiling(((long)int.MinValue * -1) / (double)MAX_BUCKETS);
#if NETCORE 
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                isRunningAsWow64 = Win32.InternalCheckIsWow64();

            }
            catch (Exception ex)
            {
                LogEvent("Win32.InternalCheckIsWow64() Error " + ex.Message, EventLogEntryType.Error);
            }

            installDir = GetInstallDir();
            logDir = GetLogDir();

            javaLibDir = GetJavaLibDir();
            DeployedAssemblyDir = Path.Combine(installDir, DeployedAssemblyDir);
            DeployedCachingModuleAssemblyDir = Path.Combine(installDir, DeployedCachingModuleAssemblyDir);

            if (ServiceConfiguration.EventLogLevel != null && ServiceConfiguration.EventLogLevel != "")
            {
                string logLevel = ServiceConfiguration.EventLogLevel.ToLower();
                switch (logLevel)
                {
                    case "error":
                        s_logLevel = 1;
                        break;

                    case "warning":
                        s_logLevel = 3;
                        break;

                    case "all":
                        s_logLevel = 7;
                        break;
                }
            }
        }
        
        public static bool IsRunningAsWow64
        {
            get { return isRunningAsWow64; }
        }

        public static bool IsNew { get { return true; } }

        private static string GetInstallDir()
        {
            if (!string.IsNullOrEmpty(NCacheConfigurationOptions.InstallDir))
            {
                return NCacheConfigurationOptions.InstallDir;
            }
            string installPath = System.Configuration.ConfigurationSettings.AppSettings["InstallDir"];
            if (!string.IsNullOrEmpty(installPath))
            {
                return installPath;
            }

            string path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

            try
            {
#if NETCORE
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    NCache.Licensing.RegistryUtil.RegUtil.LoadRegistry();
                    if (NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties != null ||
                        NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product != null || 
                        !string.IsNullOrEmpty(NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir))
                        path = NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir;
                }
                else
#endif
                path = GetAppSetting("InstallDir");

            }
            catch (Exception e)
            {
                if (InstallationTypeProvider.Provider.IsServerInstallation)
                    throw;
            }
            if (path == null || path.Length == 0)
            {
                if (InstallationTypeProvider.Provider.IsServerInstallation)
                    return null;
                else
                    path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

            }

            return path;
        }

        private static string GetLogDir()
        {
            if (!string.IsNullOrEmpty(NCacheConfigurationOptions.LogPath))
            {
                return NCacheConfigurationOptions.LogPath;
            }
            string installPath = System.Configuration.ConfigurationSettings.AppSettings["NCache.LogPath"];
            if (!string.IsNullOrEmpty(installPath))
            {
                return installPath;
            }
            string path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

            try
            {
#if NETCORE
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    NCache.Licensing.RegistryUtil.RegUtil.LoadRegistry();
                    if (NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties != null ||
                        NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product != null ||
                        !string.IsNullOrEmpty(NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir))
                        path = NCache.Licensing.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir;
                }
                else
                    path = GetAppSetting("InstallDir");
#elif !NETCORE
                path = GetAppSetting("InstallDir");
#endif 
            }
            catch (Exception)
            {
                if (InstallationTypeProvider.Provider.IsServerInstallation)
                {
                    throw;
                }
            }
            if (path == null || path.Length == 0)
            {

                if (InstallationTypeProvider.Provider.IsServerInstallation)
                {
                    return null;
                }
                else
                {
                    path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

                }
            }
            return path;
        }

        /// <summary>
        /// Reads the value/data pair from the NCache registry key.
        /// Automatically caters for wow64/win32.
        /// </summary>
        /// <param name="key">Name of the value to be read.</param>
        /// <returns>Data of the value.</returns>
        public static string GetAppSetting(string key)
        {
            return GetAppSetting("", key);
        }



        /// <summary>
        /// Reads the value/data pair from the NCache registry key.
        /// Automatically caters for wow64/win32.
        /// </summary>
        /// <param name="section">Section from which key is to be read.</param>
        /// <param name="key">Name of the value to be read.</param>
        /// <returns>Data of the value.</returns>
        public static string GetAppSetting(string section, string key)
        {
            section = RegHelper.ROOT_KEY + section;
            object tempVal = RegHelper.GetRegValue(section, key, 0);
            if (!(tempVal is string))
            {
                return Convert.ToString(tempVal);
            }
            return (string)tempVal;
        }

        /// <summary>
        /// Get decrypted value from section.
        /// Automatically caters for wow64/win32.
        /// </summary>
        /// <param name="section">">Section from which key is to be read.</param>
        /// <param name="key">key</param>
        /// <returns>value retrieved</returns>
        public static string GetDecryptedAppSetting(string section, string key)
        {
            section = RegHelper.ROOT_KEY + section;
            return (string)RegHelper.GetDecryptedRegValue(section, key, 0);
        }

        /// <summary>
        /// Write the value to the NCache registry key.
        /// Automatically caters for wow64/win32.
        /// </summary>
        /// <param name="section">">Section from which key is to be read.</param>
        /// <param name="key">Name of the value to be write.</param>
        /// <param name="value">New value of key</param>
        public static void SetAppSetting(string section, string key, string value, short prodId)
        {
            section = RegHelper.ROOT_KEY + section;
            RegHelper.SetRegValue(section, key, value, prodId);
        }

        /// <summary>
        /// Write the value to the NCache registry key after encrypting it.
        /// Automatically caters for wow64/win32.
        /// </summary>
        /// <param name="section">">Section from which key is to be read.</param>
        /// <param name="key">Name of the value to be write.</param>
        /// <param name="value">New value of key</param>
        public static void SetEncryptedAppSetting(string section, string key, string value)
        {
            section = RegHelper.ROOT_KEY + section;
            RegHelper.SetEncryptedRegValue(section, key, value);
        }

        /// <summary>
        /// Check if the section has preceeding \. If not then append one
        /// </summary>
        /// <param name="section">Section</param>
        /// <returns>Checked and completed section</returns>
        private static string CompleteSection(string section)
        {
            return section.StartsWith("\\") ? section : "\\" + section;
        }

        /// <summary>
        /// Gets the install directory of NCache.
        /// Returns null if registry key does not exist.
        /// </summary>
        public static string InstallDir
        {
            get { return installDir; }
        }
        public static string DumpsDir
        {
            get
            {
                return Path.Combine(installDir, "memory-dumps");
            }
        }
        public static string ModulesDir
        {
            get
            {
                var bin = Path.Combine(installDir, "bin");
                var modules = Path.Combine(bin, "modules");
                return modules;
            }
        }

        public static string MapDistributionDir
        {
            get
            {
                return RuntimeEnvironment.NCacheRuntimeEnvironment.GetEnvironment.LuceneIndexPath;
            }
        }

        public static string LogDir
        {
            get { return logDir; }
        }

        public static bool IsNuGetOnlyInstallation
        {
            get
            {
                string filePath = string.Empty;
                string fileDirectory = Path.Combine(InstallDir, "bin", "service");
#if !NETCORE
                filePath = Path.Combine(fileDirectory, "Alachisoft.NCache.CacheHost.exe");
#else
                filePath = Path.Combine(fileDirectory, "Alachisoft.NCache.CacheHost.dll");
#endif

                if (File.Exists(filePath))
                    return false;
                else
                    return true;
            }
        }

        private static string GetJavaLibDir()
        {
            return AppUtil.InstallDir + "Java\\Lib\\";
        }
        public static string JavaLibDir
        {
            get { return javaLibDir; }
        }
        /// <summary>
        /// Writes an error, warning, information, success audit, or failure audit 
        /// entry with the given message text to the event log.
        /// </summary>
        /// <param name="msg">The string to write to the event log.</param>
        /// <param name="type">One of the <c>EventLogEntryType</c> values.</param>
        public static void LogEvent(string source, string msg, EventLogEntryType type, short category, int eventId)
        {
            try
            {
                OSInfo currentOS = OSInfo.Windows;
#if NETCORE
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    currentOS = OSInfo.Linux;
#endif
                if (currentOS == OSInfo.Windows)
                {
                    try
                    {
                        int level = (int)type;
                        if ((level & s_logLevel) == level)
                        {
                            using (EventLog ncLog = new EventLog("Application"))
                            {
                                ncLog.Source = source;
                                ncLog.WriteEntry(msg, type, eventId);
                            }
                        }
                    }
                    catch (Exception) { }
                }
                else // For Linux
                {
                    if (_nCacheEventLogger == null)
                    {
                        _nCacheEventLogger = new NCacheLogger();
                        _nCacheEventLogger.InitializeEventsLogging();
                    }
                    _nCacheEventLogger.EventLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss, fff"), source, eventId.ToString(), type.ToString(), msg);
                }
            }
            catch { }
        }



        /// <summary>
        /// Writes an error, warning, information, success audit, or failure audit 
        /// entry with the given message text to the event log.
        /// </summary>
        /// <param name="msg">The string to write to the event log.</param>
        /// <param name="type">One of the <c>EventLogEntryType</c> values.</param>
        public static void LogEvent(string msg, EventLogEntryType type, int eventId)
        {
            string cacheserver = "NCache";
            LogEvent(cacheserver, msg, type, (short)type, eventId);
        }

        /// <summary>
        /// Writes an error, warning, information, success audit, or failure audit 
        /// entry with the given message text to the event log.
        /// </summary>
        /// <param name="msg">The string to write to the event log.</param>
        /// <param name="type">One of the <c>EventLogEntryType</c> values.</param>
        public static void LogEvent(string msg, EventLogEntryType type)
        {
            string cacheserver = "NCache";

            if (type == EventLogEntryType.Information)
                LogEvent(cacheserver, msg, type, EventCategories.Information, EventID.GeneralInformation);
            else
                LogEvent(cacheserver, msg, type, EventCategories.Warning, EventID.GeneralError);
        }

        /// <summary>
        /// Returns lg(Log2) of a number.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static byte Lg(int val)
        {
            byte i = 0;
            while (val > 1)
            {
                val >>= 1;
                i++;
            }
            return i;
        }

        /// <summary>
        /// Store all date time values as a difference to this time
        /// </summary>
        private static DateTime START_DT = new DateTime(2004, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc);
        private static Process s_currentProcess;

        /// <summary>
        /// Convert DateTime to integer taking 31-12-2004 as base
        /// and removing millisecond information
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int DiffSeconds(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            TimeSpan interval = dt - START_DT;
            return (int)interval.TotalSeconds;
        }

        public static int DiffMilliseconds(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            TimeSpan interval = dt - START_DT;
            return (int)interval.Milliseconds;
        }

        public static long DiffTicks(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            TimeSpan interval = dt - START_DT;
            return interval.Ticks;
        }

        /// <summary>
        /// Convert DateTime to integer taking 31-12-2004 as base
        /// and removing millisecond information
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime GetDateTime(int absoluteTime)
        {
            DateTime dt = new DateTime(START_DT.Ticks, DateTimeKind.Utc);
            return dt.AddSeconds(absoluteTime);
        }

        public static int DiffMinutes(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            TimeSpan interval = dt - START_DT;
            return (int)interval.TotalMinutes;
        }

        /// <summary>
        /// Convert DateTime to integer taking 31-12-2004 as base
        /// and removing millisecond information
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime AddMinutes(int minutes)
        {
            DateTime dt = new DateTime(START_DT.Ticks, DateTimeKind.Utc);
            return dt.AddMinutes(minutes);
        }

        /// <summary>
        /// Checks environment to verify if there is 'Any' version of Visual Studio installed.
        /// and removing millisecond information
        /// </summary>
        public static bool IsVSIdeInstalled()
        {
            return false;
        }

        /// <summary>
        /// Hashcode algorithm returning same hash code for both 32bit and 64 bit apps. 
        /// Used for data distribution under por/partitioned topologies.
        /// </summary>
        /// <param name="strArg"></param>
        /// <returns></returns>
        public static unsafe int GetHashCode(string strArg)
        {
            strArg = GetLocationAffinityKey(strArg);

            fixed (void* str = strArg)
            {
                char* chPtr = (char*)str;
                int num = 0x15051505;
                int num2 = num;
                int* numPtr = (int*)chPtr;
                for (int i = strArg.Length; i > 0; i -= 4)
                {
                    num = (((num << 5) + num) + (num >> 0x1b)) ^ numPtr[0];
                    if (i <= 2)
                    {
                        break;
                    }
                    num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ numPtr[1];
                    numPtr += 2;
                }
                return (num + (num2 * 0x5d588b65));
            }
        }

        public static int GetBucketId(string key)
        {
            int hashCode = GetHashCode(key);
            int bucketId = hashCode / _bucketSize;

            if (bucketId < 0)
                bucketId *= -1;
            return bucketId;
        }


        /// <summary>
        /// This method returns the cache key with whom the cache item is to be associated with; 
        /// provided that this is specified in the provided cache key.
        /// </summary>
        /// <remarks>
        /// The rules for parsing location affinity key are the same as for Keys Hash tags in Redis. 
        /// They are as follows,
        /// 
        /// - IF the key contains a '{' character.
        /// - AND IF there is a '}' character to the right of '{'
        /// - AND IF there are one or more characters between the first occurrence of '{' and the first 
        ///   occurrence of '}'.
        /// 
        /// Then instead of the key, only what is between the first occurrence of '{' and the following 
        /// first occurrence of '}' is returned.
        /// 
        /// Examples,
        /// 
        /// - The two keys '{Affinity}.Key001' and '{Affinity}.Key002' will go to the same bucket since the 
        ///   hash code will be generated from 'Affinity' only.
        /// - For the key 'Key{}{Affinity}' the whole key will be hashed as usually since the first 
        ///   occurrence of '{' is followed by '}' on the right without characters in the middle.
        /// - For the key 'Key{{Affinity}}Part' the substring '{Affinity' will be hashed, because it is the 
        ///   substring between the first occurrence of '{' and the first occurrence of '}' on its right.
        /// - For the key 'Key{Affinity}{Part}' the substring 'Affinity' will be hashed, since the algorithm 
        ///   works with the first valid or invalid (without characters inside) match of '{' and '}'.
        /// - What follows from the algorithm is that if the key starts with '{}', it is guaranteed to be 
        ///   hashed as a whole.
        /// </remarks>
        /// <param name="key">The key containing cache key for associated cache item following the 
        /// '{Affinity}CacheKey', 'CacheKey{Affinity}' or 'Cache{Affinity}Key' pattern.</param>
        /// <returns>The associated cache item's cache extracted from the key passed to the method.</returns>
        private static string GetLocationAffinityKey(string key)
        {
            int indexStart = key.IndexOf('{');
            int indexEnd = key.IndexOf('}');

            if (indexStart != -1 && indexEnd != -1 && (indexStart + 1) < indexEnd)
            {
                return key.Substring(indexStart + 1, indexEnd - indexStart - 1);
            }
            return key;
        }

        public static string GenerateMessageID(string key,string uniqueId)
        {
            string messageID;
            if (key.Contains("{") && key.Contains("}"))
            {
                messageID = key + uniqueId;
            }
            else
            {
                messageID = "{" + key.ToString() + "}" + uniqueId;
            }
          
            return messageID;
        }

        public static DateTime GetRandomNonMaintenanceTime()
        {
            DateTime start = DateTime.Now.AddSeconds(5);// must not start within 5 seconds to avoid any issues
            DateTime end = DateTime.Now.AddHours(24); //new DateTime(start.Year, start.Month, start.Day, 23, 59, 59);

            DateTime randomTime = GetRandomDate(start, end);
            while (IsInDownTimeRange(randomTime))
            {
                randomTime = GetRandomDate(DateTime.Now, end);
            }

            //convert to local time.
            TimeZone zone = TimeZone.CurrentTimeZone;
            randomTime = zone.ToLocalTime(randomTime);

            //make sure that the random time is always a future time.
            if (randomTime <= DateTime.Now)
                randomTime = DateTime.Now.AddSeconds(10);

            return randomTime;
        }

        public static double GetRandomMonthlyTime(DateTime start, DateTime end)
        {
            try
            {
                double millisec = 0;
                DateTime randomTime = GetRandomDate(start, end);
                //convert to local time.
                TimeZone zone = TimeZone.CurrentTimeZone;
                randomTime = zone.ToLocalTime(randomTime);
                millisec = (double)(randomTime - DateTime.Now).TotalMilliseconds;
                if (millisec < 0 || millisec >= Int32.MaxValue)
                    millisec = 1000 * 60 * 30;
                return Convert.ToDouble(millisec);
            }
            catch (Exception)
            {
                return 1000 * 60 * 30;
            }
        }


        static bool IsInDownTimeRange(DateTime time)
        {
            TimeSpan start = new TimeSpan(5, 0, 0); //10 AM time (GMT 5+)
            TimeSpan end = new TimeSpan(9, 0, 0); //2 PM time (GMT 5+)
            TimeSpan now = time.TimeOfDay;

            return now > start && now < end;
        }

        static DateTime GetRandomDate(DateTime startDate, DateTime endDate)
        {
            TimeZone zone = TimeZone.CurrentTimeZone;
            DateTime gmtEndDate = zone.ToUniversalTime(endDate);
            DateTime gmtStartDate = zone.ToUniversalTime(startDate);
            TimeSpan timeSpan = gmtEndDate - gmtStartDate;
            TimeSpan newSpan = new TimeSpan(0, random.Next(0, (int)timeSpan.TotalMinutes), 0);
            DateTime newDate = gmtStartDate + newSpan;

            return newDate;
        }

        public static Process CurrentProcess
        {
            get
            {
                if (s_currentProcess == null)
                    s_currentProcess = Process.GetCurrentProcess();

                return s_currentProcess;
            }
        }
        
        public static bool IsFullPath(string path)
        {
            // Checks for null and invalid strings
            // IsPathRooted will check for most cases, however, we need to exclude UNC paths
            // Also Windows paths must start with a drive letter
            if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !Path.IsPathRooted(path))
                return false;

            string pathRoot = Path.GetPathRoot(path);

            // Linux root paths will start with a forward slash
            if (pathRoot.Length <= 2 && pathRoot != "/")
                return false;

            // Paths should not start with double backslashes, as these are used for shared paths
            return (pathRoot[0] != '\\' || pathRoot[1] != '\\') ? true : false;
        }

        public static string GetHost()
        {
            string hostURl = @"https://app.alachisoft.com";
            try
            {
                string path = Path.Combine(NCacheRuntimeEnvironment.GetEnvironment.NActivatePath, "activate.json");
                if (File.Exists(path))
                {
                    using (StreamReader file = File.OpenText(path))
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        var host = (JObject)JToken.ReadFrom(reader);
                        var onPremHostURl = host["Host"].Value<string>();
                        if (!string.IsNullOrEmpty(onPremHostURl))
                            hostURl = onPremHostURl;
                    }
                }
            }
            catch (Exception) { }
            return hostURl;
        }

        public static string GetCloudHost()
        {
            string hostURl = @"https://cloud.alachisoft.com";

            try
            {
                if (!string.IsNullOrEmpty(RegUtil.LicenseProperties.UserInfo.CloudUrl))
                {
                    hostURl = RegUtil.LicenseProperties.UserInfo.CloudUrl;
                }
            }
            catch (Exception) { }
            return hostURl;
        }
        public static string BuildHeartbeatServerUrl(string subscriptionId, string licenseDuration, string azureFunctionUrl)
        {
            var uriBuilder = new UriBuilder(azureFunctionUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["subscriptionId"] = subscriptionId;
            query["licenseDuration"] = licenseDuration;
            query["macAddress"] = string.Join(",", MachineInfo.MacAddresses.Where(m => !string.IsNullOrWhiteSpace(m)));
            uriBuilder.Query = query.ToString();
            return uriBuilder.ToString();
        }
    }
}
