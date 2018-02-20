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

using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    public class AsyncCallbackInfo : CallbackInfo, ICompactSerializable
    {
        private int _requestId;

        //public AsyncCallbackInfo() { }
        public AsyncCallbackInfo(int reqid, string clietnid, object asyncCallback)
            : base(clietnid, asyncCallback, (EventDataFilter) EventDataFilter.None)
        {
            _requestId = reqid;
        }

        public int RequestID { get { return _requestId; } }

        public override bool Equals(object obj)
        {
            if (obj is CallbackInfo)
            {
                CallbackInfo other = obj as CallbackInfo;
                if (other.Client != Client) return false;
                if (other.Callback is short && theCallback is short)
                {
                    if ((short)other.Callback != (short)theCallback) return false;
                }
                else if (other.Callback != theCallback) return false;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            string cnt = theClient != null ? theClient : "NULL";
            string cback = theCallback != null ? theCallback.ToString() : "NULL";
            return cnt + ":" + cback;
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _requestId = reader.ReadInt32();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_requestId);
        }

        #endregion
    }
}