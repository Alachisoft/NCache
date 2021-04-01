//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Config.Dom;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class Cluster : ICloneable,ICompactSerializable
    {
        int opTimeout = 60;
        int statsRepInterval;
        Alachisoft.NCache.Config.Dom.ReplicationStrategy _replicationStrategy;

        Channel channel;

        public Cluster()
        {
            channel = new Channel();
        }


        [ConfigurationAttribute("operation-timeout", true, false, "sec")]
        public int OpTimeout
        {
            get { return opTimeout; }
            set { opTimeout = value; }
        }


        [ConfigurationAttribute("stats-repl-interval", true, false, "sec")]
        public int StatsRepInterval
        {
            get { return statsRepInterval; }
            set { statsRepInterval = value; }
        }

#if SERVER || CLIENT 
        [ConfigurationSection("data-replication", true, false)]
#endif
        public Alachisoft.NCache.Config.Dom.ReplicationStrategy ReplicationStrategy
        {
            get { return _replicationStrategy; }
            set { _replicationStrategy = value; }
        }

        [ConfigurationSection("cluster-connection-settings", true, false)]
        public Channel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Cluster cluster = new Cluster();
            cluster.OpTimeout = OpTimeout;
            cluster.StatsRepInterval = StatsRepInterval;
            cluster.ReplicationStrategy = ReplicationStrategy != null ? (Alachisoft.NCache.Config.Dom.ReplicationStrategy)ReplicationStrategy.Clone() : null;

            cluster.Channel = Channel != null ? (Channel)Channel.Clone() : null;
            return cluster;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            opTimeout = reader.ReadInt32();
            statsRepInterval = reader.ReadInt32();
            this._replicationStrategy=reader.ReadObject() as ReplicationStrategy;
            channel = reader.ReadObject() as Channel;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(opTimeout);
            writer.Write(statsRepInterval);
            writer.WriteObject(this._replicationStrategy);
            writer.WriteObject(channel);
        }
        #endregion
    
    }
}
