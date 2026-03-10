//  Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Security
{
    /// <summary>
    /// Information holder class that provides the Security Parameters.
    /// </summary>
    public class SecurityParams
    {
        private string _uid;
        private string _pwd;

        /// <summary>
        /// Creates an instance of the SecurityParams.
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="password">Password</param>
        public SecurityParams(string userId, string password)
        {
            _uid = userId;
            _pwd = password;
        }

        /// <summary>
        /// User Id
        /// </summary>
        public string UserID
        {
            get { return _uid; }
        }

        /// <summary>
        /// Password
        /// </summary>
        public string Password
        {
            get { return _pwd; }
        }

        internal byte[] SecuredUserId
        {
            get
            {
                if (_uid != null && _uid != String.Empty)
                {
                    return EncryptionUtil.Encrypt(_uid);
                }
                return null;
            }
        }

        internal byte[] SecuredPassword
        {
            get
            {
                if (_pwd != null && _pwd != String.Empty)
                {
                    return EncryptionUtil.Encrypt(_pwd);
                }
                return null;
            }
        }

        internal static SecurityParams[] LoadFromConfig(string cacheId)
        {
            SecurityParams[] paramsList = new SecurityParams[2];
            Hashtable tbl = ConfigReader.ReadSecurityParams("client.ncconf", cacheId);

            if (tbl != null)
            {
                Hashtable pri_user = tbl["pri-user"] as Hashtable;
                Hashtable sec_user = tbl["sec-user"] as Hashtable;

                if (pri_user != null)
                {
                    string pri_password = pri_user["password"] as string;
                    if (pri_password != "")
                    {
                        paramsList[0] = new SecurityParams(pri_user["user-id"] as string, EncryptionUtil.Decrypt(Convert.FromBase64CharArray(pri_password.ToCharArray(), 0, pri_password.Length)));
                    }
                }
                if (sec_user != null)
                {
                    string sec_password = sec_user["password"] as string;
                    if (sec_password != "")
                    {
                        paramsList[1] = new SecurityParams(sec_user["user-id"] as string, EncryptionUtil.Decrypt(Convert.FromBase64CharArray(sec_password.ToCharArray(), 0, sec_password.Length)));
                    }
                }

            }
            else
            {
                paramsList[0] = new SecurityParams(null, null);
                paramsList[1] = new SecurityParams(null, null);
            }
            return paramsList;
        }

        private static string Decrypt(string Encrypted)
        {
            byte[] str = new byte[Encrypted.Length];
            for (int i = 0; i < Encrypted.Length; i++)
            {
                str[i] = Convert.ToByte(Encrypted[i]);
                str[i] -= 5;
            }

            string decryptedString = System.Text.Encoding.Default.GetString(str);
            return decryptedString;
        }
    }
}
