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

using System;

using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Queries
{
    [Serializable]
    public class AverageResult : ICompactSerializable
    {
        private decimal sum;
        private decimal count;

        public decimal Sum
        {
            get
            {
                return this.sum;
            }
            set
            {
                this.sum = value;
            }
        }

        public decimal Count
        {
            get
            {
                return this.count;
            }
            set
            {
                this.count = value;
            }
        }

        public decimal Average
        {
            get
            {
                decimal average = 0;

                if (Count > 0)
                    average = Sum / Count;

                return average;
            }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            sum = reader.ReadDecimal();
            count = reader.ReadDecimal();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(sum);
            writer.Write(count);
        }

        #endregion
    }
}
