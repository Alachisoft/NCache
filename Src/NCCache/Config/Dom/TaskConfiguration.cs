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
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class TaskConfiguration : ICloneable, ICompactSerializable
    {
        private int maxTasks = 10;
        private int queueSize = 10;
        private bool communicateStats = false;
        private int chunkSize = 100;
        private int maxExceptions = 10;


        [ConfigurationAttribute("max-tasks")]
        public int MaxTasks
        {
            get { return maxTasks; }
            set { maxTasks = value; }
        }

        [ConfigurationAttribute("chunk-size")]
        public int ChunkSize
        {
            get { return chunkSize; }
            set { chunkSize = value; }
        }

        [ConfigurationAttribute("communicate-stats")]
        public bool CommunicateStats
        {
            get { return communicateStats; }
            set { communicateStats = value; }
        }

        [ConfigurationAttribute("queue-size")]
        public int QueueSize
        {
            get { return queueSize; }
            set { queueSize = value; }
        }


        [ConfigurationAttribute("max-avoidable-exceptions")]
        public int MaxExceptions
        {
            get { return maxExceptions; }
            set { maxExceptions = value; }
        }

        

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.maxTasks = reader.ReadInt32();
            this.chunkSize = reader.ReadInt32();
            this.communicateStats = reader.ReadBoolean();
            this.queueSize = reader.ReadInt32();
            this.maxExceptions = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(maxTasks);
            writer.Write(chunkSize);
            writer.Write(communicateStats);
            writer.Write(queueSize);
            writer.Write(maxExceptions);
        }

        public object Clone()
        {
            TaskConfiguration config = new TaskConfiguration();
            config.MaxTasks = MaxTasks;
            config.ChunkSize = ChunkSize;
            config.CommunicateStats = CommunicateStats;
            config.QueueSize = QueueSize;
            config.MaxExceptions = MaxExceptions;
            return config;
        }
    }
}
