using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class Category : ICloneable, ICompactSerializable
    {
       private bool _publish = false;
       private string _name = "";
       private Counter[] _counters;
        public Category()
        {

        }

        [ConfigurationAttribute("name")]
        public string Name
        {
            set { _name = value; }
            get { return _name; }
        }

        [ConfigurationAttribute("publish")]
        public bool Publish
        {
            set { _publish = value; }
            get { return _publish; }
        }

        [ConfigurationSection("counter")]
        public Counter[] Counters
        {
            get { return _counters; }
            set { _counters = value; }
        }

        public object Clone()
        {
            Category category = new Category();
            category.Publish = Publish;
            category.Name = Name;
            category.Counters = Counters;
            return category;
        }

        public void Deserialize(CompactReader reader)
        {
            _publish = reader.ReadBoolean();
            _name = reader.ReadObject() as string;
            _counters = reader.ReadObject() as Counter[];
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_publish);
            writer.WriteObject(_name);
            writer.WriteObject(_counters);
        }
    }
}
