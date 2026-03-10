using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class ClusterHealthData : ICompactSerializable
    {

        public ServerNode Sender { get; set; }
        public List<ServerNode> RunningServers { get; set; }
        public int ConnectedClients { get; set; }
        public ClusterHealthStatus Status { get; set; }

        public void Deserialize(CompactReader reader)
        {
            throw new NotImplementedException();
        }

        public void Serialize(CompactWriter writer)
        {
            throw new NotImplementedException();
        }

        
    }
}
