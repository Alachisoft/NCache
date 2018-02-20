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
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Common.DataReader
{
   public class DistributedRSEnumerator: IRecordSetEnumerator
    {
        protected List<IRecordSetEnumerator> _partitionRecordSets = null;
        private IRecordSetEnumerator _currentRecordSet = null;
        protected RecordRow _current = null;
        private int _counter = 0;
        private bool _next = false;
        private Dictionary<string, Dictionary<IRecordSetEnumerator, Object>> _validReaders;

        public DistributedRSEnumerator(List<IRecordSetEnumerator> partitionRecordSets, Dictionary<string, Dictionary<IRecordSetEnumerator, Object>> validReaders)
        {
            _partitionRecordSets = partitionRecordSets;
            _validReaders = validReaders;
        }

        #region-----------------IRecordSetEnumerator---------------------
        public RecordRow Current
        {
            get 
            {
               return _current;
            }
        }

        public ColumnCollection ColumnCollection
        {
            get { return _partitionRecordSets.Count > 0 ? _partitionRecordSets[0].ColumnCollection : null; }
        }

        public virtual bool MoveNext()
        {
            SetCurrentEnumerator();
            return _next;
        }
        #endregion
        private void SetCurrentEnumerator() //for each move next,call pick one of partition record set.
        {
            if (_partitionRecordSets.Count == 0) return;
            bool hasNext = false;
            do
            {
                try
                {
                    if (_partitionRecordSets.Count <= _counter)
                        throw new InvalidReaderException("Data reader has lost its state");

                    _currentRecordSet = _partitionRecordSets[_counter];

                    hasNext = _currentRecordSet.MoveNext();
                }
                catch (InvalidReaderException e)
                {
                    this.Dispose();
                    throw;
                }
                catch (Exception e)
                {
                    throw;
                }

                if (hasNext) _current = _currentRecordSet.Current;
                else
                {
                    _partitionRecordSets.Remove(_currentRecordSet);
                    RemoveFromValidReaders(_currentRecordSet);
                }
                _counter++;
                if (_counter >= _partitionRecordSets.Count)
                    _counter = 0;
            } while (!hasNext && _partitionRecordSets.Count > 0);
            _next = hasNext;

        }

        public void RemoveFromValidReaders(IRecordSetEnumerator pe)
        {
            IPartitionInfo info = pe as IPartitionInfo;

            if (info != null)
            {
                var readers = default(Dictionary<IRecordSetEnumerator, Object>);

                lock (_validReaders)
                {
                    _validReaders.TryGetValue(info.Server, out readers);

                    if (readers != null)
                    {
                        readers.Remove(pe);
                    }
                }
            }

        }

        public void Dispose()
        {            
            if (_partitionRecordSets != null)
            {
                foreach (var pe in _partitionRecordSets)
                {
                    pe.Dispose();
                    RemoveFromValidReaders(pe);
                }
                _partitionRecordSets = null;
            }
        }
    }
}
