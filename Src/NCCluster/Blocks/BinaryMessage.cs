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
using System.Collections;

namespace Alachisoft.NGroups.Blocks
{
    internal class BinaryMessage
    {
        private IList buffer;
        private Array userPayLoad;
        private DateTime _time = DateTime.Now;

        public BinaryMessage(IList buf, Array userpayLoad)
        {
            buffer = buf;
            userPayLoad = userpayLoad;
        }

        public IList Buffer
        {
            get { return buffer; }
        }
        public Array UserPayLoad
        {
            get { return userPayLoad; }
        }

        public int Size
        {
            get
            {
                int size = 0;
                if (buffer != null)
                {
                    foreach(byte[] buff in buffer)
                        size += buff.Length;
                }
                if (userPayLoad != null)
                {
                    for (int i = 0; i < userPayLoad.Length; i++)
                    {
                        byte[] tmp = userPayLoad.GetValue(i) as byte[];
                        if (tmp != null) size += tmp.Length;
                    }
                }
                return size;
            }
        }

    }
}