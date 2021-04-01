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

namespace Alachisoft.NCache.Common.Util
{
    public class ConfigurationExitCodes
    {
        public const int NOT_REGISTERED = 4;
        public const int CONFIG_MISMATCH = 3;
        public const int DEPLOYMENT_MISMATCH = 2;
        public const int CONFIGID_MATCH =1;


        public const string CONFIG = "config-id";
        public const string CONFIG_ID = "config-version";
        public const string DEPLOYMENT_ID = "dep-version";
      
    }
}
