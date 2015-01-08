
using System;
using System.Diagnostics;
using Alachisoft.NCache.Common.Interop;
using Microsoft.Win32;

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

        static int s_logLevel = 7;
        static string javaLibDir = null;
        static AppUtil()
        {
            try
            {
                isRunningAsWow64 = Win32.InternalCheckIsWow64();
            }
            catch (Exception ex)
            {
                LogEvent("Win32.InternalCheckIsWow64() Error " + ex.Message, EventLogEntryType.Error);
            }
            installDir = GetInstallDir();

            javaLibDir = GetJavaLibDir();
            DeployedAssemblyDir = installDir + DeployedAssemblyDir;
            string logLevel = System.Configuration.ConfigurationSettings.AppSettings["NCacheServer.EventLogLevel"];

            if (logLevel != null && logLevel != "")
            {
                logLevel = logLevel.ToLower();
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
            string path = System.Environment.CurrentDirectory + "\\";

            try
            {
                path = GetAppSetting("InstallDir");
            }
            catch (Exception)
            {
                throw;
            }
            if (path == null || path.Length == 0)
                return null;

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
            if (!IsRunningAsWow64)
                section = RegHelper.ROOT_KEY + section;

            object tempVal = RegHelper.GetRegValue(section, key,0);
            if (! (tempVal is String))
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

            return (string)RegHelper.GetDecryptedRegValue(section, key,0);
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

            RegHelper.SetRegValue(section, key, value,prodId);
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


        /// <summary>
        /// Writes an error, warning, information, success audit, or failure audit 
        /// entry with the given message text to the event log.
        /// </summary>
        /// <param name="msg">The string to write to the event log.</param>
        /// <param name="type">One of the <c>EventLogEntryType</c> values.</param>
        public static void LogEvent(string msg, EventLogEntryType type)
        {
            string cacheserver="NCache";

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

        /// <summary>
        /// Checks environment to verify if there is 'Any' version of Visual Studio installed.
        /// and removing millisecond information
        /// </summary>
        public static bool IsVSIdeInstalled()
        {
            RegistryKey rKey8 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\8.0");
            RegistryKey rKey9 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\9.0");

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
    }
}
