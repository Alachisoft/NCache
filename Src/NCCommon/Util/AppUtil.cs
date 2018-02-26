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
// limitations under the License

using System;
using System.Diagnostics;
using Alachisoft.NCache.Common.Interop;
using Microsoft.Win32;
using System.IO;
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
        static bool isRunningAsWow64 = false;
        static string installDir = null;

        public readonly static string DeployedAssemblyDir = "deploy\\";
        public readonly static string serviceLogsPath = "log-files" + Path.DirectorySeparatorChar + "service.log";

        static int s_logLevel = 7;
        static string javaLibDir = null;
        static int _bucketSize;
        static string logDir = null;

        static AppUtil()
        {
            try
            {
                _bucketSize = (int)Math.Ceiling(((long)int.MinValue * -1) / (double)1000);
#if !NETCORE
                isRunningAsWow64 = Win32.InternalCheckIsWow64();
#endif
            }
            catch (Exception ex)
            {
                LogEvent("Win32.InternalCheckIsWow64() Error " + ex.Message, EventLogEntryType.Error);
            }
            installDir = GetInstallDir();
            logDir = GetLogDir();

            javaLibDir = GetJavaLibDir();
            DeployedAssemblyDir = Path.Combine(installDir, DeployedAssemblyDir);


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
            string installPath = System.Configuration.ConfigurationSettings.AppSettings["InstallDir"];
            if (installPath != null && installPath != string.Empty)
            {
                return installPath;
            }
            string path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

            try
            {
#if NETCORE
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Registry.NetCore.RegistryUtil.RegUtil.LoadRegistry();
                    if (Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties != null ||
                        Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product != null ||
                        !string.IsNullOrEmpty(Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir))
                        path = Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir;
                }
                else
                    path = GetAppSetting("InstallDir");
#elif !NETCORE
                path = GetAppSetting("InstallDir");
#endif
            }
            catch (Exception)
            {
#if CLIENT
    //ignore this exception as in case of Nuget client package, nclicense.dll is not shipped with
#else
                throw;
#endif
            }
            if (path == null || path.Length == 0)
#if CLIENT
                path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;
#else
                return null;
#endif

            return path;
        }

        private static string GetLogDir()
        {
            string installPath = System.Configuration.ConfigurationSettings.AppSettings["NCache.LogPath"];
            if (installPath != null && installPath != string.Empty)
            {
                return installPath;
            }
            string path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;

            try
            {
#if NETCORE
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Registry.NetCore.RegistryUtil.RegUtil.LoadRegistry();
                    if (Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties != null ||
                        Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product != null ||
                        !string.IsNullOrEmpty(Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir))
                        path = Registry.NetCore.RegistryUtil.RegUtil.LicenseProperties.Product.InstallDir;
                }
                else
                    path = GetAppSetting("InstallDir");
#elif !NETCORE
                path = GetAppSetting("InstallDir");
#endif 
            }
            catch (Exception)
            {
#if CLIENT
                //ignore this exception as in case of Nuget client package, nclicense.dll is not shipped with
#else
                throw;
#endif
            }
            if (path == null || path.Length == 0)
#if CLIENT
                path = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar;
#else
                return null;
#endif

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
            if (key == "InstallDir" && IsRunningAsWow64)
            {
                string installDir = Environment.ExpandEnvironmentVariables("%NCHOME%");
                if (!string.IsNullOrEmpty(installDir) && !installDir.EndsWith(@"\"))
                    installDir = installDir + Path.DirectorySeparatorChar;

                return installDir;
            }

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
            if (!IsRunningAsWow64)
                section = RegHelper.ROOT_KEY + section;

            object tempVal = RegHelper.GetRegValue(section, key, 0);
            if (!(tempVal is String))
            {
                return Convert.ToString(tempVal);
            }
            return (String)tempVal;
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

        public static string LogDir
        {
            get { return logDir; }
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
                    currentOS = OSInfo.Unix;
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
                else
                {
                    //To log events on Linux related to daemon only. 
                    if (eventId == EventID.ServiceFailure || eventId == EventID.ServiceStart || eventId == EventID.ServiceStop)
                    {
                    try
                    {
                        using (StreamWriter streamWriter = new StreamWriter(GetInstallDir() + Path.DirectorySeparatorChar + serviceLogsPath, true))
                        {
                            string errorInformation = " Source: " + source + " == EventLogEntryType: " + type.ToString() + " == EventID: " + eventId + " == Message: " + msg;
                            streamWriter.WriteLine(DateTime.Now.ToString() + " == " + errorInformation);
                            streamWriter.WriteLine(" ");
                        }
                    }
                    catch (Exception) { }
                    }
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
        //private static DateTime START_DT = new DateTime(2004, 12, 31).ToUniversalTime();
        private static DateTime START_DT = new DateTime(2004, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc);

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
            //Check VS.Net 2005
            RegistryKey rKey8 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\8.0");
            RegistryKey rKey9 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\9.0");
            if (rKey8 != null)
            {
                if (rKey8.GetValue("InstallDir", "").ToString().Length != 0)
                    return true;
            }

            if (rKey9 != null)
            {
                if (rKey9.GetValue("InstallDir", "").ToString().Length != 0)
                    return true;
            }
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

    }
}
