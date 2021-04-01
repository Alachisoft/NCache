using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.FeatureUsageData.Dom
{
    [Serializable]
    public class Profile : ICloneable, ICompactSerializable
    {
        public Profile()
        {
        }

        [ConfigurationSection("user-profile")]
        public UserProfile UserProfile { get; set; }

        [ConfigurationSection("hardware-profile")]
        public HardwareProfile HardwareProfile { get; set; }


        [ConfigurationSection("caching-profile")]
        public CachingProfile CachingProfile { get; set; }


        [ConfigurationSection("client-profile")]
        public ClientProfileDom ClientProfile { get; set; }


        public object Clone()
        {
            Profile profile = new Profile
            {
                CachingProfile = CachingProfile,
                HardwareProfile = HardwareProfile,
                UserProfile = UserProfile,
                ClientProfile = ClientProfile
            };
            return profile;
        }

        public void Deserialize(CompactReader reader)
        {
            UserProfile = reader.ReadObject() as UserProfile;
            HardwareProfile = reader.ReadObject() as HardwareProfile;
            CachingProfile = reader.ReadObject() as CachingProfile;
            ClientProfile = reader.ReadObject() as ClientProfileDom;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(UserProfile);
            writer.WriteObject(HardwareProfile);
            writer.WriteObject(CachingProfile);
            writer.WriteObject(ClientProfile);
        }
    }
}
