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
// limitations under the License.

using System;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    internal partial class SR
    {
        private static ResourceManager s_resourceManager;

        internal static Type ResourceType
        {
            get { return typeof(SR); }
        }

        private static ResourceManager ResourceManager
            => s_resourceManager ?? (s_resourceManager = new ResourceManager(ResourceType));

        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
        // by default it returns false.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UsingResourceKeys()
        {
            return false;
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey); }
            catch (MissingManifestResourceException) { }

            if (defaultString != null && resourceKey.Equals(resourceString, StringComparison.Ordinal))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + string.Join(", ", args);
                }

                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1);
            }

            return string.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2);
            }

            return string.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2, p3);
            }

            return string.Format(resourceFormat, p1, p2, p3);
        }

        internal static string GetString(string value)
        {
            return value;
        }

        internal static string GetString(string format, params object[] args)
        {
            return SR.Format(format, args);
        }
    }
}
