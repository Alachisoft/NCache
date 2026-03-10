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
using System.Collections;
using System.Diagnostics;
using System.Text;
using Alachisoft.NCache.Common.Util;
using Microsoft.Win32;
using System.IO;

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Class that helps do common registry operations
    /// </summary>
    public class RegHelper
    {
        public static string  ROOT_KEY = @"Software\Alachisoft\NCache";
        public static string  APPBASE_KEY = @"\NCache Manager";
        public static string REMOTEAUTH_KEY = @"Software\Alachisoft\NCache\NCache Monitor";
        public static string  MANAGER_OPTIONS = APPBASE_KEY + @"\Options";
        public static string MONITOR_OPTIONS = REMOTEAUTH_KEY;
        public static string MONITOR_ROWS_BASE = @"Software\Alachisoft\NCache\NCache Manager\Options";
        public static string SSL_KEY = Path.Combine(ROOT_KEY, "TLS");

        static RegHelper()
        {
        }
       
        public static RegistryKey NewKey(string keyPath)
        {
            return Registry.LocalMachine.CreateSubKey(keyPath);
        }

        public static RegistryKey NewUserKey(string keyPath)
        {
            return Registry.CurrentUser.CreateSubKey(keyPath);
        }

        public static bool NewUserKey(string keyPath, string subKey, string keyValue)
        {
            try
            {
              RegistryKey key=   Registry.CurrentUser.CreateSubKey(keyPath);
              key.SetValue(subKey, keyValue);
              return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Get a key value from the registry.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        static public object GetDecryptedRegValue(string keypath, string key, short prodId)
        {
            try
            {
                object val = GetRegValue(keypath, key,prodId);
                return Protector.DecryptString(Convert.ToString(val));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Save a registry value.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        static public void SetEncryptedRegValue(string keypath, string key, object val)
        {
            try
            {
                SetRegValue(keypath, key, Protector.EncryptString(Convert.ToString(val)),0);
            }
            catch (Exception)
            {
            }
        }        

        /// <summary>
        /// Get a key value from the registry. Automatically caters with wow64 registry read mechanism
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        static public object GetRegValue(string section, string key,short prodId)
        {
            try
            {
                return GetRegValueInternal(section, key, prodId);
            }
            catch
            {
                return null;
            }
        }

        static public string GetLicenseKey(short prodId)
        {
            return (string)RegHelper.GetRegValueInternal("UserInfo", "licensekey", prodId);
        }

        /// <summary>
        /// Get a key value from the registry.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        static public int GetRegValues(string keypath, Hashtable ht, short prodId)
        {
            RegistryKey root;
            try
            {
                root = Registry.LocalMachine.OpenSubKey(keypath);
                if (root != null)
                {
                    string[] keys = root.GetValueNames();
                    for (int i = 0; i < keys.Length; i++)
                        ht[keys[i]] = GetRegValue(keypath, keys[i], prodId);
                }                
            }
            catch (Exception)
            {
            }
            return ht.Count;
        }

        static public int GetBooleanRegValuesFromCurrentUser(string keypath, Hashtable ht, short prodId)
        {
            RegistryKey root;
            try
            {
                root = Registry.CurrentUser.OpenSubKey(keypath);
                if (root != null)
                {
                    string[] keys = root.GetValueNames();
                    for (int i = 0; i < keys.Length; i++)
                        ht[keys[i]] = Convert.ToBoolean(GetRegValueFromCurrentUser(keypath, keys[i], prodId));
                }
            }
            catch (Exception)
            {
            }
            return ht.Count;
        }

        /// <summary>
        /// Save a registry value.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        static public void SetRegValue(string keypath, string key, object val,short prodId)
        {
            try
            {
                RegistryKey root = Registry.LocalMachine.OpenSubKey(keypath, true);
                if(root != null)
                    root.SetValue(key, val);
            }
            catch (Exception)
            {
               
            }
        }

        static public bool IsRegKeyExist(string keypath)
        {
            try
            {
                RegistryKey root = Registry.LocalMachine.OpenSubKey(keypath, true);
                if (root != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        static public bool IsCurrentUserRegKeyExist(string keypath)
        {
            try
            {
                RegistryKey root = Registry.CurrentUser.OpenSubKey(keypath, true);
                if (root != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        static public void DeleteRegValue(string keypath, string key)
        {
            try
            {
                RegistryKey root = Registry.LocalMachine.OpenSubKey(keypath, true);
                if (root != null)
                    root.DeleteValue(key);
            }
            catch (Exception)
            {

            }

        }
        /// <summary>
        /// Save a registry value.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        static public void SetRegValues(string keypath, Hashtable val,short prodId)
        {
            try
            {
                IDictionaryEnumerator ie = val.GetEnumerator();
                while (ie.MoveNext())
                {
                    SetRegValue(keypath, Convert.ToString(ie.Key), ie.Value, prodId);
                }
            }
            catch (Exception)
            {
            }
        }


        static public void SetRegValuesInCurrentUser(string keypath, Hashtable val, short prodId)
        {
            try
            {
                IDictionaryEnumerator ie = val.GetEnumerator();
                while (ie.MoveNext())
                {
                    SetRegValueInCurrentUser(keypath, Convert.ToString(ie.Key), ie.Value, prodId);
                }
            }
            catch (Exception)
            {
            }
        }

            
        static private string GetRegValueInternal(string section, string key, short prodId)
        {
            try
            {
                string value64 = string.Empty;
                RegistryKey localKey =
                    RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                        RegistryView.Registry64);
                localKey = localKey.OpenSubKey(section);
                if (localKey != null)
                {
                    var regValue = localKey.GetValue(key);
                    if (regValue != null)
                        value64 = regValue.ToString();
                    localKey.Close();
                }
                return value64;
            }
            catch (Exception ex) { }
            return "";
        }

        public static Hashtable GetCacheValuesfromUserRegistry(string path)
        {
            Hashtable caches = new Hashtable();
            try
            {
                RegistryKey root = Registry.CurrentUser.OpenSubKey(path, true);
                string[] subkeys = root.GetValueNames();
                if (subkeys != null && subkeys.Length > 0)
                {
                    foreach (string subKey in subkeys)
                    {
                        caches.Add(subKey, root.GetValue(subKey));
                    }

                }
            }
            catch (Exception ex)
            {

            }
            return caches;
        } 

        public static object GetRegValueFromCurrentUser(string section, string key, short prodId)
        {
            try
            {
                RegistryKey root = Registry.CurrentUser.OpenSubKey(section);
                if (root != null)
                    return root.GetValue(key);

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static object GetDecryptedRegValueFromCurrentUser(string keypath, string key, short prodId)
        {
            try
            {
                object val = GetRegValueFromCurrentUser(keypath, key,prodId);
                return Protector.DecryptString(Convert.ToString(val));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SetRegValueInCurrentUser(string keypath, string key, object val, short prodId)
        {
            try
            {
                int retry = 0;
                retry:
                RegistryKey root = Registry.CurrentUser.OpenSubKey(keypath, true);
                if (root != null)
                    root.SetValue(key, val);
                else
                {
                    Registry.CurrentUser.CreateSubKey(keypath);
                    retry++;
                    if(retry == 1)
                        goto retry;
                }
            }
            catch (Exception)
            {

            }
        }

        public static void SetEncryptedRegValueInCurrentUser(string keypath, string key, object val)
        {
            try
            {
                SetRegValueInCurrentUser(keypath, key, Protector.EncryptString(Convert.ToString(val)), 0);
            }
            catch (Exception)
            {
            }
        }                
    }
}