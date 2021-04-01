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

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class MethodActivity : Activity, Runtime.Serialization.ICompactSerializable
    {
        private string _method;

        public MethodActivity(string method, string log)
            : base(log)
        {
            _method = method;
        }

        public string MethodName
        {
            get { return _method; }
        }

        #region  ICompact Serializable Members
        public new void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _method = (string)reader.ReadObject();
        }

        public new void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_method);
        } 
        #endregion
    }
}