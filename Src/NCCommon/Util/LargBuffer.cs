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
namespace Alachisoft.NCache.Common.Util
{
    public class LargBuffer
    {
        private byte[] _buffer;
        private BufferStatus _status = BufferStatus.Free;
        private int _id;

        internal LargBuffer(byte[] buffer,int id)
        {
            _buffer = buffer;
            _id = id;
        }

        internal LargBuffer(byte[] buffer,int id, BufferStatus status):this(buffer,id)
        {
            _status = status;
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        internal BufferStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }

        internal int ID
        {
            get { return _id; }
        }

        internal bool IsFree
        {
            get { return _status == BufferStatus.Free; }
        }
    }
}