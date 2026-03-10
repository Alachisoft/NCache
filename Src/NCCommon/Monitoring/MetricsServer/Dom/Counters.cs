using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    [ConfigurationRoot("counters")]
    public class Counters : ICloneable, ICompactSerializable
    {
       private Category[] _category;

        public Counters()
        {
        }

        [ConfigurationSection("category")]
        public Category[] Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public object Clone()
        {
            Counters counters = new Counters();
            counters.Category = Category;
            return counters;
        }

        public void Deserialize(CompactReader reader)
        {
            _category = reader.ReadObject() as Category[];
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_category);
        }
    }
}
