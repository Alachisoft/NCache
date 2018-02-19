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

namespace Alachisoft.NCache.Common.DataReader
{
    public class RecordSetEnumerator : IRecordSetEnumerator
    {
        IRecordSet _recordSet = null;
        RecordRow _current = null;
        int _rowId = -1;

        public RecordSetEnumerator(IRecordSet recordSet) 
        {
            _recordSet = recordSet;
        }
        public RecordRow Current
        {
            get { return _current; }
        }

        public ColumnCollection ColumnCollection
        {
            get { return _recordSet != null ? _recordSet.GetColumnMetaData() : null; }
        }

        public bool MoveNext()
        {
            if (_recordSet != null)
            {
                if (_recordSet.ContainsRow(++_rowId))
                {
                    _current = _recordSet.GetRow(_rowId);
                    _recordSet.RemoveRow(_rowId);
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            _recordSet = null;
            _current = null;
        }
    }
}
