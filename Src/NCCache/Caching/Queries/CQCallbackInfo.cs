// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Caching.Queries
{
   [Serializable]
    public class CQCallbackInfo  : ICompactSerializable
    {
        string activeQueryId;
        List<string> clientIds = new List<string>();
        IDictionary<string, EventDataFilter> datafilters = new Dictionary<string, EventDataFilter>();

        public string CQId
        {
            get { return activeQueryId; }
            set { activeQueryId = value; }
        }


        public List<string> ClientIds
        {
            get { return clientIds; }
            set { clientIds = value; }
        }

        public IDictionary<string, EventDataFilter> DataFilters
        {
            get { return datafilters; }
            set { datafilters = value; }
        }

        public override bool Equals(object obj)
        {
            CQCallbackInfo info = obj as CQCallbackInfo;
            if (info != null)
            {
                if (this.CQId == info.CQId)
                {
                    return true;
                }
            }
            return false;
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            activeQueryId =(string) reader.ReadObject();
            
            clientIds = Common.Util.SerializationUtility.DeserializeList<string>(reader);
            
            datafilters = Common.Util.SerializationUtility.DeserializeDictionary<string, EventDataFilter>(reader);
        }

        void ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(activeQueryId);


            Common.Util.SerializationUtility.SerializeList<string>(clientIds,writer);
            Common.Util.SerializationUtility.SerializeDictionary(datafilters,writer);
        }

        #endregion
    }
}
