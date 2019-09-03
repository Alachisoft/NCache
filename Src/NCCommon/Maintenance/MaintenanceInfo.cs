using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Maintenance
{
    public class MaintenanceInfo : ICompactSerializable
    {
        TimeSpan timeout = TimeSpan.MaxValue;

        Address address = null;

        public TimeSpan Timeout
        {
            set
            {
              
                    timeout = value;
            }
            get
            {
                return timeout;
            }
        }

        public Address Address
        {
            set
            {
                address = value;
            }
            get
            {
                return address;
            }
        }

        public void Deserialize(CompactReader reader)
        {
            Address = reader.ReadObject() as Address;
            Timeout = new TimeSpan(reader.ReadInt64());
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(address);
            writer.Write(timeout.Ticks);
        }
    }
}
