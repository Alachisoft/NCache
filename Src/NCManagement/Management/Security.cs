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
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Settings related to security.
    /// </summary>
    public class Security
    {
        internal static string BASE_KEY = RegHelper.ROOT_KEY + RegHelper.APPBASE_KEY + @"\Options";
        private static RtContextValue _context=RuntimeContext.CurrentContext;
        internal static string SECURITY_KEY = BASE_KEY + @"\Security";
        static private string _userName;
        static private string _passwd;
        static private string _dc;
        static private string _port;
        static private string _userdistinguishedName;
        static private string _modifieddistinguishedName;
        static private string _organizationUnit;
        static private string _searchBase;
        static private string _modifiedUserName;
        static private string SECURITY_TYPE = "ActiveDirectory";
        static Security()
        {
            Refresh();
        }

        static public RtContextValue Context
        {
            get { return _context;}
            set { _context = value; }
        }

        static public string SearchBase
        {
            get { return _searchBase; }
            set { _searchBase = value; }
        }

        static public string DistinguishedName
        {
            get { return _userdistinguishedName; }
            set { _userdistinguishedName = value; }
        }

        static public string ModifiedDistinguishedName
        {
            get { return _modifieddistinguishedName; }
            set { _modifieddistinguishedName = value; }
        }

        static public string ModifiedUserName
        {
            get { return _modifiedUserName; }
            set { _modifiedUserName = value; }
        }

        static public string OrganizationUnit
        {
            get { return _organizationUnit; }
            set { _organizationUnit = value; }
        }

        static public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        static public string Passwd
        {
            get { return _passwd; }
            set { _passwd = value; }
        }

        static public string DomainController
        {
            get { return _dc; }
            set { _dc = value; }
        }

        static public string Port
        {
            get { return _port; }
            set { _port = value; }
        }

        static public void Refresh()
        { 
             RefreshContext();
             string key=SECURITY_KEY+@"\"+SECURITY_TYPE;
             _userName = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key,"UserName", 0));
             _passwd = Convert.ToString(RegHelper.GetDecryptedRegValueFromCurrentUser(key, "Passwd", 0));
             _dc = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "DomainController", 0));
             _port = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "Port", 0));
            if (_context.Equals(RtContextValue.JVCACHE))
            {
                _userdistinguishedName = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "DistinguishedName", 0));
                _modifieddistinguishedName = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "ModifiedDistinguishedName", 0));
                _organizationUnit = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "OrganizationUnit", 0));
                _searchBase = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "SearchBase", 0));
                _modifiedUserName = Convert.ToString(RegHelper.GetRegValueFromCurrentUser(key, "ModifiedUserName", 0));
            }
            else 
            {
                _userdistinguishedName = "";
                _modifieddistinguishedName = "";
                _organizationUnit = "";
                _searchBase = "";
                _modifiedUserName = "";
            }
        }

        static public void Apply()
        {
            RefreshContext();
            string key = SECURITY_KEY + @"\" + SECURITY_TYPE;
            RegHelper.SetRegValueInCurrentUser(key, "UserName", _userName, 0);
            RegHelper.SetEncryptedRegValueInCurrentUser(key, "Passwd", _passwd);
            RegHelper.SetRegValueInCurrentUser(key, "DomainController", _dc, 0);
            RegHelper.SetRegValueInCurrentUser(key, "Port", _port, 0);

            if (_context.Equals(RtContextValue.JVCACHE))
            {
                RegHelper.SetRegValueInCurrentUser(key, "DistinguishedName", _userdistinguishedName, 0);
                RegHelper.SetRegValueInCurrentUser(key, "OrganizationUnit", _organizationUnit, 0);

                RegHelper.SetRegValueInCurrentUser(key, "ModifiedDistinguishedName", _modifieddistinguishedName, 0);
                RegHelper.SetRegValueInCurrentUser(key, "SearchBase", _searchBase, 0);
                RegHelper.SetRegValueInCurrentUser(key, "ModifiedUserName", _modifiedUserName, 0);
            }
        }

        public static void UpdateCredentials(string userName, string password)
        {
            _userName = userName;
            _passwd = password;
            _modifiedUserName = userName;
            Apply();
        }

        static private void RefreshContext()
        {
            if (_context.Equals(RtContextValue.NCACHE))
                SECURITY_TYPE = "ActiveDirectory";
            else
                SECURITY_TYPE = "Ldap";
        }
    }
}
