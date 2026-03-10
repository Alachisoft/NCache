using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Messaging
{
    public class TopicState : ICompactSerializable
    {

        private ArrayList _registeredTopicsState = new ArrayList();

        public ArrayList RegisteredTopicStates
        {
            get { return _registeredTopicsState; }
            set { _registeredTopicsState = value; }
        }


        public void Deserialize(CompactReader reader)
        {
            _registeredTopicsState = reader.ReadObject() as ArrayList;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_registeredTopicsState);
        }

      
    }
}
