using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{

    [Serializable]
    public class Counter : ICloneable, ICompactSerializable
    {
        private string _name = "";
        private bool _publish = false;

        public Counter() { }
        public Counter(string name, bool publish)
        {
            Name = name;
            Publish = publish;
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

        public object Clone()
        {
            Counter counter = new Counter();
            counter.Publish = Publish;
            counter.Name = Name;
            return counter;
        }

        public void Deserialize(CompactReader reader)
        {
            Publish = reader.ReadBoolean();
            Name = reader.ReadObject() as string;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_publish);
            writer.WriteObject(_name);
        }
    }
}
