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
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class BatchConfig : ICloneable, ICompactSerializable
    {
        string batchInterval, operationDelay;

        public BatchConfig() { }

        [ConfigurationAttribute("batch-interval","ms")]
        public string BatchInterval
        {
            get { return batchInterval; }
            set { batchInterval = value; }
        }

        [ConfigurationAttribute("operation-delay","ms")]
        public string OperationDelay
        {
            get { return operationDelay; }
            set { operationDelay = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            BatchConfig config = new BatchConfig();
            config.BatchInterval = BatchInterval != null ? (string)BatchInterval.Clone() : null;
            config.OperationDelay = OperationDelay != null ? (string)OperationDelay.Clone() : null;
            return config;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            batchInterval = reader.ReadObject() as string;
            operationDelay = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(batchInterval);
            writer.WriteObject(operationDelay);
        }

        #endregion
    }
}
