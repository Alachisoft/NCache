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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Queries
{
    [Serializable]
    public class OrderByArgument : ICompactSerializable
    {
        private string _attributeName;
        private Order _order = Order.ASC;

        public string AttributeName
        {
            get { return _attributeName; }
            set { _attributeName = value; }
        }

        public Order Order
        {
            get { return _order; }
            set { _order = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _attributeName = reader.ReadObject() as string;
            _order = (Order)reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_attributeName);
            writer.Write(Convert.ToInt32(_order));
        }
    }
}
