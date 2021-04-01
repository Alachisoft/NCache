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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Web;
using System.Text.RegularExpressions;
using Alachisoft.ContentOptimization.Diagnostics.Logging;

namespace Alachisoft.ContentOptimization.Diagnostics.Counters
{
    public abstract class PerfCountersBase
    {
        const int MAX_INSTANCE_NAME_LEN = 128;

        protected virtual bool UserHasAccessRights(string catagory)
        {
            PerformanceCounterPermission permissions = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Write, ".", catagory);
            permissions.Demand();

            if (!PerformanceCounterCategory.Exists(catagory, "."))
                return false;
            
            return true;
        }

        public static string EncodeInstanceName(string instanceName)
        {
            var invalidChars = new Regex(@"[()#\/]");
            instanceName = invalidChars.Replace(instanceName, "_");
            if (instanceName.Length > MAX_INSTANCE_NAME_LEN)
                instanceName = instanceName.Substring(0, MAX_INSTANCE_NAME_LEN);
            return instanceName;
        }
    }
}
