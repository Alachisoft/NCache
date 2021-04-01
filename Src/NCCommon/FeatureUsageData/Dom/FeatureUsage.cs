using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData.Dom
{
    [Serializable]
    [ConfigurationRoot("feature-usage")]
    public class FeatureUsage : ICloneable, ICompactSerializable
    {
        private string _edition;
        private string _version;
        private string _platform;
        private string _updateTime;
        private string _lastPostingTime;
        private Profile _profile;
        private FeatureDetails _featureDetails;

        public FeatureUsage()
        {
        }

        [ConfigurationAttribute("edition")]
        public string Edition
        {
            set { _edition = value; }
            get { return _edition; }
        }

        [ConfigurationAttribute("version")]
        public string Version
        {
            set { _version = value; }
            get { return _version; }
        }

        [ConfigurationAttribute("platform")]
        public string Platform
        {
            set { _platform = value; }
            get { return _platform; }
        }
        [ConfigurationAttribute("update-time")]
        public string UpdateTime
        {
            set { _updateTime = value; }
            get { return _updateTime; }
        }

        [ConfigurationAttribute("last-posting-time")]
        public string LastPostingTime
        {
            set { _lastPostingTime = value; }
            get { return _lastPostingTime; }
        }

        [ConfigurationSection("profile")]
        public Profile Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        [ConfigurationSection("feature-details")]
        public FeatureDetails FeatureDetails
        {
            get { return _featureDetails; }
            set { _featureDetails = value; }
        }
        public object Clone()
        {
            FeatureUsage featureUsage = new FeatureUsage();
            featureUsage.Edition = Edition;
            featureUsage.Version = Version;
            featureUsage.Platform = Platform;
            featureUsage.UpdateTime = UpdateTime;
            featureUsage.Profile = Profile;
            featureUsage.LastPostingTime = LastPostingTime;
            featureUsage.FeatureDetails = FeatureDetails;
            return featureUsage;
        }

        public void Deserialize(CompactReader reader)
        {
            _edition = reader.ReadObject() as string;
            _version = reader.ReadObject() as string;
            _platform = reader.ReadObject() as string;
            _updateTime = reader.ReadObject() as string;
            _profile = reader.ReadObject() as Profile;
            _lastPostingTime = reader.ReadObject() as string;
            _featureDetails = reader.ReadObject() as FeatureDetails;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_edition);
            writer.WriteObject(_version);
            writer.WriteObject(_platform);
            writer.WriteObject(_updateTime);
            writer.WriteObject(_profile);
            writer.Write(_lastPostingTime);
            writer.WriteObject(_featureDetails);
        }
    }
}
