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

namespace Alachisoft.NCache.Management.APILogging
{
    public class APILogging 
    {
        bool _enableLogging = false;
        static APILogging s_Instance;
        APILogManager _logmanager;
        
        public bool EnableLogging
        {
            set { _enableLogging = value; }
            get { return _enableLogging; }
        }

        public APILogManager APILogManger
        {
            get { return _logmanager; }
        }

        public static APILogging Instance
        {
            get { return s_Instance; }
            set { s_Instance = value; }
        }
       
        public  void StartLogging(string cacheID )
        {
            try
            {
                _enableLogging = true;
                _logmanager = new APILogManager();
            }
            catch
            {
            }
        }

        public void StopLogging(string cacheID)
        {
            try
            {
                _enableLogging = false;
                _logmanager.Dispose();
                
            }
            catch
            {
            }
        }
    }
}
