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
using System.Collections;
using System.DirectoryServices.Protocols;
using Alachisoft.NCache.Common.DataStructures;
using System.DirectoryServices;

using System.Net;


namespace Alachisoft.NCache.Common
{
    public class DomainSearcher
    {
        static private string _domainName;
        static private Hashtable domains;

        private static int port = 389;
        private static string hostName;
        private static string domainName;

        private static string userName;
        private static string password;
        private static string organiztaionunit;

        private static LdapConnection _ldapConnection;

        static DomainSearcher()
        {

        }

        // for connecting using a different user name, password and domain name
        // while not required, you should always pass encrypted credentials
        // over the wire. This example uses a secure connection
        private static LdapConnection GetLdapConnection(string hostOrDomainName, int port, string userName, string password)
        {

            LdapConnection _connection;
            if (string.IsNullOrEmpty(hostOrDomainName) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                throw new Exception("Insufficient information provided.");
            //to use LDAP calls, you must first connect to the directory.
            try
            {
                NetworkCredential credential = new NetworkCredential();

                credential.UserName = userName;
                credential.Password = password;

                _connection = new LdapConnection(new LdapDirectoryIdentifier(hostOrDomainName, port), credential, AuthType.Basic);
                _connection.SessionOptions.ProtocolVersion = 3;

                _connection.Bind(credential);
                return _connection;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static int ConvertPort(string port)
        {

            if (!string.IsNullOrEmpty(port))
            {
                return Convert.ToInt32(port);

            }
            return 0;
        }

        //Verfiy User In Ldap Directory Having Proper Authentication 
        public static bool VerifyLdapUser(string hostOrDomainName, string port, string userName, string password)
        {

            int ldapPort = ConvertPort(port);
            if (ldapPort == 0) ldapPort = 389;

            _ldapConnection = GetLdapConnection(hostOrDomainName, ldapPort, userName, password);

            bool _verfyUser = false;

            NetworkCredential credential = new NetworkCredential();
            credential.UserName = userName.Trim();
            credential.Password = password.Trim();

            try
            {

                _ldapConnection.Bind(credential);
                _verfyUser = true;

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return _verfyUser;
        }

        //Verfiy User In Active Directory
        public static bool VerifyADUser(string hostOrDomainName, string userName, string password)
        {
            try
            {
                DirectoryEntry de = new DirectoryEntry("LDAP://" + hostOrDomainName, userName, password, AuthenticationTypes.Secure);
                object o = de.NativeObject;
                DirectorySearcher ds = new DirectorySearcher(de);
                ds.Filter = "SAMAccountname=" + userName;
                ds.PropertiesToLoad.Add("cn");
                SearchResult sr = ds.FindOne();
                if (sr == null) return false;

                DirectoryEntry userDE = sr.GetDirectoryEntry();
                if (userDE != null)
                    return true;
                else
                    return false;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public static DomainInfo GetDomainInfo(string domainController, string port, string userName, string password, string searchBase, RtContextValue _context)
        {
            int ldapPort = ConvertPort(port);
            if (ldapPort == 0) ldapPort = 389;
            DomainInfo domainInfo = new DomainInfo();
            if (domains != null && domains.Contains(domainController))
            {
                domainInfo.DomainName = domainController;
                domainInfo.Users = (ArrayList)domains[domainController];
                domainInfo.DistiguishNames = (Hashtable)domains[domainController + "1"];
                return domainInfo;
            }

            if (domains == null)
                domains = new Hashtable();
            ArrayList users = new ArrayList();
            if (_context.Equals(RtContextValue.JVCACHE))
            {
                Hashtable distinguishNames = new Hashtable();

                //Connection Build for Ldap Authentication 
                _ldapConnection = GetLdapConnection(domainController, ldapPort, userName, password);

                string filter = "(objectClass=*)";

                String[] attribsToReturn;

                string attribs = "cn";

                // split the single string expression from the string argument into an array of strings
                attribsToReturn = attribs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                SearchRequest searchRequest = new SearchRequest(searchBase, filter, System.DirectoryServices.Protocols.SearchScope.Subtree, attribsToReturn);
                try
                {
                    SearchResponse searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);

                    if (searchResponse.Entries.Count > 0)
                    {
                        foreach (SearchResultEntry entry in searchResponse.Entries)
                        {
                            // retrieve a specific attribute
                            SearchResultAttributeCollection attributes = entry.Attributes;
                            foreach (DirectoryAttribute attribute in attributes.Values)
                            {
                                users.Add(attribute[0].ToString());
                                distinguishNames.Add(attribute[0].ToString(), entry.DistinguishedName);
                            }
                        }
                    }
                }
                catch (Exception ex) { throw ex; }

                domainInfo.Users = users;
                domainInfo.DistiguishNames = distinguishNames;

                domains[domainController] = users;
                domains[domainController + "1"] = distinguishNames;

            }
            else
            {
                #region --- Previous Code ----
                DirectoryEntry adRoot = new DirectoryEntry("LDAP://" + domainController, userName, password);
                DirectorySearcher searcher = new DirectorySearcher(adRoot);
                searcher.SearchScope = System.DirectoryServices.SearchScope.Subtree;
                searcher.ReferralChasing = ReferralChasingOption.All;
                searcher.Filter = "(&(objectClass=user)(objectCategory=person))";
                searcher.PropertiesToLoad.Add("SAMAccountname");
                searcher.PageSize = 1000;

                try
                {
                    SearchResultCollection result = searcher.FindAll();
                    foreach (SearchResult a in result)
                    {
                        DirectoryEntry entry = a.GetDirectoryEntry();
                        string UserName = a.Properties["SAMAccountname"][0].ToString();
                        users.Add(UserName);
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    adRoot.Dispose();
                    searcher.Dispose();
                }
                domainInfo.Users = users;

                domains[domainController] = users;
                #endregion
            }
            return domainInfo;
        }

        public static string GetDomainName(string domainController, string port, string userName, string password, RtContextValue _context)
        {
            string _domainName = "";

            if (_context.Equals(RtContextValue.JVCACHE))
            {
                int ldapPort = ConvertPort(port);
                if (ldapPort == 0) ldapPort = 389;
                _ldapConnection = _ldapConnection = GetLdapConnection(domainController, ldapPort, userName, password);

                try
                {
                    _ldapConnection.Bind();
                    _domainName = domainController;
                }
                catch (LdapException ex)
                {
                    throw ex;
                }
            }
            else
            {
                #region -----Previous code-----
                DirectoryEntry adRoot = new DirectoryEntry("LDAP://" + domainController, userName, password);
                DirectorySearcher searcher = new DirectorySearcher(adRoot);
                try
                {
                    SearchResult result = searcher.FindOne();
                    _domainName = ExtractDomainName(result.Path, _context);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    adRoot.Dispose();
                    searcher.Dispose();
                }

                #endregion
            }
            return _domainName;
        }

        private static string ExtractDomainName(string path, RtContextValue _context)
        {
            string domainName = "";
            string[] dcPath = path.Split(',');
            if (_context.Equals(RtContextValue.JVCACHE))
            {
                foreach (string str in dcPath)
                {

                    string[] keyvalue = str.Split('=');
                    if (keyvalue[0].ToLower() == "dc")
                    {
                        domainName += keyvalue[1];
                        domainName += ".";
                    }
                }
            }
            else
            {
                foreach (string str in dcPath)
                {
                    int index = str.IndexOf('=');
                    domainName += str.Substring(index + 1);
                    domainName += ".";
                }
            }
            domainName = domainName.Remove(domainName.Length - 1);
            return domainName;
        }

        // extract distinguish name of user which has allready verified. Then this distinguished name used for futher searches. 

        public static string getOU(SearchResponse searchResponse, string userName)
        {

            string[] splitString;
            if (searchResponse.Entries.Count > 0)
            {
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {

                    if (entry.DistinguishedName.ToLower().Contains(userName) && entry.DistinguishedName.ToLower().Contains("ou"))
                    {
                        splitString = entry.DistinguishedName.Split(',');
                        for (int i = 0; i < splitString.Length; i++)
                        {
                            if (splitString[i].ToLower().Contains("ou"))
                            {
                                return splitString[i].ToString();
                            }
                        }
                    }
                }
            }
            return null;

        }
    }
}

