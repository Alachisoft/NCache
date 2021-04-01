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
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ClientDeathDetection : ICloneable, ICompactSerializable
    {
        bool enabled = true;
        int graceTime = 60;

        public ClientDeathDetection() { }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

     
        public int GraceInterval
        {
            get { return graceTime; }
            set { graceTime = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            ClientDeathDetection deathDetection = new ClientDeathDetection();
            deathDetection.Enabled = Enabled;
            deathDetection.GraceInterval = GraceInterval;
            return deathDetection;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            enabled = reader.ReadBoolean();
            graceTime = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(enabled);
            writer.Write(graceTime);
        }

        #endregion
    }
}
