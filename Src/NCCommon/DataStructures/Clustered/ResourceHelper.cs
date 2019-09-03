/*
* Copyright (c) 2015, Alachisoft. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public class ResourceHelper
    {
        private static ResourceManager s_resourceManager;

        static ResourceHelper()
        {
            Assembly assembly = typeof(string).Assembly;
#if NETCORE
            s_resourceManager = new ResourceManager("FxResources.System.Private.CoreLib.SR", assembly);
#else
            s_resourceManager = new ResourceManager("mscorlib", assembly);
#endif
        }

        public static string GetResourceString(string name)
        {
            return s_resourceManager.GetString(name);
        }

        public static string GetResourceString(string name, CultureInfo culture)
        {
            return s_resourceManager.GetString(name, culture);
        }


    }
}
