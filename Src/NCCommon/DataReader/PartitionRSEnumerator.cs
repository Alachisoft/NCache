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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Common.DataReader
{
    public class PartitionRSEnumerator : IRecordSetEnumerator
    {
        IRecordSetEnumerator _recordSetEnumerator;
        string _nodeIP;
        string _readerId;
        int _nextIndex;
        IRecordSetLoader _cacheImpl;
        private bool isValid = true;
        private IRecordSetEnumerator rse;

        public PartitionRSEnumerator(IRecordSetEnumerator recordSetEnumerator, string readerId, string nodeIP, int nextIdex, IRecordSetLoader cacheImpl) 
        {
            _recordSetEnumerator = recordSetEnumerator;
            _readerId = readerId;
            _nodeIP = nodeIP;
            _nextIndex = nextIdex;
            _cacheImpl = cacheImpl;
        }


       

        public bool IsValid
        {
            set { isValid = value; }
        }

       public bool GetNextRecordSetChunk()
       {
           ReaderResultSet readerChunk = null;
           if (_cacheImpl != null)
           {
               if (!isValid)
                   throw new InvalidReaderException("Reader state has been lost.");
               readerChunk = _cacheImpl.GetRecordSet(_readerId, _nodeIP, _nextIndex);

           }

           if (readerChunk != null && readerChunk.RecordSet != null && readerChunk.RecordSet.RowCount > 0)
           {
               _recordSetEnumerator = new RecordSetEnumerator(readerChunk.RecordSet);
               _nextIndex = readerChunk.NextIndex;
               return _recordSetEnumerator.MoveNext();
           }
           return false;
       }
       #region-----------------IRecordSetEnumerator---------------------

       public RecordRow Current
       {
           get { return _recordSetEnumerator.Current; }
       }

       public ColumnCollection ColumnCollection
       {
           get { return _recordSetEnumerator.ColumnCollection; }
       }

       public bool MoveNext()
       {
           bool next=_recordSetEnumerator.MoveNext();
           if (!next)
           {
               next = GetNextRecordSetChunk();
           }
           return next;
       }
       #endregion

       public void Dispose()
       {
           if (_cacheImpl != null)
               _cacheImpl.DisposeReader(_readerId, _nodeIP);
           _recordSetEnumerator.Dispose();
           _cacheImpl = null;
       }
    }
}
