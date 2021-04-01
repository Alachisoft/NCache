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

using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    public class AsyncCallbackInfo : CallbackInfo
    {
        private int _requestId;

        public int RequestID
        {
            get { return _requestId; }
        }

        public AsyncCallbackInfo(int reqId, string clientId, object asyncCallback)
            : base(clientId, asyncCallback, EventDataFilter.None)
        {
            _requestId = reqId;
        }

        #region ------------- ICompactSerializable -------------

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _requestId = reader.ReadInt32();
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_requestId);
        }

        #endregion

        #region ------------------- ToString() -----------------

        public override string ToString()
        {
            string client = Client ?? "NULL";
            string callback = Callback != null ? Callback.ToString() : "NULL";
            return client + ":" + callback;
        }

        #endregion
    }
}