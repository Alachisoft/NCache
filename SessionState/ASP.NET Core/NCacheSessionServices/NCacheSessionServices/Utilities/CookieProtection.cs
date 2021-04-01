//Copyright(c) .NET Foundation.All rights reserved.


//Licensed under the Apache License, Version 2.0 (the "License"); you may not use
//these files except in compliance with the License.You may obtain a copy of the
//License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed
//under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
//CONDITIONS OF ANY KIND, either express or implied. See the License for the
//specific language governing permissions and limitations under the License.

using System;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace Alachisoft.NCache.Web.SessionState.Utilities
{
    internal static class CookieProtection
    {
        internal static string Protect(IDataProtector protector, string data)
        {
            if (protector == null)
            {
                throw new ArgumentNullException(nameof(protector));
            }
            if (string.IsNullOrEmpty(data))
            {
                return data;
            }

            var userData = Encoding.UTF8.GetBytes(data);

            var protectedData = protector.Protect(userData);
            return Convert.ToBase64String(protectedData).TrimEnd('=');
        }

        internal static string Unprotect(IDataProtector protector, string protectedText)
        {
            try
            {
                if (string.IsNullOrEmpty(protectedText))
                {
                    return string.Empty;
                }

                var protectedData = Convert.FromBase64String(Pad(protectedText));
                if (protectedData == null)
                {
                    return string.Empty;
                }

                var userData = protector.Unprotect(protectedData);
                if (userData == null)
                {
                    return string.Empty;
                }

                return Encoding.UTF8.GetString(userData);
            }
            catch (Exception)
            {
                // Log the exception, but do not leak other information
                return string.Empty;
            }
        }

        private static string Pad(string text)
        {
            var padding = 3 - ((text.Length + 3) % 4);
            if (padding == 0)
            {
                return text;
            }
            return text + new string('=', padding);
        }
    }
}
