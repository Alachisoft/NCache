using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml;

namespace Lextm.SharpSnmpLib.Browser
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class ProfileRegistry : IProfileRegistry
    {
        private const string SettingFile = "Agents4.xml";
        private const string NameAuthenticationMethod = "authenticationMethod";
        private const string NamePrivacyPassphrase = "privacyPassphrase";
        private const string NamePrivacyMethod = "privacyMethod";
        private const string NameUserName = "userName";
        private const string NameTimeout = "timeout";
        private const string NameAuthenticationPassphrase = "authenticationPassphrase";
        private const string NameSetCommunity = "setCommunity";
        private const string NameGetCommunity = "getCommunity";
        private const string NameVersion = "version";
        private AgentProfile _defaultProfile;
        private readonly IDictionary<Guid, AgentProfile> _profiles = new Dictionary<Guid, AgentProfile>();
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Browser");
        private const string NameName = "name";
        private const string NameAdd = "add";
        private const string NameDefaultAgent = "defaultAgent";
        private const string NameAgent = "agents";
        private const string NameBinding = "binding";
        private const string NameId = "id";

        public event EventHandler<EventArgs> OnChanged;

        public IEnumerable<AgentProfile> Profiles
        {
            get { return _profiles.Values; }
        }

        public AgentProfile DefaultProfile
        {
            get 
            { 
                return _defaultProfile; 
            }
            
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _defaultProfile = value;
            }
        }

        public void AddProfile(AgentProfile profile)
        {
            AddInternal(profile);
            if (OnChanged != null)
            {
                OnChanged(null, EventArgs.Empty);
            }
        }

        private void AddInternal(AgentProfile profile)
        {
            if (_profiles.ContainsKey(profile.Id))
            {
                throw new BrowserException("This endpoint is already registered");
            }

            _profiles.Add(profile.Id, profile);

            if (DefaultProfile == null)
            {
                DefaultProfile = profile;
            }
        }
        
        public void DeleteProfile(AgentProfile profile)
        {
            DeleteInternal(profile.Id);
            if (OnChanged != null)
            {
                OnChanged(null, EventArgs.Empty);
            }
        }

        private void DeleteInternal(Guid id)
        {
            DeleteInternal(id, false);
        }

        private void DeleteInternal(Guid id, bool replace)
        {
            if (id.Equals(DefaultProfile.Id) && !replace)
            {
                throw new BrowserException("Cannot delete the default endpoint!");
            }

            if (_profiles.ContainsKey(id))
            {
                _profiles.Remove(id);
            }
        }

        public void LoadProfiles()
        {
            if (LoadProfilesFromFile() == 0)
            {
                LoadDefaultProfile();
            }
        }

        public void SaveProfiles()
        {
            ICollection<Guid> keys = _profiles.Keys;
            using (XmlTextWriter writer = new XmlTextWriter(SettingFile, null))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement(NameAgent);
                writer.WriteStartAttribute(NameDefaultAgent);
                writer.WriteString(_defaultProfile.Id.ToString());
                writer.WriteEndAttribute();

                foreach (Guid k in keys)
                {
                    writer.WriteStartElement(NameAdd);
                    
                    writer.WriteStartAttribute(NameId);
                    writer.WriteString(_profiles[k].Id.ToString());
                    writer.WriteEndAttribute();
                    
                    writer.WriteStartAttribute(NameName);
                    writer.WriteString(_profiles[k].Name);
                    writer.WriteEndAttribute();
                    
                    writer.WriteStartAttribute(NameBinding);
                    writer.WriteString(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", _profiles[k].Agent.Address, _profiles[k].Agent.Port));
                    writer.WriteEndAttribute();
                    
                    writer.WriteStartAttribute(NameVersion);
                    writer.WriteValue((int)_profiles[k].VersionCode);
                    writer.WriteEndAttribute();

                    var normal = _profiles[k] as NormalAgentProfile;
                    writer.WriteStartAttribute(NameGetCommunity);
                    writer.WriteString(normal == null ? string.Empty : normal.GetCommunity.ToString());
                    writer.WriteEndAttribute();
                    
                    writer.WriteStartAttribute(NameSetCommunity);
                    writer.WriteString(normal == null ? string.Empty : normal.SetCommunity.ToString());
                    writer.WriteEndAttribute();

                    var secure = _profiles[k] as SecureAgentProfile;
                    writer.WriteStartAttribute(NameAuthenticationPassphrase);
                    writer.WriteString(secure == null ? string.Empty : secure.AuthenticationPassphrase);
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute(NamePrivacyPassphrase);
                    writer.WriteString(secure == null ? string.Empty : secure.PrivacyPassphrase);
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute(NameAuthenticationMethod);
                    writer.WriteString(secure == null ? string.Empty : secure.AuthenticationMethod.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute(NamePrivacyMethod);
                    writer.WriteString(secure == null ? string.Empty : secure.PrivacyMethod.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute(NameUserName);
                    writer.WriteString(_profiles[k].UserName);
                    writer.WriteEndAttribute();

                    writer.WriteStartAttribute(NameTimeout);
                    writer.WriteValue(_profiles[k].Timeout);
                    writer.WriteEndAttribute();

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
        }

        private void LoadDefaultProfile()
        {
            AgentProfile first = AgentProfileFactory.Create(Guid.Empty, VersionCode.V1, new IPEndPoint(IPAddress.Loopback, 161), "public", "private", "localhost", string.Empty, string.Empty, 0, 0, string.Empty, 1000);
            AddProfile(first);
            DefaultProfile = first;
        }

        private int LoadProfilesFromFile()
        {
            if (!File.Exists(SettingFile))
            {
                return 0;
            }

            Guid defaultId = Guid.Empty;
            using (XmlTextReader reader = new XmlTextReader(SettingFile))
            {
                try
                {
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                {
                                    if (reader.Name == NameAgent)
                                    {
                                        defaultId = new Guid(reader.GetAttribute(NameDefaultAgent));
                                    }
                                    else if (reader.Name == NameAdd)
                                    {
                                        string name = reader.GetAttribute(NameName);
                                        Guid id = new Guid(reader.GetAttribute(NameId));
                                        string[] parts = reader.GetAttribute(NameBinding).Split(new[] { ':' });
                                        IPAddress def = IPAddress.Parse(parts[0]);
                                        int port = int.Parse(parts[1], CultureInfo.InvariantCulture);
                                        VersionCode vc = (VersionCode)int.Parse(reader.GetAttribute(NameVersion), CultureInfo.InvariantCulture);
                                        string get = reader.GetAttribute(NameGetCommunity);
                                        string set = reader.GetAttribute(NameSetCommunity);
                                        string authenticationPassphrase = reader.GetAttribute(NameAuthenticationPassphrase);
                                        string privacyPassphrase = reader.GetAttribute(NamePrivacyPassphrase);
                                        string authenticationMethod = reader.GetAttribute(NameAuthenticationMethod);
                                        string privacyMethod = reader.GetAttribute(NamePrivacyMethod);
                                        string userName = reader.GetAttribute(NameUserName);
                                        int timeout = int.Parse(reader.GetAttribute(NameTimeout), CultureInfo.InvariantCulture);

                                        AgentProfile profile = AgentProfileFactory.Create(
                                            id, 
                                            vc, 
                                            new IPEndPoint(def, port), 
                                            get, 
                                            set, 
                                            name,
                                            authenticationPassphrase, 
                                            privacyPassphrase,
                                            string.IsNullOrEmpty(authenticationMethod) ? 0 : int.Parse(authenticationMethod, CultureInfo.InvariantCulture),
                                            string.IsNullOrEmpty(privacyMethod) ? 0 : int.Parse(privacyMethod, CultureInfo.InvariantCulture),
                                            userName, 
                                            timeout);
                                        if (id == defaultId)
                                        {
                                            DefaultProfile = profile;
                                        }
                                        
                                        AddProfile(profile);
                                    }
                                    
                                    break;
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }

            return _profiles.Count;
        }
    }
}
