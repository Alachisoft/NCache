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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Queries;

namespace Alachisoft.NCache.Common.DataReader
{
    public class DistributedGroupedRSEnumerator : DistributedOrderedRSEnumerator
    {
        public DistributedGroupedRSEnumerator(List<IRecordSetEnumerator> partitionRecordSets, List<OrderByArgument> orderByArguments)
            : base(partitionRecordSets, orderByArguments)
        { }

        public override bool MoveNext()
        {
            if (base.MoveNext())
            {
                List<IRecordSetEnumerator> emptyRSE = new List<IRecordSetEnumerator>();
                foreach (IRecordSetEnumerator rse in _partitionRecordSets)
                {
                    if (_current.CompareOrder(rse.Current, _orderByArguments) == 0)
                    {
                        _current.Merge(rse.Current);
                        if (!rse.MoveNext())
                            emptyRSE.Add(rse);
                    }
                }

                foreach (IRecordSetEnumerator rse in emptyRSE)
                {
                    _partitionRecordSets.Remove(rse);
                }

                return true;
            }
            return false;
        }
    }
}
