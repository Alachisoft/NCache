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
    public class DistributedOrderedRSEnumerator : DistributedRSEnumerator
    {
        protected List<OrderByArgument> _orderByArguments;

        public DistributedOrderedRSEnumerator(List<IRecordSetEnumerator> partitionRecordSets, List<OrderByArgument> orderByArguments, Dictionary<string, Dictionary<IRecordSetEnumerator, Object>> validReaders)
            : base(partitionRecordSets,validReaders)
        {
            _orderByArguments = orderByArguments;

            List<IRecordSetEnumerator> emprtEnumeratorsList = new List<IRecordSetEnumerator>();
            foreach (IRecordSetEnumerator rse in base._partitionRecordSets)
            {
                if (!rse.MoveNext())
                    emprtEnumeratorsList.Add(rse);
            }

            foreach (IRecordSetEnumerator rse in emprtEnumeratorsList)
            {
                _partitionRecordSets.Remove(rse);
                RemoveFromValidReaders(rse);
            }

        }

        public override bool MoveNext()
        {
            if (_partitionRecordSets.Count == 0)
                return false;
            IRecordSetEnumerator rse = _partitionRecordSets[0];
            _current = _partitionRecordSets[0].Current;
            foreach (IRecordSetEnumerator rs in _partitionRecordSets)
            {
                if (_current.CompareOrder(rs.Current, _orderByArguments) > 0)
                {
                    _current = rs.Current;
                    rse = rs;
                }
            }
            if (!rse.MoveNext())
            {
                _partitionRecordSets.Remove(rse);
                RemoveFromValidReaders(rse);
            }
            if (_current == null)
                return false;
            return true;
        }
    }
}
