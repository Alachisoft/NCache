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
using System.IO;
using System.Reflection;

namespace Alachisoft.NCache.Common
{
    public class DirectoryUtil
    {
        /// <summary>
        /// search for the specified file in the executing assembly's working folder
        /// if the file is found, then a path string is returned back. otherwise it returns null.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileLocalPath(string fileName)
        {
            string path = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName + Path.DirectorySeparatorChar + fileName;
            if (File.Exists(path))
                return path;
            return null;
        }

        /// <summary>
        /// search for the specified file in NCache install directory. if the file is found
        /// then returns the path string from where the file can be loaded. otherwise it returns 
        /// null.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileGlobalPath(string fileName, string directoryName)
        {
            string directoryPath = string.Empty;
            string filePath = string.Empty;

            if (!SearchGlobalDirectory(directoryName, false, out directoryPath)) return null;
            filePath = Path.Combine(directoryPath, fileName);
            if (!File.Exists(filePath))
                return null;
            return filePath;
        }

        public static bool SearchLocalDirectory(string directoryName, bool createNew, out string path)
        {
            path = Environment.CurrentDirectory;
            if (!Directory.Exists(path))
            {
                if (createNew)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                return false;
            }
            return true;
        }

        public static bool SearchGlobalDirectory(string directoryName, bool createNew, out string path)
        {
            string ncacheInstallDirectory = AppUtil.LogDir;
            path = string.Empty;            
            if (ncacheInstallDirectory == null)
                return false;
            path = Path.Combine(ncacheInstallDirectory, directoryName);
            if (!Directory.Exists(path))
            {
                if (createNew)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                return false;
            }
            return true;
        }
    }
}