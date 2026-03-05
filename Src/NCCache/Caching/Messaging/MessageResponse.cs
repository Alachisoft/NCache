using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
   
   public class MessageResponse : ICompactSerializable
    {
            private IDictionary<string, IList<object>> _assignedMessages;

            public MessageResponse()
            {
             _assignedMessages = new Dictionary<string, IList<object>>();
            }

            public IDictionary<string, IList<object>> AssignedMessages
            {
                set { _assignedMessages = value; }
                get { return this._assignedMessages; }
            }

            

        public void Deserialize(CompactReader reader)
        {
            _assignedMessages = reader.ReadObject() as IDictionary<string,IList<object>>;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_assignedMessages);
        }
    }
}
