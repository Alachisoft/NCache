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
using System.Diagnostics;
using System.Text;
using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class RequestStatus : Runtime.Serialization.ICompactSerializable, ISizable
    {
        int _status;
        IList _requestResult;

        public RequestStatus() { }

        public RequestStatus(int status)
        {
            this._status = status;
        }

        public int Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public IList RequestResult
        {
            get { return _requestResult; }
            set { _requestResult = value; }
        }
        
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _status = reader.ReadInt32();
            _requestResult = (IList)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_status);
            writer.WriteObject(_requestResult);
        }

        public int Size
        {
            get { return MemoryUtil.NetIntSize + ResponseSize; }
        }

        public int InMemorySize
        {
            get { return Size; }
        }

        public int ResponseSize
        {
            get
            {
                int size = 0;
                if (_requestResult != null)
                {
                    for (int i = 0; i < _requestResult.Count; i++)
                        if (_requestResult[i].GetType().IsAssignableFrom(typeof(byte[])))
                            size += ((byte[])_requestResult[i]).Length;
                        else if (_requestResult[i] is IList)
                        {
                            foreach (object o in (IList)_requestResult[i])
                            {
                                if (o.GetType().IsAssignableFrom(typeof(byte[])))
                                    size += ((byte[])o).Length;
                            }
                        }
                }
                return size;
            }
        }
    }
}
