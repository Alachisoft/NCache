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

        //private IDictionary<string, IList<PatternInfo>> _subscribedPatterns = new Dictionary<string, IList<PatternInfo>>();
        private ArrayList _registeredTopicsState = new ArrayList();

        //public IDictionary<string, IList<PatternInfo>> SubscribedPatterns
        //{
        //    get { return _subscribedPatterns; }
        //    set { _subscribedPatterns = value; }

        //}

        public ArrayList RegisteredTopicStates
        {
            get { return _registeredTopicsState; }
            set { _registeredTopicsState = value; }
        }


        public void Deserialize(CompactReader reader)
        {
           // _subscribedPatterns= SerializationUtility.DeserializeDictionaryList<string, PatternInfo>(reader);
            _registeredTopicsState = reader.ReadObject() as ArrayList;
        }

        public void Serialize(CompactWriter writer)
        {
            //SerializationUtility.SerializeDictionaryList(_subscribedPatterns, writer);
            writer.WriteObject(_registeredTopicsState);
        }

      
    }
}
