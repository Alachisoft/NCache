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
    public class UserProfile : ICloneable, ICompactSerializable
    {
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _company;

        public UserProfile()
        {
        }

        [ConfigurationAttribute("first-name")]
        public string FirstName
        {
            set { _firstName = value; }
            get { return _firstName; }
        }

        [ConfigurationAttribute("last-name")]
        public string LastName
        {
            set { _lastName = value; }
            get { return _lastName; }
        }


        [ConfigurationAttribute("email")]
        public string Email
        {
            get { return _email; }
            set { _email = value; }
        }

        [ConfigurationAttribute("company")]
        public string Company
        {
            get { return _company; }
            set { _company = value; }
        }

        public object Clone()
        {
            UserProfile userInfo = new UserProfile();
            userInfo.Email = Email;
            userInfo.FirstName = FirstName;
            userInfo.LastName = LastName;
            userInfo.Company = Company;
            return userInfo;
        }

        public void Deserialize(CompactReader reader)
        {
            _firstName = reader.ReadObject() as string;
            _lastName = reader.ReadObject() as string;
            _email = reader.ReadObject() as string;
            _company = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_firstName);
            writer.WriteObject(_lastName);
            writer.WriteObject(_email);
            writer.WriteObject(_company);
        }
    }
}
