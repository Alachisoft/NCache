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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class WriteBehind : ICloneable, ICompactSerializable
    {
        string mode,throttling,requeueLimit,eviction;
        BatchConfig batchConfig;

        public WriteBehind() { }

        [ConfigurationAttribute("mode")]
        public string Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        [ConfigurationAttribute("failed-operations-queue-limit","")]
        public string RequeueLimit
        {
            get { return requeueLimit; }
            set { requeueLimit = value; }
        }

        [ConfigurationAttribute("failed-operations-eviction-ratio","%")]
        public string Eviction
        {
            get { return eviction; }
            set { eviction = value; }
        }
        [ConfigurationAttribute("throttling-rate-per-sec", "")]
        public string Throttling
        {
            get { return throttling; }
            set { throttling = value; }
        }


        [ConfigurationSection("batch-mode-config")]//Changes for New Dom from param
        public BatchConfig BatchConfig
        {
            get { return batchConfig; }
            set { batchConfig = value; }
        }
        #region ICloneable Members

        public object Clone()
        {
            WriteBehind writeBehind = new WriteBehind();
            writeBehind.Mode = Mode != null ? (string)Mode.Clone() : null;
            writeBehind.Throttling = Throttling != null ? (string)Throttling.Clone() : null;
            writeBehind.Eviction = Eviction != null ? (string)Eviction.Clone() : null;
            writeBehind.RequeueLimit = RequeueLimit != null ? (string)RequeueLimit.Clone() : null;
            writeBehind.BatchConfig = BatchConfig != null ? BatchConfig.Clone() as BatchConfig : null;
            return writeBehind;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            mode = reader.ReadObject() as string;
            throttling = reader.ReadObject() as string;
            batchConfig = reader.ReadObject() as BatchConfig;
            eviction = reader.ReadObject() as string;
            requeueLimit = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(mode);
            writer.WriteObject(throttling);
            writer.WriteObject(batchConfig);
            writer.WriteObject(eviction);
            writer.WriteObject(requeueLimit);
        }

        #endregion
    }
}
