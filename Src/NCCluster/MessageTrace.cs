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
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NGroups
{
    internal class MessageTrace : ICompactSerializable
    {
        string _trace;
        HPTime _timeStamp;

        public MessageTrace(string trace)
        {
            _trace = trace;
            _timeStamp = HPTime.Now;
        }

        public override string ToString()
        {
            string toString = "";
            if (_trace != null)
            {
                toString = _trace + " : " + _timeStamp.ToString();
            }
            return toString;
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _trace = reader.ReadObject() as string;
            _timeStamp = reader.ReadObject() as HPTime;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_trace);
            writer.WriteObject(_timeStamp);
        }

        #endregion
    }
}
