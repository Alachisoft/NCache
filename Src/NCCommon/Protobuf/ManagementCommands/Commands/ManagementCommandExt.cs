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
//using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.RPCFramework;

namespace Alachisoft.NCache.Common.Protobuf
{
    public partial class ManagementCommand : global::ProtoBuf.IExtensible,IRequest
    {
        private TargetMethodParameter _parameters = new TargetMethodParameter();
        public long RequestId
        {
            get
            {
                return this._requestId;
            }
            set
            {
                this._requestId = value;
            }
        }

        public TargetMethodParameter Parameters
        {
            get { return _parameters; }
        }

        public override string ToString()
        {
            string str = "[" + methodName + " (" + overload + "), request_id = " + requestId + "]";
            return  str;
        }
    }
}
