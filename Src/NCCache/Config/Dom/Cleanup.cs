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
    public class Cleanup: ICloneable,ICompactSerializable
    {
        int interval;

        public Cleanup() { }

        [ConfigurationAttribute("interval", "sec")]
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Cleanup cleanup = new Cleanup();
            cleanup.Interval = Interval;
            return cleanup;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            interval = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(interval);
        }

        #endregion
    }
}
